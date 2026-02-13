// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0029

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using ConsoleValidationSample.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Validation;

namespace ConsoleValidationSample.Models;

[ValidatableType]
public class InventoryItem : IValidatableObject
{
    private const int MinPremiumPrice = 100;

    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    public bool IsPremium { get; set; }

    [Range(0, int.MaxValue)]
    [Display(ResourceType = typeof(InventoryItemLabels), Name = "Price")]
    public int Price { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsPremium || Price >= MinPremiumPrice)
        {
            return [];
        }

        var localizerFactory = validationContext.GetRequiredService<IStringLocalizerFactory>();
        var localizer = localizerFactory.Create(typeof(ValidationMessages));
        var errorMessage = localizer["Premium items must have price set to at least {0}", MinPremiumPrice];

        return [new ValidationResult(errorMessage, [nameof(IsPremium), nameof(Price)])];
    }
}

internal static class InventoryItemLabels
{
    public static string Price => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "es" => "precio",
        _ => "price"
    };
}
