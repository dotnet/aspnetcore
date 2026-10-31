// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class ProductValidatorAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var product = value as ProductViewModel;
        if (product != null)
        {
            if (!product.Country.Equals("USA") || string.IsNullOrEmpty(product.Name))
            {
                return new ValidationResult("Product must be made in the USA if it is not named.");
            }
            else
            {
                return null;
            }
        }
        var software = value as SoftwareViewModel;
        if (software != null)
        {
            if (!software.Country.Equals("USA") || string.IsNullOrEmpty(software.Name))
            {
                return new ValidationResult("Product must be made in the USA if it is not named.");
            }
            else
            {
                return null;
            }
        }

        return new ValidationResult("Expected either ProductViewModel or SoftwareViewModel instance but got "
            + value.GetType() + " instance");
    }
}
