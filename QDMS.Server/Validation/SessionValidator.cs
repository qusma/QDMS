// -----------------------------------------------------------------------
// <copyright file="SessionValidator.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace QDMS.Server.Validation
{
    public class SessionValidator : AbstractValidator<ISession>
    {
        public SessionValidator()
        {
            RuleFor(x => x).Must(x =>
            {
                if (x.OpeningDay == x.ClosingDay)
                {
                    return x.OpeningTime < x.ClosingTime;
                }

                return true;
            }).WithMessage("Opening time must be before closing time");
        }
    }
}