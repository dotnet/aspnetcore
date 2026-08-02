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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates.

namespace BasicTestApp.FormsTest;

// Async per-field validation attribute used by the DataAnnotations async E2E components.
// It awaits the test controlled AsyncValidationGate when one is registered, so the pending to settled
// transition is deterministic.
// The field value selects the outcome:
// "error" -> throws (surfaced as a faulted field), "taken" -> invalid, anything else -> valid.
public sealed class AsyncAvailabilityAttribute : AsyncValidationAttribute
{
    protected override async Task<ValidationResult> IsValidAsync(object value, ValidationContext validationContext, CancellationToken cancellationToken)
    {
        await AsyncRegistrationModelBase.WaitForGateOrYieldAsync(validationContext, cancellationToken);

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
        await WaitForGateOrYieldAsync(validationContext, cancellationToken);

        if (string.Equals(Username, "reserved", StringComparison.Ordinal))
        {
            yield return new ValidationResult("Username is reserved", new[] { nameof(Username) });
        }
    }

    // Interactive hosts register AsyncValidationGate so the test controls when validation settles.
    // When it is absent (for example under static SSR) the validators complete on their own.
    internal static async Task WaitForGateOrYieldAsync(ValidationContext validationContext, CancellationToken cancellationToken)
    {
        if (validationContext.GetService(typeof(AsyncValidationGate)) is AsyncValidationGate gate)
        {
            await gate.WaitAsync(cancellationToken);
        }
        else
        {
            await Task.Yield();
        }
    }
}

// Not registered with any IValidatableInfoResolver, so EnableDataAnnotationsValidation falls back to
// the static System.ComponentModel.DataAnnotations.Validator path (Validator.TryValidate*Async).
public sealed class ValidatorPathModel : AsyncRegistrationModelBase
{
}

// Resolved by the validation source generator, so EnableDataAnnotationsValidation uses the
// Microsoft.Extensions.Validation (MEV) path (IValidatableTypeInfo.ValidateAsync).
[Microsoft.Extensions.Validation.ValidatableType]
public sealed class MevPathModel : AsyncRegistrationModelBase
{
}

// Bridges the source-generated resolver across assemblies. MevPathModel is annotated
// [ValidatableType] in THIS assembly, so BasicTestApp's generated AddValidation interceptor emits a
// resolver that knows MevPathModel (and its base). Components.TestServer cannot itself source-generate
// a resolver for a type defined here, so it registers this adapter, which reuses the generated resolver.
public sealed class AsyncValidationResolver : IValidatableInfoResolver
{
    private readonly IValidatableInfoResolver _generatedResolver;

    public AsyncValidationResolver()
    {
        var services = new ServiceCollection();

        // Intercepted by BasicTestApp's generated AddValidation, which inserts the generated resolver
        // at the front of Resolvers.
        services.AddValidation();

        var options = services.BuildServiceProvider()
            .GetRequiredService<IOptions<ValidationOptions>>().Value;

        // The generated resolver is inserted at index 0 by the interceptor; grab it and delegate to it.
        _generatedResolver = options.Resolvers[0];
    }

    public bool TryGetValidatableTypeInfo(Type type, [NotNullWhen(true)] out IValidatableTypeInfo validatableTypeInfo)
        => _generatedResolver.TryGetValidatableTypeInfo(type, out validatableTypeInfo);

    public bool TryGetValidatableParameterInfo(ParameterInfo parameterInfo, [NotNullWhen(true)] out IValidatableParameterInfo validatableParameterInfo)
        => _generatedResolver.TryGetValidatableParameterInfo(parameterInfo, out validatableParameterInfo);
}
