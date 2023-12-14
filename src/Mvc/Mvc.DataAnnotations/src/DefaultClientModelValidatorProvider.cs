// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

/// <summary>
/// A default implementation of <see cref="IClientModelValidatorProvider"/>.
/// </summary>
/// <remarks>
/// The <see cref="DefaultClientModelValidatorProvider"/> provides validators from
/// <see cref="IClientModelValidator"/> instances in <see cref="ModelBinding.ModelMetadata.ValidatorMetadata"/>.
/// </remarks>
internal sealed class DefaultClientModelValidatorProvider : IClientModelValidatorProvider
{
    /// <inheritdoc />
    public void CreateValidators(ClientValidatorProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Perf: Avoid allocations
        var results = context.Results;
        var resultsCount = results.Count;
        for (var i = 0; i < resultsCount; i++)
        {
            var validatorItem = results[i];
            // Don't overwrite anything that was done by a previous provider.
            if (validatorItem.Validator != null)
            {
                continue;
            }

            var validator = validatorItem.ValidatorMetadata as IClientModelValidator;
            if (validator != null)
            {
                validatorItem.Validator = validator;
                validatorItem.IsReusable = true;
            }
        }
    }
}
