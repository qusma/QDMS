// -----------------------------------------------------------------------
// <copyright file="ExchangeModule.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using EntityData;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using QDMS.Server.Nancy;
using System.Data.Entity;
using System.Linq;

namespace QDMS.Server.NancyModules
{
    public class ExchangeModule : NancyModule
    {
        public ExchangeModule(IMyDbContext context) : base("/exchanges")
        {
            this.RequiresAuthentication();

            var dbSet = context.Set<Exchange>();

            Get["/", runAsync: true] = async (_, token) => await dbSet.Include(x => x.Sessions).ToListAsync(token).ConfigureAwait(false);

            Get["/{Id:int}"] = parameters =>
            {
                var id = (int)parameters.Id;
                var exchange = dbSet.Include(x => x.Sessions).FirstOrDefault(x => x.ID == id);

                if (exchange == null) return HttpStatusCode.NotFound;

                return exchange;
            };

            Post["/"] = _ =>
            {
                Exchange exchange = this.BindAndValidate<Exchange>();
                if (ModelValidationResult.IsValid == false)
                {
                    return this.ValidationFailure();
                }
                
                dbSet.Add(exchange);
                context.SaveChanges();

                //return the object with the id after inserting
                return exchange;
            };

            Put["/"] = _ =>
            {
                Exchange newValues = this.BindAndValidate<Exchange>();
                if (ModelValidationResult.IsValid == false)
                {
                    return this.ValidationFailure();
                }

                //make sure the exchange we want to update exists
                var exchange = dbSet.Include(x => x.Sessions).FirstOrDefault(x => x.ID == newValues.ID);
                if (exchange == null) return HttpStatusCode.NotFound;

                //update values on the exchange
                context.UpdateEntryValues(exchange, newValues);

                //add/remove/update the Sessions collection
                exchange.Sessions.UpdateCollectionAndElements(newValues.Sessions, context);

                //some instruments may have their sessions based on this exchange, we need to update them
                var instruments = context.Set<Instrument>()
                    .Where(x => x.SessionsSource == SessionsSource.Exchange && x.ExchangeID == exchange.ID).ToList();

                foreach (Instrument i in instruments)
                {
                    context.Set<InstrumentSession>().RemoveRange(i.Sessions);
                    i.Sessions.Clear();

                    foreach (ExchangeSession s in exchange.Sessions)
                    {
                        i.Sessions.Add(s.ToInstrumentSession());
                    }
                }

                context.SaveChanges();

                return exchange;
            };

            Delete["/{Id:int}"] = parameters =>
            {
                int id = parameters.Id;
                var exchange = dbSet.FirstOrDefault(x => x.ID == id);

                if (exchange == null) return HttpStatusCode.NotFound;

                var exchangeReferenced = context.Set<Instrument>().Any(x => x.ExchangeID == id || x.PrimaryExchangeID == id);
                if (exchangeReferenced)
                {
                    return Negotiate
                        .WithModel(new ErrorResponse(
                            HttpStatusCode.Conflict,
                            "Can't delete this exchange because it has instruments assigned to it.", ""))
                        .WithStatusCode(HttpStatusCode.Conflict);
                }

                dbSet.Remove(exchange);
                context.SaveChanges();
                return exchange;
            };
        }
    }
}