// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations;

namespace HtmlGenerationWebSite.Models;

public class ValidationProviderAttributeModel
{
    [FirstName]
    public string FirstName { get; set; }

    [StringLength(maximumLength: 6)]
    [LastName]
    public string LastName { get; set; }
}

public class FirstNameAttribute : ValidationProviderAttribute
{
    public override IEnumerable<ValidationAttribute> GetValidationAttributes()
    {
        return new List<ValidationAttribute>
            {
                new RequiredAttribute(),
                new RegularExpressionAttribute(pattern: "[A-Za-z]*"),
                new StringLengthAttribute(maximumLength: 5)
            };
    }
}

public class LastNameAttribute : ValidationProviderAttribute
{
    public override IEnumerable<ValidationAttribute> GetValidationAttributes()
    {
        return new List<ValidationAttribute>
            {
                new RequiredAttribute()
            };
    }
}
