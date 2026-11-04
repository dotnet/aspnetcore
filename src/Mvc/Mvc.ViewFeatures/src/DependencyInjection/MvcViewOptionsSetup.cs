// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Sets up default options for <see cref="MvcViewOptions"/>.
/// </summary>
internal sealed class MvcViewOptionsSetup : IConfigureOptions<MvcViewOptions>
{
    private readonly IOptions<MvcDataAnnotationsLocalizationOptions> _dataAnnotationsLocalizationOptions;
    private readonly IValidationAttributeAdapterProvider _validationAttributeAdapterProvider;
    private readonly IStringLocalizerFactory _stringLocalizerFactory;

    public MvcViewOptionsSetup(
        IOptions<MvcDataAnnotationsLocalizationOptions> dataAnnotationLocalizationOptions,
        IValidationAttributeAdapterProvider validationAttributeAdapterProvider)
    {
        ArgumentNullException.ThrowIfNull(dataAnnotationLocalizationOptions);
        ArgumentNullException.ThrowIfNull(validationAttributeAdapterProvider);

        _dataAnnotationsLocalizationOptions = dataAnnotationLocalizationOptions;
        _validationAttributeAdapterProvider = validationAttributeAdapterProvider;
    }

    public MvcViewOptionsSetup(
        IOptions<MvcDataAnnotationsLocalizationOptions> dataAnnotationOptions,
        IValidationAttributeAdapterProvider validationAttributeAdapterProvider,
        IStringLocalizerFactory stringLocalizerFactory)
        : this(dataAnnotationOptions, validationAttributeAdapterProvider)
    {
        ArgumentNullException.ThrowIfNull(stringLocalizerFactory);

        _stringLocalizerFactory = stringLocalizerFactory;
    }

    public void Configure(MvcViewOptions options)
    {
        // Set up client validators
        options.ClientModelValidatorProviders.Add(new DefaultClientModelValidatorProvider());
        options.ClientModelValidatorProviders.Add(new DataAnnotationsClientModelValidatorProvider(
            _validationAttributeAdapterProvider,
            _dataAnnotationsLocalizationOptions,
            _stringLocalizerFactory));
        options.ClientModelValidatorProviders.Add(new NumericClientModelValidatorProvider());
    }
}
