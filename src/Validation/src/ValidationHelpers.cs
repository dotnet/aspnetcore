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
        Action<ValidateContext, Exception, TState> onValidationException,
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        CancellationToken cancellationToken)
    {
        await ValidateAttributesAsync(validationAttributes, ValidationMode.SyncOnly, value, context, state, onValidationError, onValidationException, cancellationToken);
        await ValidateAttributesAsync(validationAttributes, ValidationMode.AsyncOnly, value, context, state, onValidationError, onValidationException, cancellationToken);
    }

    private static async Task ValidateAttributesAsync<TState>(
        ValidationAttribute[] validationAttributes,
        ValidationMode mode,
        object? value,
#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        ValidateContext context,
        TState state,
        Action<ValidateContext, ValidationResult, ValidationAttribute, TState> onValidationError,
        Action<ValidateContext, Exception, TState> onValidationException,
#pragma warning restore ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        CancellationToken cancellationToken)
    {
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

            try
            {
                ValidationResult? result;
                if (asyncValidationAttribute is not null)
                {
                    result = await asyncValidationAttribute.GetValidationResultAsync(value, context.ValidationContext, cancellationToken);
                }
                else
                {
                    result = attribute.GetValidationResult(value, context.ValidationContext);
                }

                if (result is not null && result != ValidationResult.Success)
                {
                    onValidationError(context, result, attribute, state);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                onValidationException(context, ex, state);
            }
        }
    }
}
