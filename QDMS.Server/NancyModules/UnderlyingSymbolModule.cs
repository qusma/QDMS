// -----------------------------------------------------------------------
// <copyright file="RootSymbolModule.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
using EntityData;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using QDMS.Server.Nancy;

namespace QDMS.Server.NancyModules
{
    public class UnderlyingSymbolModule : NancyModule
    {
        public UnderlyingSymbolModule(IMyDbContext context) : base ("/underlyingsymbols")
        {
            this.RequiresAuthentication();
            var dbSet = context.Set<UnderlyingSymbol>();

            Get("/", _ => dbSet.ToList());

            Get("/{Id:int}", parameters =>
            {
                var id = (int)parameters.Id;
                var exchange = dbSet.FirstOrDefault(x => x.ID == id);

                if (exchange == null) return HttpStatusCode.NotFound;

                return exchange;
            });

            Put("/", _ =>
            {
                UnderlyingSymbol newValues = this.BindAndValidate<UnderlyingSymbol>();
                if (ModelValidationResult.IsValid == false)
                {
                    return this.ValidationFailure();
                }

                //make sure the exchange we want to update exists
                var symbol = dbSet.FirstOrDefault(x => x.ID == newValues.ID);
                if (symbol == null) return HttpStatusCode.NotFound;


                //update values on the exchange
                context.UpdateEntryValues(symbol, newValues);

                context.SaveChanges();

                return symbol;
            });

            Post("/", _ =>
            {
                UnderlyingSymbol symbol = this.BindAndValidate<UnderlyingSymbol>();
                if (ModelValidationResult.IsValid == false)
                {
                    return this.ValidationFailure();
                }

                dbSet.Add(symbol);
                context.SaveChanges();

                //return the object with the id after inserting
                return symbol;
            });

            Delete("/{Id:int}", parameters =>
            {
                int id = parameters.Id;
                var symbol = dbSet.FirstOrDefault(x => x.ID == id);

                if (symbol == null) return HttpStatusCode.NotFound;

                var symbolReferenced = context.Set<ContinuousFuture>().Any(x => x.UnderlyingSymbolID == id);
                if (symbolReferenced)
                {
                    return Negotiate
                        .WithModel(new ErrorResponse(
                            HttpStatusCode.Conflict,
                            "Can't delete this underlying symbol because it has continuous futures assigned to it.", ""))
                        .WithStatusCode(HttpStatusCode.Conflict);
                }

                dbSet.Remove(symbol);
                context.SaveChanges();
                return symbol;
            });
        }
    }
}
