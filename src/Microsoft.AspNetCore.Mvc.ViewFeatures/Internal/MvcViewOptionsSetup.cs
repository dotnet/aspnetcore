// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    /// <summary>
    /// Sets up default options for <see cref="MvcViewOptions"/>.
    /// </summary>
    public class MvcViewOptionsSetup : ConfigureOptions<MvcViewOptions>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MvcViewOptionsSetup"/>.
        /// </summary>
        public MvcViewOptionsSetup(IServiceProvider serviceProvider)
            : base(options => ConfigureMvc(options, serviceProvider))
        {
        }

        public static void ConfigureMvc(
            MvcViewOptions options,
            IServiceProvider serviceProvider)
        {
            var dataAnnotationsLocalizationOptions =
                serviceProvider.GetRequiredService<IOptions<MvcDataAnnotationsLocalizationOptions>>();
            var stringLocalizerFactory = serviceProvider.GetService<IStringLocalizerFactory>();
            var validationAttributeAdapterProvider = serviceProvider.GetRequiredService<IValidationAttributeAdapterProvider>();

            // Set up client validators
            options.ClientModelValidatorProviders.Add(new DefaultClientModelValidatorProvider());
            options.ClientModelValidatorProviders.Add(new DataAnnotationsClientModelValidatorProvider(
                validationAttributeAdapterProvider,
                dataAnnotationsLocalizationOptions,
                stringLocalizerFactory));
            options.ClientModelValidatorProviders.Add(new NumericClientModelValidatorProvider());
        }
    }
}
