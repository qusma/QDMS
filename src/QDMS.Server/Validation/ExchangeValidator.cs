// -----------------------------------------------------------------------
// <copyright file="ExchangeValidator.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;
using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QDMS.Server.Validation
{
    public class ExchangeValidator : AbstractValidator<Exchange>
    {
        public ExchangeValidator()
        {
            RuleFor(req => req.Name).NotEmpty().WithMessage("Name must have a value");
            RuleFor(req => req.LongName).NotEmpty().WithMessage("LongName must have a value");
            //accepts both Windows and IANA ids, since existing data uses Windows ids
            RuleFor(req => req.Timezone).Must(x => x != null && TimeZoneInfo.TryFindSystemTimeZoneById(x, out _)).WithMessage("Timezone does not exist");

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
        }
    }
}