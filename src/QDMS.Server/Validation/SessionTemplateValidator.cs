// -----------------------------------------------------------------------
// <copyright file="SessionTemplateValidator.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;
using FluentValidation.Results;
using System.Collections.Generic;
using System.Linq;

namespace QDMS.Server.Validation
{
    public class SessionTemplateValidator : AbstractValidator<SessionTemplate>
    {
        public SessionTemplateValidator()
        {
            RuleFor(req => req.Name).NotEmpty().WithMessage("Must have a name");

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