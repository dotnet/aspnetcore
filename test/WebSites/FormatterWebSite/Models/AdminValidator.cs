// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace FormatterWebSite
{
    public class AdminValidator : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var admin = (Administrator)value;
            
            if (admin.AdminAccessCode != 1)
            {
                return new ValidationResult ("AdminAccessCode property does not have the right value");
            }

            return null;
        }
    }
}