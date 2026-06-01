// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace Microsoft.Extensions.Validation;

internal static class ValidationHelpers
{
    private enum ValidationMode
    {
        SyncOnly,
        AsyncOnly,
    }

    internal static async Task ValidateAttributesAsync<TState>(
        ValidationAttribute[] validationAttributes,
        object? value,
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        ValidateContext context,
        TState state,
        Action<ValidateContext, ValidationResult, ValidationAttribute, TState> onValidationError,
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        CancellationToken cancellationToken)
    {
        await ValidateAttributesAsync(validationAttributes, ValidationMode.SyncOnly, value, context, state, onValidationError, cancellationToken);
        await ValidateAttributesAsync(validationAttributes, ValidationMode.AsyncOnly, value, context, state, onValidationError, cancellationToken);
    }

    private static async Task ValidateAttributesAsync<TState>(
        ValidationAttribute[] validationAttributes,
        ValidationMode mode,
        object? value,
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        ValidateContext context,
        TState state,
        Action<ValidateContext, ValidationResult, ValidationAttribute, TState> onValidationError,
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        CancellationToken cancellationToken)
    {
        List<Task>? validationResultTasks = null;

        for (var i = 0; i < validationAttributes.Length; i++)
        {
            var attribute = validationAttributes[i];
            var asyncValidationAttribute = attribute as AsyncValidationAttribute;
            var shouldValidate = mode switch
            {
                ValidationMode.SyncOnly => asyncValidationAttribute is null,
                ValidationMode.AsyncOnly => asyncValidationAttribute is not null,
                _ => throw new UnreachableException(),
            };

            if (!shouldValidate)
            {
                continue;
            }

            if (asyncValidationAttribute is not null)
            {
                validationResultTasks ??= new();
                validationResultTasks.Add(
                    GetValidationResultTaskCoreAsync(asyncValidationAttribute, value, context, state, onValidationError, cancellationToken));
            }
            else
            {
                var result = attribute.GetValidationResult(value, context.ValidationContext);
                if (result is not null && result != ValidationResult.Success)
                {
                    onValidationError(context, result, attribute, state);
                }
            }
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
