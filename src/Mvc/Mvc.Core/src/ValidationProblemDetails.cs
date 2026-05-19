// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// A <see cref="ProblemDetails"/> for validation errors.
/// </summary>
public class ValidationProblemDetails : HttpValidationProblemDetails
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationProblemDetails"/>.
    /// </summary>
    public ValidationProblemDetails()
    {
        Title = Resources.ValidationProblemDescription_Title;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationProblemDetails"/> using the specified <paramref name="modelState"/>.
    /// </summary>
    /// <param name="modelState"><see cref="ModelStateDictionary"/> containing the validation errors.</param>
    public ValidationProblemDetails(ModelStateDictionary modelState)
        : base(CreateErrorDictionary(modelState))
    {
    }

    private static IDictionary<string, string[]> CreateErrorDictionary(ModelStateDictionary modelState)
    {
        ArgumentNullException.ThrowIfNull(modelState);

        var errorDictionary = new Dictionary<string, string[]>(StringComparer.Ordinal);

        foreach (var keyModelStatePair in modelState)
        {
            var key = keyModelStatePair.Key;
            var errors = keyModelStatePair.Value.Errors;
            if (errors != null && errors.Count > 0)
            {
                if (errors.Count == 1)
                {
                    var errorMessage = GetErrorMessage(errors[0]);
                    errorDictionary.Add(key, new[] { errorMessage });
                }
                else
                {
                    var errorMessages = new string[errors.Count];
                    for (var i = 0; i < errors.Count; i++)
                    {
                        errorMessages[i] = GetErrorMessage(errors[i]);
                    }

                    errorDictionary.Add(key, errorMessages);
                }
            }
        }

        return errorDictionary;

        static string GetErrorMessage(ModelError error)
        {
            return string.IsNullOrEmpty(error.ErrorMessage) ?
                Resources.SerializableError_DefaultError :
                error.ErrorMessage;
        }
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationProblemDetails"/> using the specified <paramref name="errors"/>.
    /// </summary>
    /// <param name="errors">The validation errors.</param>
    public ValidationProblemDetails(IDictionary<string, string[]> errors)
        : base(errors)
    {
    }

    /// <summary>
    /// Gets the validation errors associated with this instance of <see cref="HttpValidationProblemDetails"/>.
    /// </summary>
    [JsonPropertyName("errors")]
    public new IDictionary<string, string[]> Errors { get { return base.Errors; } set { base.Errors = value; } }
}
