// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class CompanyNameAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var valueString = value as string;
        if (string.IsNullOrEmpty(valueString))
        {
            return new ValidationResult("CompanyName cannot be null or empty.");
        }

        return null;
    }
}
