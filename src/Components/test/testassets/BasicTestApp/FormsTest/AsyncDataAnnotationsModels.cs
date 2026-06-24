// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Validation;

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace BasicTestApp.FormsTest;

// Async per-field validation attribute used by the DataAnnotations async E2E components. It is
// genuinely asynchronous (Task.Yield) but completes promptly; the tests assert the settled outcome
// with polling rather than relying on a fixed delay. The field value selects the outcome:
//   "error" -> throws (surfaced as a faulted field), "taken" -> invalid, anything else -> valid.
public sealed class AsyncAvailabilityAttribute : AsyncValidationAttribute
{
    protected override async Task<ValidationResult> IsValidAsync(object value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        await Task.Yield();

        var text = value as string;
        if (string.Equals(text, "error", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Username service unavailable");
        }

        if (string.Equals(text, "taken", StringComparison.Ordinal))
        {
            return new ValidationResult("Username is taken", new[] { nameof(AsyncRegistrationModelBase.Username) });
        }

        return ValidationResult.Success;
    }

    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        => throw new NotSupportedException("This attribute only supports asynchronous validation.");
}

// Base model shared by the Validator-path and MEV-path components. The async [ValidationAttribute] on
// Username exercises the per-field async path; the IAsyncValidatableObject implementation exercises the
// form-level async path on submit ("reserved" passes the per-field check but is rejected by the form).
public abstract class AsyncRegistrationModelBase : IAsyncValidatableObject
{
    [Required(ErrorMessage = "Username is required.")]
    [AsyncAvailability]
    public string Username { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        => Enumerable.Empty<ValidationResult>();

    public async IAsyncEnumerable<ValidationResult> ValidateAsync(
        ValidationContext validationContext,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.Yield();

        if (string.Equals(Username, "reserved", StringComparison.Ordinal))
        {
            yield return new ValidationResult("Username is reserved", new[] { nameof(Username) });
        }
    }
}

// Not registered with any IValidatableInfoResolver, so EnableDataAnnotationsValidation falls back to
// the static System.ComponentModel.DataAnnotations.Validator path (Validator.TryValidate*Async).
public sealed class ValidatorPathModel : AsyncRegistrationModelBase
{
}

// Resolved by AsyncValidationResolver below, so EnableDataAnnotationsValidation uses the
// Microsoft.Extensions.Validation (MEV) path (IValidatableTypeInfo.ValidateAsync).
public sealed class MevPathModel : AsyncRegistrationModelBase
{
}

// Custom resolver that provides IValidatableTypeInfo for MevPathModel only. Registered via
// AddValidation in Program.cs. This routes MevPathModel through the MEV validation path without
// depending on the validation source generator (which BasicTestApp does not reference), while
// ValidatorPathModel remains unresolved and uses the static Validator path.
public sealed class AsyncValidationResolver : IValidatableInfoResolver
{
    public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableTypeInfo validatableTypeInfo)
    {
        if (type == typeof(MevPathModel))
        {
            validatableTypeInfo = new ModelTypeInfo(typeof(MevPathModel),
            [
                new ModelPropertyInfo(
                    typeof(MevPathModel),
                    typeof(string),
                    nameof(MevPathModel.Username),
                    [new RequiredAttribute { ErrorMessage = "Username is required." }, new AsyncAvailabilityAttribute()]),
            ]);
            return true;
        }

        validatableTypeInfo = null;
        return false;
    }

    public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableParameterInfo validatableParameterInfo)
    {
        validatableParameterInfo = null;
        return false;
    }

    private sealed class ModelTypeInfo : ValidatableTypeInfo
    {
        public ModelTypeInfo(Type type, IReadOnlyList<ValidatablePropertyInfo> members)
            : base(type, members)
        {
        }

        protected override ValidationAttribute[] GetValidationAttributes() => [];
    }

    private sealed class ModelPropertyInfo : ValidatablePropertyInfo
    {
        private readonly ValidationAttribute[] _attributes;

        public ModelPropertyInfo(Type declaringType, Type propertyType, string name, ValidationAttribute[] attributes)
            : base(declaringType, propertyType, name, displayNameInfo: null)
        {
            _attributes = attributes;
        }

        protected override ValidationAttribute[] GetValidationAttributes() => _attributes;
    }
}
