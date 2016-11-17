// -----------------------------------------------------------------------
// <copyright file="SessionTemplateValidator.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace QDMS.Server.Validation
{
    public class SessionTemplateValidator : AbstractValidator<SessionTemplate>
    {
        public SessionTemplateValidator()
        {
            RuleFor(req => req.Name).NotEmpty().WithMessage("Must have a name");
        }
    }
}