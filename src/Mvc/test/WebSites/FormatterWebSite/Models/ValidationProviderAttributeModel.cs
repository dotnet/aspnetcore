// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations;

namespace FormatterWebSite
{
    public class ValidationProviderAttributeModel
    {
        [FirstName]
        public string FirstName { get; set; }

        [StringLength(maximumLength: 5)]
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
}
