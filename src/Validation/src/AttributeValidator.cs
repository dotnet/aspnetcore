// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

/// <summary>
/// Provides helpers for executing <see cref="ValidationAttribute"/> instances against a value and
/// reporting the results through an <see cref="IValidationErrorReporter"/>. These helpers only use the
/// public surface of <see cref="ValidateContext"/> so that the validatable metadata types do not depend
/// on any internal <see cref="ValidateContext"/> APIs.
/// </summary>
[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
internal static class AttributeValidator
{
    [return: NotNullIfNotNull(nameof(objectInstance))]
    public static ValidationContext? CreateValidationContext(ValidateContext context, object? objectInstance, string displayName, string? memberName)
        => objectInstance is null
            ? null
            : new ValidationContext(objectInstance, displayName, context.ServiceProvider, null)
            {
                MemberName = memberName,
            };

    public static string? ResolveAttributeErrorMessage(
        ValidateContext context,
        string memberName,
        string displayName,
        Type? declaringType,
        ValidationAttribute attribute,
        ValidationResult result)
    {
        if (context.ValidationOptions.Localizer is null || attribute.ErrorMessageResourceType is not null)
        {
            return result.ErrorMessage;
        }

        var localizationContext = new ErrorMessageLocalizationContext
        {
            MemberName = memberName,
            DisplayName = displayName,
            DeclaringType = declaringType,
            Attribute = attribute,
        };

        return context.ValidationOptions.Localizer.ResolveErrorMessage(localizationContext) ?? result.ErrorMessage;
    }

    public static async Task ValidateAttributesAsync(
        ValidateContext context,
        object? value,
        object? container,
        IValidationErrorReporter reporter,
        ValidationContext validationContext,
        string displayName,
        CancellationToken cancellationToken)
    {
        // NOTE: In case there are no async validation attributes, there should be no performance impact.
        // The async state machine is a class only in Debug builds. But in Release it's a struct.
        // So it will be efficient.
        // And if this method completed synchronously because no async validation attributes exist, this
        // will returned the same cached instance as Task.CompletedTask.
        var validationAttributes = reporter.GetValidationAttributes();
        if (ValidateSynchronousOnly(validationAttributes, context, value, container, reporter, validationContext, displayName))
        {
            // Only validate async attributes if synchronous validation passed.
            await ValidateAsynchronousOnlyAsync(validationAttributes, context, value, container, reporter, validationContext, displayName, cancellationToken);
        }
    }

    public static void ValidateAllAttributesSynchronously(
        ValidateContext context,
        object? value,
        object? container,
        IValidationErrorReporter reporter,
        ValidationContext validationContext,
        string displayName)
    {
        var validationAttributes = reporter.GetValidationAttributes();
        for (var i = 0; i < validationAttributes.Length; i++)
        {
            var attribute = validationAttributes[i];

            var result = attribute.GetValidationResult(value, validationContext);
            if (result is not null && result != ValidationResult.Success)
            {
                reporter.ReportError(context, displayName, container, attribute, result);
            }
        }
    }

    private static bool ValidateSynchronousOnly(
        ValidationAttribute[] validationAttributes,
        ValidateContext context,
        object? value,
        object? container,
        IValidationErrorReporter reporter,
        ValidationContext validationContext,
        string displayName)
    {
        bool hasErrors = false;
        for (var i = 0; i < validationAttributes.Length; i++)
        {
            var attribute = validationAttributes[i];

            if (attribute is AsyncValidationAttribute)
            {
                continue;
            }

            var result = attribute.GetValidationResult(value, validationContext);
            if (result is not null && result != ValidationResult.Success)
            {
                hasErrors = true;
                reporter.ReportError(context, displayName, container, attribute, result);
            }
        }

        return !hasErrors;
    }

    private static async Task ValidateAsynchronousOnlyAsync(
        ValidationAttribute[] validationAttributes,
        ValidateContext context,
        object? value,
        object? container,
        IValidationErrorReporter reporter,
        ValidationContext validationContext,
        string displayName,
        CancellationToken cancellationToken)
    {
        CancellationTokenSource? linkedCts = null;
        try
        {
            var tracker = new AsyncValidationTracker(context);
            for (var i = 0; i < validationAttributes.Length; i++)
            {
                var attribute = validationAttributes[i];
                if (attribute is not AsyncValidationAttribute asyncValidationAttribute)
                {
                    continue;
                }

                linkedCts ??= CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                tracker.Track(
                    GetValidationResultTaskCoreAsync(asyncValidationAttribute, value, container, reporter, tracker.NextContext(), validationContext, displayName, cancellationToken, linkedCts));
            }

            await tracker.CompleteAsync();
        }
        finally
        {
            linkedCts?.Dispose();
        }
    }

    private static async Task GetValidationResultTaskCoreAsync(
        AsyncValidationAttribute attribute,
        object? value,
        object? container,
        IValidationErrorReporter reporter,
        ValidateContext context,
        ValidationContext validationContext,
        string displayName,
        CancellationToken originalCancellationToken,
        CancellationTokenSource linkedCancellationTokenSource)
    {
        // originalCancellationToken is the cancellation token passed to ValidateAttributesAsync.
        // linkedCancellationToken is a LinkedCancellationToken that combines:
        // 1. the original cancellation token, and
        // 2. cancellation when we want to short-circuit on first error.
        try
        {
            var result = await attribute.GetValidationResultAsync(value, validationContext, linkedCancellationTokenSource.Token);
            if (result is not null && result != ValidationResult.Success)
            {
                reporter.ReportError(context, displayName, container, attribute, result);
                linkedCancellationTokenSource.Cancel();
            }
        }
        catch (OperationCanceledException) when (linkedCancellationTokenSource.IsCancellationRequested && !originalCancellationToken.IsCancellationRequested)
        {
            // If the original token wasn't cancelled, but ours is cancelled, it means we cancelled to short-circuit.
            // In this case, we want to just ignore this cancellation.
        }
    }
}
