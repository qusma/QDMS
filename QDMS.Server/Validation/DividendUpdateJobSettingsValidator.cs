// -----------------------------------------------------------------------
// <copyright file="DividendUpdateJobSettingsValidator.cs" company="">
// Copyright 2017 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace QDMS.Server.Validation
{
    public class DividendUpdateJobSettingsValidator : AbstractValidator<DividendUpdateJobSettings>
    {
        public DividendUpdateJobSettingsValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name must have a value");

            RuleFor(x => x.BusinessDaysAhead).GreaterThanOrEqualTo(0).WithMessage("Must be >= 0");
            RuleFor(x => x.BusinessDaysBack).GreaterThanOrEqualTo(0).WithMessage("Must be >= 0");

            RuleFor(x => x.Tag)
                .Must((job, tag) => !job.UseTag || job.TagID != null)
                .WithMessage("Must set tag");
        }
    }
}
