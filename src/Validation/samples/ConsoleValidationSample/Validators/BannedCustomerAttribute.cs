// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using ConsoleValidationSample.Models;
using ConsoleValidationSample.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace ConsoleValidationSample.Validators;

[AttributeUsage(AttributeTargets.Class)]
public class BannedCustomerAttribute(string bannedName) : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (validationContext.ObjectInstance is Customer customer)
        {
            if (customer.Name == bannedName)
            {
                var localizerFactory = validationContext.GetRequiredService<IStringLocalizerFactory>();
                var localizer = localizerFactory.Create(typeof(ValidationMessages));
                var errorMessage = localizer["The customer {0} is banned", bannedName];

                return new ValidationResult(errorMessage, [nameof(Customer.Name)]);
            }
        }

        return ValidationResult.Success;
    }
}
