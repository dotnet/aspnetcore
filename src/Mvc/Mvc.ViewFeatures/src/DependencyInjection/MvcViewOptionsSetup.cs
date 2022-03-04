// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Sets up default options for <see cref="MvcViewOptions"/>.
    /// </summary>
    internal class MvcViewOptionsSetup : IConfigureOptions<MvcViewOptions>
    {
        private readonly IOptions<MvcDataAnnotationsLocalizationOptions> _dataAnnotationsLocalizationOptions;
        private readonly IValidationAttributeAdapterProvider _validationAttributeAdapterProvider;
        private readonly IStringLocalizerFactory _stringLocalizerFactory;

        public MvcViewOptionsSetup(
            IOptions<MvcDataAnnotationsLocalizationOptions> dataAnnotationLocalizationOptions,
            IValidationAttributeAdapterProvider validationAttributeAdapterProvider)
        {
            if (dataAnnotationLocalizationOptions == null)
            {
                throw new ArgumentNullException(nameof(dataAnnotationLocalizationOptions));
            }

            if (validationAttributeAdapterProvider == null)
            {
                throw new ArgumentNullException(nameof(validationAttributeAdapterProvider));
            }

            _dataAnnotationsLocalizationOptions = dataAnnotationLocalizationOptions;
            _validationAttributeAdapterProvider = validationAttributeAdapterProvider;
        }

        public MvcViewOptionsSetup(
            IOptions<MvcDataAnnotationsLocalizationOptions> dataAnnotationOptions,
            IValidationAttributeAdapterProvider validationAttributeAdapterProvider,
            IStringLocalizerFactory stringLocalizerFactory)
            : this(dataAnnotationOptions, validationAttributeAdapterProvider)
        {
            if (stringLocalizerFactory == null)
            {
                throw new ArgumentNullException(nameof(stringLocalizerFactory));
            }

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
}
