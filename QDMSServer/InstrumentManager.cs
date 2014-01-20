// -----------------------------------------------------------------------
// <copyright file="InstrumentManager.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

// This class is used to add, remove, search for, and modify instruments.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Data.Entity;
using EntityData;
using QDMS;
using QDMSServer.DataSources;

namespace QDMSServer
{
    public class InstrumentManager : IInstrumentSource
    {
        public static bool AddContinuousFuture()
        {
            return true;
        }

        /// <summary>
        /// Tries to add multiple instruments to the database.
        /// </summary>
        /// <returns>The number of instruments that were successfully added.</returns>
        public static int AddInstruments(IList<Instrument> instruments, bool updateIfExists = false)
        {
            int count =  instruments.Count(s => AddInstrument(s, updateIfExists, false));
            return count;
        }

        /// <summary>
        /// Add a new instrument or update an existing instrument in the database.
        /// </summary>
        /// <param name="instrument"></param>
        /// <param name="updateIfExists"></param>
        /// <param name="saveChanges">Set to true if saving to db should be done.</param>
        /// <returns>True if the insertion or update succeeded. False if it did not.</returns>
        public static bool AddInstrument(Instrument instrument, bool updateIfExists = false, bool saveChanges = true)
        {
            if (instrument.IsContinuousFuture)
            {
                throw new Exception("Cannot add continuous futures using this method");
            }

            using (var context = new MyDBContext())
            {
                //check if the instrument already exists in the database or not
                var existingInstrument = context.Instruments.SingleOrDefault(x =>
                    (x.ID == instrument.ID) ||
                    (x.Symbol == instrument.Symbol && 
                    x.DatasourceID == instrument.DatasourceID && 
                    x.ExchangeID == instrument.ExchangeID &&
                    x.Expiration == instrument.Expiration));

                if (existingInstrument == null) //object doesn't exist, so we add it
                {
                    //attach the datasource, exchanges, etc. so it doesn't try to add them
                    context.Datasources.Attach(instrument.Datasource);
                    if (instrument.PrimaryExchange != null)
                    {
                        context.Exchanges.Attach(instrument.PrimaryExchange);
                    }
                    if (instrument.PrimaryExchangeID != instrument.ExchangeID && instrument.Exchange != null)
                    {
                        context.Exchanges.Attach(instrument.Exchange);
                    }

                    //if necessary, load sessions from teplate or exchange
                    if (instrument.SessionsSource == SessionsSource.Exchange && instrument.Exchange != null)
                    {
                        instrument.Sessions = new List<InstrumentSession>();
                        var exchange = context.Exchanges.Include("Sessions").First(x => x.ID == instrument.ExchangeID);
                        foreach (ExchangeSession s in exchange.Sessions)
                        {
                            instrument.Sessions.Add(MyUtils.SessionConverter(s));
                        }
                    }
                    else if (instrument.SessionsSource == SessionsSource.Template)
                    {
                        instrument.Sessions = new List<InstrumentSession>();
                        var template = context.SessionTemplates.Include("Sessions").FirstOrDefault(x => x.ID == instrument.SessionTemplateID);
                        if (template != null)
                        {
                            foreach (TemplateSession s in template.Sessions)
                            {
                                instrument.Sessions.Add(MyUtils.SessionConverter(s));
                            }
                        }
                    }
                    else if (instrument.SessionsSource == SessionsSource.Exchange && instrument.Exchange == null)
                    {
                        instrument.SessionsSource = SessionsSource.Custom;
                        instrument.Sessions = new List<InstrumentSession>();
                    }
                    
                    context.Instruments.Add(instrument);
                    context.Database.Connection.Open();
                    if (saveChanges) context.SaveChanges();
                    return true;
                }
                else if (updateIfExists) //object exist, but we want to update it
                {
                    context.Entry(existingInstrument).CurrentValues.SetValues(instrument);
                    if (saveChanges) context.SaveChanges();
                    return true;
                }
            }
            return false; //object exists and we don't update it
        }

        /// <summary>
        /// Search for instruments.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="search">Any properties set on this instrument are used as search parameters.
        /// If null, all instruments are returned.</param>
        /// <param name="pred">A predicate to use directly in the instrument search.</param>
        /// <returns>A list of instruments matching the criteria.</returns>
        public List<Instrument> FindInstruments(MyDBContext context = null, Instrument search = null, Func<Instrument, bool> pred = null)
        {
            if (context == null) context = new MyDBContext();

            IQueryable<Instrument> query = context.Instruments
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

            if (pred != null)
            {
                var instruments = query.Where(pred).ToList();
                foreach (Instrument i in instruments)
                {
                    i.Exchange.Sessions.ToList();
                }
                return instruments;
            }
            else if (search == null)
            {
                var allExchanges = context.Exchanges.Include(x => x.Sessions).ToList();
                var allInstruments = query.ToList();
                return allInstruments;
            }
            else
            {
                //first handle the cases where there is a single, unique instrument to return
                if (search.ID != null)
                {
                    query = query.Where(x => x.ID == search.ID);
                }
                else if (search.Symbol != null && search.DatasourceID.HasValue && search.ExchangeID.HasValue && search.Expiration.HasValue)
                {
                    query = query.Where(x => x.Symbol == search.Symbol 
                        && x.DatasourceID == search.DatasourceID 
                        && x.ExchangeID == search.ExchangeID
                        && x.Expiration == search.Expiration);
                }
                else if (search.ContinuousFutureID.HasValue)
                {
                    query = query.Where(x => x.ContinuousFutureID == search.ContinuousFutureID);
                }
                else //no unique cases, so just add the restrictions where applicable
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

                    if(search.DatasourceID.HasValue)
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
            }
            var instrumentList =  query.ToList();
            //see comment above, we can't include these in the original query due to a bug
            //and we can't allow lazy loading of the sessions
            //so we force them to be loaded right now
            foreach (Instrument i in instrumentList) 
            {
                i.Exchange.Sessions.ToList();
            }
            return instrumentList;
        }

        /// <summary>
        /// Updates the instrument with new values. Instrument must have an ID.
        /// </summary>
        public static void UpdateInstrument(Instrument instrument)
        {
            if(!instrument.ID.HasValue) return;

            using (var context = new MyDBContext())
            {
                try
                {
                    //find it
                    Instrument instrumentFromDB = context.Instruments.First(x => x.ID == instrument.ID);
                    //update it
                    context.Entry(instrumentFromDB).CurrentValues.SetValues(instrument); //perhaps update all the underlying collections as well?

                    //save it
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Update instrument error: " + ex.Message);
                }
                

            }
        }

        /// <summary>
        /// Delete an instrument and all locally stored data.
        /// </summary>
        public static void RemoveInstrument(Instrument instrument)
        {
            using (var entityContext = new MyDBContext())
            {
                //hacking around the circular reference issue
                if (instrument.IsContinuousFuture)
                {
                    entityContext.Instruments.Attach(instrument);
                    var tmpCF = instrument.ContinuousFuture;
                    instrument.ContinuousFuture = null;
                    instrument.ContinuousFutureID = null;
                    entityContext.SaveChanges();

                    entityContext.ContinuousFutures.Attach(tmpCF);
                    entityContext.ContinuousFutures.Remove(tmpCF);
                    entityContext.SaveChanges();
                }

                entityContext.Instruments.Attach(instrument);
                entityContext.Instruments.Remove(instrument);
                entityContext.SaveChanges();
            }

            using (var localStorage = DataStorageFactory.Get())
            {
                localStorage.Connect();

                localStorage.DeleteAllInstrumentData(instrument);
            }
        }

    }
}
