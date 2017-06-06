// -----------------------------------------------------------------------
// <copyright file="SessionTemplateValidator.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;

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
            AddRule(new DelegateValidator<SessionTemplate>((template, ctx) =>
            {
                List<string> errors;
                if (!MyUtils.ValidateSessions(template.Sessions?.ToList<ISession>(), out errors))
                {
                    return errors.Select(x => new ValidationFailure("Sessions", x));
                }

                return Enumerable.Empty<ValidationFailure>();
            }));
        }
    }
}