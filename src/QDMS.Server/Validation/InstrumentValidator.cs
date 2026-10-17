// -----------------------------------------------------------------------
// <copyright file="InstrumentValidator.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;
using FluentValidation.Results;
using System.Collections.Generic;
using System.Linq;

namespace QDMS.Server.Validation
{
    public class InstrumentValidator : AbstractValidator<Instrument>
    {
        public InstrumentValidator()
        {
            RuleFor(req => req.Name).NotEmpty().WithMessage("Name must have a value");
            RuleFor(req => req.Symbol).NotEmpty().WithMessage("Symbol must have a value");
            //RuleFor(req => req.Exchange).NotNull().WithMessage("Must have an exchange"); //some instruments like FRED indexes have no natural exchange
            RuleFor(req => req.Datasource).NotNull().WithMessage("Must have a datasource");
            RuleFor(req => req.Multiplier).NotNull().WithMessage("Must have a multiplier");

            //if using a session template, the template must be set properly
            RuleFor(req => req.SessionTemplateID)
                .Must((inst, templateId) =>
                {
                    if (inst.SessionsSource == SessionsSource.Template)
                    {
                        return templateId != null && templateId > 0;
                    }

                    return true;
                })
                .WithMessage("Session template must be set");

            //rule for individual sessions
            var sessionValidator = new SessionValidator();
            RuleForEach(x => x.Sessions).SetValidator(sessionValidator);

            //rule for the sessions combined (check for overlaps)
            RuleFor(x => x.Sessions).Custom((sessions, ctx) =>
            {
                if (!MyUtils.ValidateSessions(sessions?.ToList<ISession>(), out List<string> errors))
                {
                    foreach (var error in errors)
                    {
                        ctx.AddFailure("Sessions", error);
                    }
                }
            });

            //Continuous future stuff
            RuleFor(req => req.IsContinuousFuture)
                .Must((inst, isCont) =>
                    {
                        if (isCont) return inst.Type == InstrumentType.Future;
                        return true;
                    })
                .WithMessage("Continuous futures must have Type set to Future");

            RuleFor(req => req.IsContinuousFuture)
                .Must((inst, isCont) =>
                {
                    if (isCont) return inst.ContinuousFuture != null;
                    return true;
                })
                .WithMessage("Must have continuous future object for continuous futures");
        }
    }
}