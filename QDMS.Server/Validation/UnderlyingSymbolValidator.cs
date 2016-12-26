// -----------------------------------------------------------------------
// <copyright file="UnderlyingSymbolValidator.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace QDMS.Server.Validation
{
    public class UnderlyingSymbolValidator : AbstractValidator<UnderlyingSymbol>
    {
        public UnderlyingSymbolValidator()
        {
            RuleFor(x => x.Symbol).NotEmpty().WithMessage("Symbol must have a value");
        }
    }
}
