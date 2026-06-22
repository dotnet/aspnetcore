// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Validation;

[Experimental("ASP0029", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
internal static class ValidationHelpers
{
    internal static async Task ValidateAttributesAsync(
        object? value,
        object? container,
        IValidationErrorReporter reporter,
        ValidateContext context,
        CancellationToken cancellationToken)
    {
        var validationAttributes = reporter.GetValidationAttributes();
        if (ValidateSynchronousOnly(validationAttributes, value, container, reporter, context))
        {
            // Only validate async attributes if synchronous validation passed.
            await ValidateAsynchronousOnlyAsync(validationAttributes, value, container, reporter, context, cancellationToken);
        }
    }

    private static bool ValidateSynchronousOnly(
        ValidationAttribute[] validationAttributes,
        object? value,
        object? container,
        IValidationErrorReporter reporter,
        ValidateContext context)
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
                reporter.ReportError(context, container, attribute, result);
            }
        }

        return !hasErrors;
    }

    private static async Task ValidateAsynchronousOnlyAsync(
        ValidationAttribute[] validationAttributes,
        object? value,
        object? container,
        IValidationErrorReporter reporter,
        ValidateContext context,
        CancellationToken cancellationToken)
    {
        CancellationTokenSource? linkedCts = null;
        try
        {
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
                    GetValidationResultTaskCoreAsync(asyncValidationAttribute, value, container, reporter, context, cancellationToken, linkedCts));
            }

            if (validationResultTasks is not null)
            {
                await Task.WhenAll(validationResultTasks);
            }
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
        CancellationToken originalCancellationToken,
        CancellationTokenSource linkedCancellationTokenSource)
    {
        // originalCancellationToken is the cancellation token passed to ValidateAttributesAsync.
        // linkedCancellationToken is a LinkedCancellationToken that combines:
        // 1. the original cancellation token, and
        // 2. cancellation when we want to short-circuit on first error.
        try
        {
            var result = await attribute.GetValidationResultAsync(value, context.ValidationContext, linkedCancellationTokenSource.Token);
            if (result is not null && result != ValidationResult.Success)
            {
                reporter.ReportError(context, container, attribute, result);
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
