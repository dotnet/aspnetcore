// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
internal static class ValidationHelpers
{
    internal static async Task ValidateAttributesAsync<TState>(
        ValidationAttribute[] validationAttributes,
        object? value,
        ValidateContext context,
        TState state,
        Action<ValidateContext, ValidationResult, ValidationAttribute, TState> onValidationError,
        CancellationToken cancellationToken)
    {
        if (ValidateSynchronousOnly(validationAttributes, value, context, state, onValidationError))
        {
            // Only validate async attributes if synchronous validation passed.
            await ValidateAsynchronousOnlyAsync(validationAttributes, value, context.Clone(), state, onValidationError, cancellationToken);
        }
    }

    private static bool ValidateSynchronousOnly<TState>(
        ValidationAttribute[] validationAttributes,
        object? value,
        ValidateContext context,
        TState state,
        Action<ValidateContext, ValidationResult, ValidationAttribute, TState> onValidationError)
    {
        bool hasErrors = false;
        for (var i = 0; i < validationAttributes.Length; i++)
        {
            var attribute = validationAttributes[i];

            if (attribute is AsyncValidationAttribute)
            {
                continue;
            }

            var result = attribute.GetValidationResult(value, context.ValidationContext);
            if (result is not null && result != ValidationResult.Success)
            {
                hasErrors = true;
                onValidationError(context, result, attribute, state);
            }
        }

        return !hasErrors;
    }

    private static async Task ValidateAsynchronousOnlyAsync<TState>(
        ValidationAttribute[] validationAttributes,
        object? value,
        ValidateContext context,
        TState state,
        Action<ValidateContext, ValidationResult, ValidationAttribute, TState> onValidationError,
        CancellationToken cancellationToken)
    {
        CancellationTokenSource? linkedCts = null;
        List<Task>? validationResultTasks = null;

        for (var i = 0; i < validationAttributes.Length; i++)
        {
            var attribute = validationAttributes[i];
            if (attribute is not AsyncValidationAttribute asyncValidationAttribute)
            {
                continue;
            }

            linkedCts ??= CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            validationResultTasks ??= new();
            validationResultTasks.Add(
                GetValidationResultTaskCoreAsync(asyncValidationAttribute, value, context, state, onValidationError, cancellationToken, linkedCts.Token));
        }

        if (validationResultTasks is not null)
        {
            await Task.WhenAll(validationResultTasks);
        }
    }

    private static async Task GetValidationResultTaskCoreAsync<TState>(
        AsyncValidationAttribute attribute,
        object? value,
        ValidateContext context,
        TState state,
        Action<ValidateContext, ValidationResult, ValidationAttribute, TState> onValidationError,
        CancellationToken originalCancellationToken,
        CancellationToken linkedCancellationToken)
    {
        // originalCancellationToken is the cancellation token passed to ValidateAttributesAsync.
        // linkedCancellationToken is a LinkedCancellationToken that combines:
        // 1. the original cancellation token, and
        // 2. cancellation when we want to short-circuit on first error.
        try
        {
            var result = await attribute.GetValidationResultAsync(value, context.ValidationContext, linkedCancellationToken);
            if (result is not null && result != ValidationResult.Success)
            {
                onValidationError(context, result, attribute, state);
            }
        }
        catch (OperationCanceledException) when (linkedCancellationToken.IsCancellationRequested && !originalCancellationToken.IsCancellationRequested)
        {
            // If the original token wasn't cancelled, but ours is cancelled, it means we cancelled to short-circuit.
            // In this case, we want to just ignore this cancellation.
        }
    }
}
