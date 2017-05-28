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
            RuleFor(x => x.ClosingTime).Must((session,closingTime) =>
            {
                if (session.OpeningDay == session.ClosingDay)
                {
                    return session.OpeningTime < closingTime;
                }

                return true;
            }).WithMessage("Opening time must be before closing time");
        }
    }
}