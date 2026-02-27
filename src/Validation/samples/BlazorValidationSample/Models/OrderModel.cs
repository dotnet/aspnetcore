// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASP0029

using System.ComponentModel.DataAnnotations;
using System.Globalization;
using BlazorValidationSample.Resources;
using BlazorValidationSample.Validators;
using Microsoft.Extensions.Localization;
namespace BlazorValidationSample.Models;

/// <summary>
/// Represents an order demonstrating <see cref="IValidatableObject"/> custom validation,
/// the custom <see cref="FutureDateAttribute"/>, and localized error messages.
/// </summary>
[Microsoft.Extensions.Validation.ValidatableType]
public class OrderModel : IValidatableObject
{
    private const int MinPremiumQuantity = 10;
    private const string PremiumQuantityError = "PremiumQuantityError";

    [Range(1, int.MaxValue, ErrorMessage = "RangeError")]
    [Display(Name = "OrderId")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "RequiredError")]
    [StringLength(200, ErrorMessage = "StringLengthError")]
    [Display(Name = "ProductName")]
    public string? ProductName { get; set; }

    [Range(1, 1000, ErrorMessage = "RangeError")]
    [Display(Name = "Quantity")]
    public int Quantity { get; set; }

    [FutureDate(ErrorMessage = "FutureDateError")]
    [Display(Name = "DeliveryDate")]
    public DateTime DeliveryDate { get; set; }

    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProductName is not null &&
            ProductName.StartsWith("Premium", StringComparison.OrdinalIgnoreCase) &&
            Quantity < MinPremiumQuantity)
        {
            var localizerFactory = validationContext.GetService<IStringLocalizerFactory>();
            var localizer = localizerFactory?.Create(typeof(ValidationMessages));
            var errorMessage = localizer is not null
                ? localizer[PremiumQuantityError, MinPremiumQuantity].Value
                : string.Format(CultureInfo.InvariantCulture, "Premium products require a minimum quantity of {0}.", MinPremiumQuantity);

            yield return new ValidationResult(errorMessage, [nameof(Quantity), nameof(ProductName)]);
        }
    }
}
