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
            await ValidateAsynchronousOnlyAsync(validationAttributes, value, context, state, onValidationError, cancellationToken);
        }
    }

    private static bool ValidateSynchronousOnly<TState>(
        ValidationAttribute[] validationAttributes,
        object? value,
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        ValidateContext context,
        TState state,
        Action<ValidateContext, ValidationResult, ValidationAttribute, TState> onValidationError)
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
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
        List<Task>? validationResultTasks = null;

        for (var i = 0; i < validationAttributes.Length; i++)
        {
            var attribute = validationAttributes[i];
            if (attribute is not AsyncValidationAttribute asyncValidationAttribute)
            {
                continue;
            }

            validationResultTasks ??= new();
            validationResultTasks.Add(
                GetValidationResultTaskCoreAsync(asyncValidationAttribute, value, context, state, onValidationError, cancellationToken));
        }

        if (validationResultTasks is not null)
        {
            await Task.WhenAll(validationResultTasks);
        }
    }

    private static async Task GetValidationResultTaskCoreAsync<TState>(
        AsyncValidationAttribute attribute,
        object? value,
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        ValidateContext context,
        TState state,
        Action<ValidateContext, ValidationResult, ValidationAttribute, TState> onValidationError,
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        CancellationToken cancellationToken)
    {
        // TODO: Discuss if we want to force yielding.
        // await Task.CompletedTask.ConfigureAwait(ConfigureAwaitOptions.ForceYielding);
        // The difference between that it introduces is, if the async validation attribute has some synchronous work at the beginning.
        // Do we want those "initial" synchronous work among different async validation attributes to be run in parallel or not?
        var result = await attribute.GetValidationResultAsync(value, context.ValidationContext, cancellationToken);
        if (result is not null && result != ValidationResult.Success)
        {
            onValidationError(context, result, attribute, state);
        }
    }
}
