// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using BlazorUnitedApp.Localization;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Validation;

namespace BlazorUnitedApp.Models;

// Type-level attribute that runs after property validation. Demonstrates the
// "type-level ValidationAttribute" path through ValidatableTypeInfo.ValidateTypeAttributes.
// Localization is automatic because we set ErrorMessage as the lookup key.
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ParentalConsentRequiredAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is RegistrationModel model
            && model.Age < 18
            && string.Equals(model.Country, "DE", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(
                ErrorMessage ?? "Parental consent required.",
                [nameof(RegistrationModel.Age), nameof(RegistrationModel.Country)]);
        }

        return ValidationResult.Success;
    }
}

// Custom property-level attribute. Localization works through the standard
// ErrorMessage-as-key pipeline (no IValidationAttributeFormatter needed because
// the template only uses {0}).
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class StrongPasswordAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string s)
        {
            return true;
        }

        return s.Any(char.IsDigit) && s.Any(c => !char.IsLetterOrDigit(c));
    }
}

// Validatable model registered via [ValidatableType] so the source generator
// emits the metadata. Each property uses [Display(Name = "<key>")] for a
// localizable display name and [<Validator>(ErrorMessage = "<key>")] for a
// localizable error template.
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates.
[Microsoft.Extensions.Validation.ValidatableType]
#pragma warning restore ASP0029
[ParentalConsentRequired(ErrorMessage = "Validation.RegistrationConsent")]
public class RegistrationModel : IValidatableObject
{
    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(60, MinimumLength = 2, ErrorMessage = "Validation.StringLengthRange")]
    [Display(Name = "Registration.FullName")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation.Required")]
    [EmailAddress(ErrorMessage = "Validation.Email")]
    [Display(Name = "Registration.Email")]
    public string Email { get; set; } = string.Empty;

    [Range(13, 120, ErrorMessage = "Validation.Range")]
    [Display(Name = "Registration.Age")]
    public int Age { get; set; }

    [Required(ErrorMessage = "Validation.Required")]
    [RegularExpression("^[A-Z]{2}$", ErrorMessage = "Validation.RegExp")]
    [Display(Name = "Registration.Country")]
    public string Country { get; set; } = string.Empty;

    [Required(ErrorMessage = "Validation.Required")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "Validation.StringLengthRange")]
    [StrongPassword(ErrorMessage = "Validation.PasswordPolicy")]
    [Display(Name = "Registration.Password")]
    public string Password { get; set; } = string.Empty;

    // Compare reads the OtherProperty's [Display] attribute and uses its raw Name as
    // OtherPropertyDisplayName, which is then NOT routed back through IValidationLocalizer.
    // The error message therefore renders the literal lookup key ("Registration.Password")
    // instead of the translated value. To localize that placeholder you would register
    // a custom IValidationAttributeFormatter for CompareAttribute that resolves the key
    // through IStringLocalizer in its FormatErrorMessage call.
    [Compare(nameof(Password), ErrorMessage = "Validation.Compare")]
    [Display(Name = "Registration.ConfirmPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;

    // IValidatableObject results are deliberately NOT routed through IValidationLocalizer
    // (per the design: messages here are produced by user code that already has access
    // to ValidationContext.GetService<IStringLocalizer>). This sample uses that escape
    // hatch to reuse the same shared resource as everything else.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Demonstrate cross-property validation that doesn't fit a single attribute:
        // FullName must not be the same as the part before @ in Email.
        if (!string.IsNullOrEmpty(FullName)
            && !string.IsNullOrEmpty(Email)
            && Email.Contains('@')
            && string.Equals(
                FullName.Trim(),
                Email[..Email.IndexOf('@')],
                StringComparison.OrdinalIgnoreCase))
        {
            var localizer = validationContext.GetService(typeof(IStringLocalizer<SharedValidationMessages>))
                as IStringLocalizer<SharedValidationMessages>;

            yield return new ValidationResult(
                localizer is null
                    ? "Full name and the local part of the email address must differ."
                    : localizer["Validation.RegistrationNameVsEmail"].Value,
                [nameof(FullName)]);
        }
    }
}
