// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

internal interface IValidationErrorReporter
{
    ValidationAttribute[] GetValidationAttributes();

    [Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
    void ReportError(ValidateContext context, object? container, ValidationAttribute attribute, ValidationResult result);
}
