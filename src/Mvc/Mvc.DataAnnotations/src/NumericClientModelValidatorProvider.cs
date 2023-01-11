// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

/// <summary>
/// An implementation of <see cref="IClientModelValidatorProvider"/> which provides client validators
/// for specific numeric types.
/// </summary>
internal sealed class NumericClientModelValidatorProvider : IClientModelValidatorProvider
{
    /// <inheritdoc />
    public void CreateValidators(ClientValidatorProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var typeToValidate = context.ModelMetadata.UnderlyingOrModelType;

        // Check only the numeric types for which we set type='text'.
        if (typeToValidate == typeof(float) ||
            typeToValidate == typeof(double) ||
            typeToValidate == typeof(decimal))
        {
            var results = context.Results;
            // Read interface .Count once rather than per iteration
            var resultsCount = results.Count;
            for (var i = 0; i < resultsCount; i++)
            {
                var validator = results[i].Validator;
                if (validator != null && validator is NumericClientModelValidator)
                {
                    // A validator is already present. No need to add one.
                    return;
                }
            }

            results.Add(new ClientValidatorItem
            {
                Validator = new NumericClientModelValidator(),
                IsReusable = true
            });
        }
    }
}
