// -----------------------------------------------------------------------
// <copyright file="InstrumentManager.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using EntityData;
using NLog;
using QDMSServer;

namespace QDMS.Server
{
    /// <summary>
    /// This class is used to add, remove, search for, and modify instruments.
    /// </summary>
    public class InstrumentRepository : IInstrumentSource
    {
        public readonly IMyDbContext Context;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public InstrumentRepository(IMyDbContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Add a new instrument
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="saveChanges">Set to true if saving to db should be done.</param>
        /// <returns>True if the insertion or update succeeded. False if it did not.</returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<Instrument> AddInstrument(Instrument instrument, bool saveChanges = true)
        {
            //Check if an instrument with these unique constraints already exists
            var existingInstrument = Context.Instruments.SingleOrDefault(x =>
                (x.ID == instrument.ID) ||
                (x.Symbol == instrument.Symbol &&
                x.DatasourceID == instrument.DatasourceID &&
                x.ExchangeID == instrument.ExchangeID &&
                x.Expiration == instrument.Expiration &&
                x.Type == instrument.Type));

            if (existingInstrument != null)
            {
                //throw new ArgumentException("Unique constraint violation");
            }

            ValidateInstrument(instrument);

            //All this stuff is detached, so we need to get the attached objects
            instrument.Datasource = Context.GetAttachedEntity(instrument.Datasource);
            instrument.Exchange = Context.GetAttachedEntity(instrument.Exchange);
            instrument.PrimaryExchange = Context.GetAttachedEntity(instrument.PrimaryExchange);
            instrument.Tags = instrument.Tags != null 
                ? new List<Tag>(instrument.Tags.Select(Context.GetAttachedEntity).ToList()) 
                : new List<Tag>();

            //If necessary, load sessions from teplate or exchange
            if (instrument.SessionsSource == SessionsSource.Exchange && instrument.Exchange != null)
            {
                instrument.Sessions = instrument.Exchange.Sessions.Select(x => x.ToInstrumentSession()).ToList();
            }
            else if (instrument.SessionsSource == SessionsSource.Exchange && instrument.Exchange == null)
            {
                instrument.SessionsSource = SessionsSource.Custom;
                instrument.Sessions = new List<InstrumentSession>();
            }
            else if (instrument.SessionsSource == SessionsSource.Template)
            {
                instrument.Sessions = new List<InstrumentSession>();
                var template = Context.SessionTemplates.Include(x => x.Sessions).FirstOrDefault(x => x.ID == instrument.SessionTemplateID);
                if (template != null)
                {
                    foreach (TemplateSession s in template.Sessions)
                    {
                        instrument.Sessions.Add(s.ToInstrumentSession());
                    }
                }
            }

            //Continuous future requires a bit of a workaround
            ContinuousFuture tmpCf = null;
            if (instrument.IsContinuousFuture)
            {
                tmpCf = instrument.ContinuousFuture; //EF can't handle circular references, so we hack around it
                instrument.ContinuousFuture = null;
                instrument.ContinuousFutureID = null;
            }

            Context.Instruments.Add(instrument);
            if (saveChanges)
            {
                await Context.SaveChangesAsync().ConfigureAwait(false);
            }

            if (tmpCf != null)
            {
                tmpCf.UnderlyingSymbol = Context.GetAttachedEntity(tmpCf.UnderlyingSymbol);

                instrument.ContinuousFuture = tmpCf;
                instrument.ContinuousFuture.Instrument = instrument;
                instrument.ContinuousFuture.InstrumentID = instrument.ID.Value;
                if (saveChanges)
                {
                    await Context.SaveChangesAsync().ConfigureAwait(false);
                }
            }

            _logger.Info($"Instrument Manager: successfully added instrument {instrument}");

            return instrument;
        }

        /// <summary>
        /// Updates the instrument with new values.
        /// </summary>
        public async Task UpdateInstrument(Instrument attachedInstrument, Instrument newValues)
        {
            if (attachedInstrument == null) throw new ArgumentNullException(nameof(attachedInstrument));
            if (newValues == null) throw new ArgumentNullException(nameof(newValues));

            ValidateInstrument(newValues);

            //update it
            Context.UpdateEntryValues(attachedInstrument, newValues);

            //Update tags
            attachedInstrument.Tags.UpdateCollection(newValues.Tags, Context);

            //Update sessions
            if (newValues.SessionsSource == SessionsSource.Custom)
            {
                attachedInstrument.Sessions.UpdateCollectionAndElements(newValues.Sessions, Context);
            }

            //Continuous future object
            if (attachedInstrument.IsContinuousFuture)
            {
                Context.UpdateEntryValues(attachedInstrument.ContinuousFuture, newValues.ContinuousFuture);
            }

            //Exchange/PrimaryExchange/Datasource just works (because they work by the ID properties), we don't need to do any attaching

            //save it
            await Context.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="instrument"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ValidateInstrument(Instrument instrument)
        {
            //make sure data source is set and exists
            if (instrument.Datasource == null || Context.Datasources.Find(instrument.DatasourceID) == null)
            {
                throw new ArgumentException("Invalid datasource.");
            }

            //make sure exchange exists, if it is set
            if (instrument.Exchange != null && Context.Exchanges.Find(instrument.ExchangeID) == null)
            {
                throw new ArgumentException("Exchange does not exist.");
            }

            if (instrument.PrimaryExchange != null && Context.Exchanges.Find(instrument.PrimaryExchangeID) == null)
            {
                throw new ArgumentException("Primary exchange does not exist.");
            }
        }

        public async Task<List<Instrument>> FindInstruments(Expression<Func<Instrument, bool>> pred)
        {
            var query = GetIQueryable();

            //A bad predicate can cause an exception here, but I think it should be handled further up
            var instruments = await query.Where(pred).ToListAsync().ConfigureAwait(false);

            foreach (Instrument i in instruments)
            {
                //hack because we can't load these in the normal way, see comment below
                if (i.Exchange != null)
                    i.Exchange.Sessions.ToList();
            }
            return instruments;
        }

        /// <summary>
        /// Search for instruments.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="search">Any properties set on this instrument are used as search parameters.
        /// If null, all instruments are returned.</param>
        /// <returns>A list of instruments matching the criteria.</returns>
        public async Task<List<Instrument>> FindInstruments(Instrument search = null)
        {
            var query = GetIQueryable();

            if (search == null)
            {
                return await FindAllInstruments(query).ConfigureAwait(false);
            }
            else
            {
                //first handle the cases where there is a single, unique instrument to return
                if (search.ID != null)
                {
                    query = query.Where(x => x.ID == search.ID);
                }
                else if (search.Symbol != null && search.DatasourceID.HasValue && search.ExchangeID.HasValue && search.Expiration.HasValue && search.Strike.HasValue && search.Type != InstrumentType.Undefined)
                {
                    query = query.Where(x => x.Symbol == search.Symbol
                        && x.DatasourceID == search.DatasourceID
                        && x.ExchangeID == search.ExchangeID
                        && x.Expiration == search.Expiration
                        && x.Strike == search.Strike
                        && x.Type == search.Type);
                }
                else if (search.ContinuousFutureID.HasValue)
                {
                    query = query.Where(x => x.ContinuousFutureID == search.ContinuousFutureID);
                }
                else //no unique cases, so just add the restrictions where applicable
                {
                    BuildQueryFromSearchInstrument(search, ref query);
                }
            }
            var instrumentList = await query.ToListAsync().ConfigureAwait(false);
            //see comment above, we can't include these in the original query due to a bug
            //and we can't allow lazy loading of the sessions
            //so we force them to be loaded right now
            foreach (Instrument i in instrumentList.Where((x => x.Exchange != null && x.Exchange.Sessions != null)))
            {
                i.Exchange.Sessions.ToList();
            }
            return instrumentList;
        }

        private IQueryable<Instrument> GetIQueryable()
        {
            IQueryable<Instrument> query = Context.Instruments
                .Include(x => x.Tags)
                .Include(x => x.Exchange)
                .Include(x => x.PrimaryExchange)
                .Include(x => x.Datasource)
                .Include(x => x.Sessions)
                //.Include(x => x.Exchange.Sessions)
                //.Include(x => x.PrimaryExchange.Sessions)
                .Include(x => x.ContinuousFuture)
                .Include(x => x.ContinuousFuture.UnderlyingSymbol)
                .AsQueryable();
            //there's a bug in the mysql connector that prevents us from including those session collections right here
            //it just crashes if you do. Devart connector works perfectly fine.
            //We just hack around it by loading up the session collections separately.
            return query;
        }

        private static void BuildQueryFromSearchInstrument(Instrument search, ref IQueryable<Instrument> query)
        {
            if (!string.IsNullOrEmpty(search.Symbol))
                query = query.Where(x => x.Symbol.Contains(search.Symbol));

            if (!string.IsNullOrEmpty(search.UnderlyingSymbol))
                query = query.Where(x => x.UnderlyingSymbol.Contains(search.UnderlyingSymbol));

            if (!string.IsNullOrEmpty(search.Name))
                query = query.Where(x => x.Name.Contains(search.Name));

            if (search.PrimaryExchangeID.HasValue)
                query = query.Where(x => x.PrimaryExchangeID == search.PrimaryExchangeID.Value);

            if (search.ExchangeID.HasValue)
                query = query.Where(x => x.ExchangeID == search.ExchangeID.Value);

            if (search.DatasourceID.HasValue)
                query = query.Where(x => x.DatasourceID == search.DatasourceID.Value);

            if (search.Type != InstrumentType.Undefined)
                query = query.Where(x => x.Type == search.Type);

            if (search.Multiplier.HasValue)
                query = query.Where(x => x.Multiplier == search.Multiplier.Value);

            if (search.OptionType.HasValue)
                query = query.Where(x => x.OptionType == search.OptionType.Value);

            if (search.Strike.HasValue)
                query = query.Where(x => x.Strike == search.Strike.Value);

            if (!string.IsNullOrEmpty(search.Currency))
                query = query.Where(x => x.Currency == search.Currency);

            if (search.MinTick.HasValue)
                query = query.Where(x => x.MinTick == search.MinTick.Value);

            if (!string.IsNullOrEmpty(search.Industry))
                query = query.Where(x => x.Industry.Contains(search.Industry));

            if (!string.IsNullOrEmpty(search.Category))
                query = query.Where(x => x.Category.Contains(search.Category));

            if (!string.IsNullOrEmpty(search.Subcategory))
                query = query.Where(x => x.Subcategory.Contains(search.Subcategory));

            if (!string.IsNullOrEmpty(search.ValidExchanges))
                query = query.Where(x => x.ValidExchanges.Contains(search.ValidExchanges));

            if (search.IsContinuousFuture)
                query = query.Where(x => x.IsContinuousFuture);
        }

        private async Task<List<Instrument>> FindAllInstruments(IQueryable<Instrument> query)
        {
            var allExchanges = Context.Exchanges.Include(x => x.Sessions).ToList();
            return await query.ToListAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Delete an instrument and all locally stored data.
        /// </summary>
        public async Task RemoveInstrument(Instrument instrument, IDataStorage localStorage)
        {
            //hacking around the circular reference issue
            if (instrument.IsContinuousFuture)
            {
                Context.Instruments.Attach(instrument);
                var tmpCF = instrument.ContinuousFuture;
                instrument.ContinuousFuture = null;
                instrument.ContinuousFutureID = null;
                await Context.SaveChangesAsync().ConfigureAwait(false);

                Context.ContinuousFutures.Attach(tmpCF);
                Context.ContinuousFutures.Remove(tmpCF);
                await Context.SaveChangesAsync().ConfigureAwait(false);
            }

            Context.Instruments.Attach(instrument);
            Context.Instruments.Remove(instrument);
            await Context.SaveChangesAsync().ConfigureAwait(false);

            localStorage.Connect();

            localStorage.DeleteAllInstrumentData(instrument);
        }
    }
}