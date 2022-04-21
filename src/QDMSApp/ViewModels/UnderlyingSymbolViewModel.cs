// -----------------------------------------------------------------------
// <copyright file="UnderlyingSymbolViewModel.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using QDMS;
using QDMS.Server.Validation;
using ReactiveUI;
using System;

namespace QDMSApp.ViewModels
{
    public class UnderlyingSymbolViewModel : ValidatingViewModelBase<UnderlyingSymbol>
    {
        /// <summary>
        /// design-time usage only
        /// </summary>
        [Obsolete]
        public UnderlyingSymbolViewModel() : base(null, null) { }

        public UnderlyingSymbolViewModel(UnderlyingSymbol model) : base(model, new UnderlyingSymbolValidator())
        {
        }

        public string Symbol
        {
            get { return Model.Symbol; }
            set { Model.Symbol = value; this.RaisePropertyChanged(); }
        }

        public ExpirationRule Rule
        {
            get { return Model.Rule; }
            set { Model.Rule = value; this.RaisePropertyChanged(); }
        }

        public ReferenceDayType ReferenceDayType { get; set; }
    }
}