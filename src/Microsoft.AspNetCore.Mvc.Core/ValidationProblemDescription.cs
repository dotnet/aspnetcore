// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// A <see cref="ProblemDescription"/> for validation errors.
    /// </summary>
    public class ValidationProblemDescription : ProblemDescription
    {
        /// <summary>
        /// Intializes a new instance of <see cref="ValidationProblemDescription"/>.
        /// </summary>
        public ValidationProblemDescription()
        {
            Title = Resources.ValidationProblemDescription_Title;
        }

        public ValidationProblemDescription(ModelStateDictionary modelState)
            : this()
        {
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            foreach (var keyModelStatePair in modelState)
            {
                var key = keyModelStatePair.Key;
                var errors = keyModelStatePair.Value.Errors;
                if (errors != null && errors.Count > 0)
                {
                    if (errors.Count == 1)
                    {
                        var errorMessage = GetErrorMessage(errors[0]);
                        Errors.Add(key, new[] { errorMessage });
                    }
                    else
                    {
                        var errorMessages = new string[errors.Count];
                        for (var i = 0; i < errors.Count; i++)
                        {
                            errorMessages[i] = GetErrorMessage(errors[i]);
                        }

                        Errors.Add(key, errorMessages);
                    }
                }
            }

            string GetErrorMessage(ModelError error)
            {
                return string.IsNullOrEmpty(error.ErrorMessage) ?
                    Resources.SerializableError_DefaultError :
                    error.ErrorMessage;
            }
        }

        /// <summary>
        /// Gets or sets the validation errors associated with this instance of <see cref="ValidationProblemDescription"/>.
        /// </summary>
        public IDictionary<string, string[]> Errors { get; } = new Dictionary<string, string[]>(StringComparer.Ordinal);
    }
}
