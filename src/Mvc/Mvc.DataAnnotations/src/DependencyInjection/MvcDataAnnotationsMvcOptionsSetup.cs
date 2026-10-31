// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Sets up default options for <see cref="MvcOptions"/>.
/// </summary>
internal sealed class MvcDataAnnotationsMvcOptionsSetup : IConfigureOptions<MvcOptions>
{
    private readonly IStringLocalizerFactory? _stringLocalizerFactory;
    private readonly IValidationAttributeAdapterProvider _validationAttributeAdapterProvider;
    private readonly IOptions<MvcDataAnnotationsLocalizationOptions> _dataAnnotationLocalizationOptions;

    public MvcDataAnnotationsMvcOptionsSetup(
        IValidationAttributeAdapterProvider validationAttributeAdapterProvider,
        IOptions<MvcDataAnnotationsLocalizationOptions> dataAnnotationLocalizationOptions)
    {
        ArgumentNullException.ThrowIfNull(validationAttributeAdapterProvider);
        ArgumentNullException.ThrowIfNull(dataAnnotationLocalizationOptions);

        _validationAttributeAdapterProvider = validationAttributeAdapterProvider;
        _dataAnnotationLocalizationOptions = dataAnnotationLocalizationOptions;
    }

    public MvcDataAnnotationsMvcOptionsSetup(
        IValidationAttributeAdapterProvider validationAttributeAdapterProvider,
        IOptions<MvcDataAnnotationsLocalizationOptions> dataAnnotationLocalizationOptions,
        IStringLocalizerFactory stringLocalizerFactory)
        : this(validationAttributeAdapterProvider, dataAnnotationLocalizationOptions)
    {
        _stringLocalizerFactory = stringLocalizerFactory;
    }

    public void Configure(MvcOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.ModelMetadataDetailsProviders.Add(new DataAnnotationsMetadataProvider(
            options,
            _dataAnnotationLocalizationOptions,
            _stringLocalizerFactory));

        options.ModelValidatorProviders.Add(new DataAnnotationsModelValidatorProvider(
            _validationAttributeAdapterProvider,
            _dataAnnotationLocalizationOptions,
            _stringLocalizerFactory));
    }
}
