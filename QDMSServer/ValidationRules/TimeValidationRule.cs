// -----------------------------------------------------------------------
// <copyright file="TimeValidationRule.cs" company="">
// Copyright 2013 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Globalization;
using System.Windows.Controls;

namespace QDMSServer
{
    class TimeValidationRule : ValidationRule
    {
        /// <summary>
        /// When overridden in a derived class, performs validation checks on a value.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Windows.Controls.ValidationResult"/> object.
        /// </returns>
        /// <param name="value">The value from the binding target to check.</param><param name="cultureInfo">The culture to use in this rule.</param>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (value != null)
            {
                DateTime result;
                bool succeeded = DateTime.TryParse((string)value, out result);
                if (succeeded)
                {
                    return new ValidationResult(true, null);
                }
                else
                {
                    return new ValidationResult(false, "Could not parse time.");
                }
            }

            return new ValidationResult(false, "Null not allowed.");
        }
    }
}
