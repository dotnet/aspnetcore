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

/// <summary>
/// Represents an inventory item demonstrating <see cref="IValidatableObject"/> validation,
/// <see cref="Display"/> with <see cref="Display.ResourceType"/>, and localized error messages.
/// </summary>
[ValidatableType]
public class InventoryItem : IValidatableObject
{
    private const int MinPremiumPrice = 100;
    private const string ErrorTemplate = "Premium items must have price set to at least {0}";

    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    public bool IsPremium { get; set; }

    [Range(0, int.MaxValue)]
    [Display(ResourceType = typeof(InventoryItemLabels), Name = "Price")]
    public int Price { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsPremium || Price >= MinPremiumPrice)
        {
            return [];
        }

        var localizerFactory = validationContext.GetService<IStringLocalizerFactory>();
        var localizer = localizerFactory?.Create(typeof(ValidationMessages));
        var errorMessage = localizer is not null
            ? localizer[ErrorTemplate, MinPremiumPrice]
            : string.Format(CultureInfo.InvariantCulture, ErrorTemplate, MinPremiumPrice);

        return [new ValidationResult(errorMessage, [nameof(IsPremium), nameof(Price)])];
    }
}

/// <summary>
/// Provides localized display labels for <see cref="InventoryItem"/> properties
/// via <see cref="Display.ResourceType"/>.
/// </summary>
internal static class InventoryItemLabels
{
    public static string Price => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
    {
        "es" => "Precio",
        _ => "Price"
    };
}
