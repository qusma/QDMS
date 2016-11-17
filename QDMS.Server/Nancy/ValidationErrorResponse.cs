// -----------------------------------------------------------------------
// <copyright file="ValidationErrorResponse.cs" company="">
// Copyright 2016 Alexander Soffronow Pagonidis
// </copyright>
// -----------------------------------------------------------------------

using Nancy.Validation;
using System.Collections.Generic;

namespace QDMS.Server.Nancy
{
    public class ValidationErrorResponse
    {
        public List<string> ErrorMessages { get; set; }

        public ValidationErrorResponse()
        {
        }

        public ValidationErrorResponse(ModelValidationResult result)
        {
            ErrorMessages = new List<string>();

            foreach (var group in result.Errors)
            {
                foreach (var error in group.Value)
                {
                    ErrorMessages.Add($"{group.Key}: {error.ErrorMessage}");
                }
            }
        }

        public ValidationErrorResponse(string error)
        {
            ErrorMessages = new List<string> { error };
        }
    }
}