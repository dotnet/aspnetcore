// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace BlazorUnitedApp.Validation;

// Demo model for the async validation page. Exercises:
//   - per-field async validation (AsyncAvailabilityAttribute on Username),
//   - per-field async validation that faults (AsyncFaultingAttribute on Coupon),
//   - synchronous validation for contrast (EmailAddress on Email),
//   - form-level async validation (IAsyncValidatableObject.ValidateAsync, runs on submit).
//
// The app does not call AddValidation(), so DataAnnotationsValidator uses the static Validator
// path, which now supports AsyncValidationAttribute / IAsyncValidatableObject via the async APIs.
public class AsyncRegistrationModel : IAsyncValidatableObject
{
    [Required(ErrorMessage = "Username is required.")]
    [AsyncAvailability]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Coupon code is required.")]
    [AsyncFaulting]
    public string? Coupon { get; set; }

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string? Email { get; set; }

    // The synchronous half of the contract is unused; the framework calls ValidateAsync via the
    // asynchronous pipeline. DataAnnotations only runs object-level validation once every field is
    // individually valid, so the form-level check below runs after the per-field checks pass.
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => Enumerable.Empty<ValidationResult>();

    // Form-level asynchronous check. Runs on submit (Validator.TryValidateObjectAsync). Simulates a
    // server-side rule: the username "reserved" passes the per-field availability check but is
    // rejected here.
    public async IAsyncEnumerable<ValidationResult> ValidateAsync(
        ValidationContext validationContext,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Simulated server round-trip so the form-level "pending" state is observable on submit.
        await Task.Delay(1500, cancellationToken);

        if (string.Equals(Username, "reserved", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult(
                "The username 'reserved' is not available (form-level async check).",
                new[] { nameof(Username) });
        }
    }
}

// Per-field async availability check (simulated remote lookup). Invalid for a fixed set of taken names.
[AttributeUsage(AttributeTargets.Property)]
public sealed class AsyncAvailabilityAttribute : AsyncValidationAttribute
{
    private static readonly string[] _taken = ["admin", "root", "taken"];

    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        // Simulate network latency so the per-field "pending" UX is observable. If the field is
        // re-edited or the form is submitted, our token is cancelled and this throws, which the
        // framework treats as silent supersession.
        await Task.Delay(1500, cancellationToken);

        var text = value as string;
        if (!string.IsNullOrEmpty(text) && _taken.Contains(text, StringComparer.OrdinalIgnoreCase))
        {
            return new ValidationResult($"'{text}' is already taken.", new[] { validationContext.MemberName! });
        }

        return ValidationResult.Success;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException("This attribute only supports asynchronous validation.");
}

// Per-field async check that simulates an infrastructure fault for a specific value, so the
// "faulted" UX can be exercised. Throws for "error", reports invalid for "bad", otherwise valid.
[AttributeUsage(AttributeTargets.Property)]
public sealed class AsyncFaultingAttribute : AsyncValidationAttribute
{
    protected override async Task<ValidationResult?> IsValidAsync(object? value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        await Task.Delay(1200, cancellationToken);

        var text = value as string;
        if (string.Equals(text, "error", StringComparison.OrdinalIgnoreCase))
        {
            // Simulate the validation service being unavailable. The framework contains this and
            // surfaces it as a faulted field (IsValidationFaulted), not as a validation message.
            throw new InvalidOperationException("The validation service is unavailable. Please try again.");
        }

        if (string.Equals(text, "bad", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult("Coupon 'bad' is not valid.", new[] { validationContext.MemberName! });
        }

        return ValidationResult.Success;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        => throw new NotSupportedException("This attribute only supports asynchronous validation.");
}
