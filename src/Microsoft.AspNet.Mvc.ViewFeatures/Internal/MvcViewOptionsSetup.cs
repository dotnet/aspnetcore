// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Localization;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.ViewFeatures.Internal
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

            // Set up client validators
            options.ClientModelValidatorProviders.Add(new DefaultClientModelValidatorProvider());
            options.ClientModelValidatorProviders.Add(new DataAnnotationsClientModelValidatorProvider(
                dataAnnotationsLocalizationOptions,
                stringLocalizerFactory));
            options.ClientModelValidatorProviders.Add(new NumericClientModelValidatorProvider());
        }
    }
}
