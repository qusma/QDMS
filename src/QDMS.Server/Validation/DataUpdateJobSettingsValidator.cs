// -----------------------------------------------------------------------
// <copyright file="DataUpdateJobSettingsValidator.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace QDMS.Server.Validation
{
    public class DataUpdateJobSettingsValidator : AbstractValidator<DataUpdateJobSettings>
    {
        public DataUpdateJobSettingsValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name must have a value");
            RuleFor(x => x.Instrument)
                .Must((job, inst) => job.UseTag || (job.Instrument != null && job.InstrumentID.HasValue))
                .WithMessage("Must set instrument");

            RuleFor(x => x.Tag)
                .Must((job, tag) => !job.UseTag || (job.Tag != null && job.TagID.HasValue))
                .WithMessage("Must set tag");
        }
    }
}
