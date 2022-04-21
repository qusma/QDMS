// -----------------------------------------------------------------------
// <copyright file="EconomicReleaseUpdateJobSettingsValidator.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace QDMS.Server.Validation
{
    public class EconomicReleaseUpdateJobSettingsValidator : AbstractValidator<EconomicReleaseUpdateJobSettings>
    {
        public EconomicReleaseUpdateJobSettingsValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name must have a value");
            RuleFor(x => x.BusinessDaysAhead).GreaterThanOrEqualTo(0).WithMessage("Must be > 0");
            RuleFor(x => x.BusinessDaysBack).GreaterThanOrEqualTo(0).WithMessage("Must be > 0");
        }
    }
}