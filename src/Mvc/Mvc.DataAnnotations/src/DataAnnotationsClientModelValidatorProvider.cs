// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.DataAnnotations;

/// <summary>
/// An implementation of <see cref="IClientModelValidatorProvider"/> which provides client validators
/// for attributes which derive from <see cref="ValidationAttribute"/>. It also provides
/// a validator for types which implement <see cref="IClientModelValidator"/>.
/// The logic to support <see cref="IClientModelValidator"/>
/// is implemented in <see cref="ValidationAttributeAdapter{TAttribute}"/>.
/// </summary>
internal sealed class DataAnnotationsClientModelValidatorProvider : IClientModelValidatorProvider
{
    private readonly IOptions<MvcDataAnnotationsLocalizationOptions> _options;
    private readonly IStringLocalizerFactory? _stringLocalizerFactory;
    private readonly IValidationAttributeAdapterProvider _validationAttributeAdapterProvider;

    /// <summary>
    /// Create a new instance of <see cref="DataAnnotationsClientModelValidatorProvider"/>.
    /// </summary>
    /// <param name="validationAttributeAdapterProvider">The <see cref="IValidationAttributeAdapterProvider"/>
    /// that supplies <see cref="IAttributeAdapter"/>s.</param>
    /// <param name="options">The <see cref="IOptions{MvcDataAnnotationsLocalizationOptions}"/>.</param>
    /// <param name="stringLocalizerFactory">The <see cref="IStringLocalizerFactory"/>.</param>
    public DataAnnotationsClientModelValidatorProvider(
        IValidationAttributeAdapterProvider validationAttributeAdapterProvider,
        IOptions<MvcDataAnnotationsLocalizationOptions> options,
        IStringLocalizerFactory? stringLocalizerFactory)
    {
        ArgumentNullException.ThrowIfNull(validationAttributeAdapterProvider);
        ArgumentNullException.ThrowIfNull(options);

        _validationAttributeAdapterProvider = validationAttributeAdapterProvider;
        _options = options;
        _stringLocalizerFactory = stringLocalizerFactory;
    }

    /// <inheritdoc />
    public void CreateValidators(ClientValidatorProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        IStringLocalizer? stringLocalizer = null;
        if (_options.Value.DataAnnotationLocalizerProvider != null && _stringLocalizerFactory != null)
        {
            // This will pass first non-null type (either containerType or modelType) to delegate.
            // Pass the root model type(container type) if it is non null, else pass the model type.
            stringLocalizer = _options.Value.DataAnnotationLocalizerProvider(
                context.ModelMetadata.ContainerType ?? context.ModelMetadata.ModelType,
                _stringLocalizerFactory);
        }

        var hasRequiredAttribute = false;

        var results = context.Results;
        // Read interface .Count once rather than per iteration
        var resultsCount = results.Count;
        for (var i = 0; i < resultsCount; i++)
        {
            var validatorItem = results[i];
            if (validatorItem.Validator != null)
            {
                // Check if a required attribute is already cached.
                hasRequiredAttribute |= validatorItem.Validator is RequiredAttributeAdapter;
                continue;
            }

            var attribute = validatorItem.ValidatorMetadata as ValidationAttribute;
            if (attribute == null)
            {
                continue;
            }

            hasRequiredAttribute |= attribute is RequiredAttribute;

            var adapter = _validationAttributeAdapterProvider.GetAttributeAdapter(attribute, stringLocalizer);
            if (adapter != null)
            {
                validatorItem.Validator = adapter;
                validatorItem.IsReusable = true;
            }
        }

        if (!hasRequiredAttribute && context.ModelMetadata.IsRequired)
        {
            // Add a default '[Required]' validator for generating HTML if necessary.
            context.Results.Add(new ClientValidatorItem
            {
                Validator = _validationAttributeAdapterProvider.GetAttributeAdapter(
                    new RequiredAttribute(),
                    stringLocalizer),
                IsReusable = true
            });
        }
    }
}
