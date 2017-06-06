// -----------------------------------------------------------------------
// <copyright file="ValidatingViewModelBase.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;
using FluentValidation.Results;
using ReactiveUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace QDMSServer.ViewModels
{
    public abstract class ValidatingViewModelBase<T> : ReactiveObject, INotifyDataErrorInfo where T : class
    {
        public T Model { get; }
        private readonly AbstractValidator<T> _validator;
        private Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();
        private bool _hasErrors;

        protected ValidatingViewModelBase(T model, AbstractValidator<T> validator)
        {
            Model = model;
            _validator = validator;

            //we have to use pass-through properties for validation to work...
            //the issue is that INotifyPropertyChanged and INotifyDataErrorInfo must be in the same class
            //and I don't want to saddle the models with validating themselves...
            //it would require an additional library reference in QDMSClient, etc.
            this.PropertyChanged += (s, e) => Validate(e.PropertyName);
        }

        protected void Validate(string propertyName = null)
        {
            if (propertyName == nameof(HasErrors)) return;

            if (string.IsNullOrEmpty(propertyName))
            {
                ValidateObject();
            }
            else
            {
                ValidateProperty(propertyName);
            }
        }

        /// <summary>
        /// Validate only a particular property
        /// </summary>
        /// <param name="propertyName"></param>
        private void ValidateProperty(string propertyName)
        {
            var result = _validator.Validate(Model, propertyName);
            if (!_errors.ContainsKey(propertyName)) _errors.Add(propertyName, new List<string>());

            var newErrors = result.Errors.Select(x => x.ErrorMessage).ToList();

            _errors[propertyName] = newErrors;
            HasErrors = _errors.Any(x => x.Value.Count > 0);

            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Validate the entire object
        /// </summary>
        private void ValidateObject()
        {
            var result = _validator.Validate(Model);
            HasErrors = result.IsValid;

            var newErrors = GetErrorDict(result.Errors);
            var oldErrors = _errors;
            _errors = newErrors;
            NotifyChangedErrors(oldErrors, newErrors);
            HasErrors = _errors.Any(x => x.Value.Count > 0);
        }

        private void NotifyChangedErrors(Dictionary<string, List<string>> oldErrors, Dictionary<string, List<string>> newErrors)
        {
            var removed = oldErrors.Keys.Except(newErrors.Keys);
            var added = newErrors.Keys.Except(oldErrors.Keys);
            var changed = oldErrors.Keys.Intersect(newErrors.Keys).Where(x => oldErrors[x].Any(y => !newErrors[x].Contains(y)));
            var allChangedErrors = removed.Concat(changed).Concat(added);
            foreach (var prop in allChangedErrors)
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(prop));
            }
        }

        private Dictionary<string, List<string>> GetErrorDict(IList<ValidationFailure> validationFailures)
        {
            var dict = new Dictionary<string, List<string>>();

            foreach (var failure in validationFailures)
            {
                if (!dict.ContainsKey(failure.PropertyName))
                {
                    dict.Add(failure.PropertyName, new List<string>());
                }

                dict[failure.PropertyName].Add(failure.ErrorMessage);
            }

            return dict;
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (propertyName == null)
            {
                //it wants all the errors
                return _errors.SelectMany(x => x.Value).ToList();
            }

            List<string> failures;
            if (_errors != null && _errors.TryGetValue(propertyName, out failures))
            {
                return failures.ToList();
            }
            return null;
        }

        public bool HasErrors
        {
            get { return _hasErrors; }
            set { this.RaiseAndSetIfChanged(ref _hasErrors, value); }
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
    }
}