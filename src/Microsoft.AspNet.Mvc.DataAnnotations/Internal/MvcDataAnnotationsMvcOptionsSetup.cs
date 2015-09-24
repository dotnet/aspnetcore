// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Localization;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.DataAnnotations.Internal
{
    /// <summary>
    /// Sets up default options for <see cref="MvcOptions"/>.
    /// </summary>
    public class MvcDataAnnotationsMvcOptionsSetup : ConfigureOptions<MvcOptions>
    {
        public MvcDataAnnotationsMvcOptionsSetup(IServiceProvider serviceProvider)
            : base(options => ConfigureMvc(options, serviceProvider))
        {
        }

        public static void ConfigureMvc(MvcOptions options, IServiceProvider serviceProvider)
        {
            var dataAnnotationLocalizationOptions =
                serviceProvider.GetRequiredService<IOptions<MvcDataAnnotationsLocalizationOptions>>();

            // This service will be registered only if AddDataAnnotationsLocalization() is added to service collection.
            var stringLocalizerFactory = serviceProvider.GetService<IStringLocalizerFactory>();

            options.ModelMetadataDetailsProviders.Add(new DataAnnotationsMetadataProvider());
            options.ModelValidatorProviders.Add(new DataAnnotationsModelValidatorProvider(
                dataAnnotationLocalizationOptions,
                stringLocalizerFactory));
        }
    }
}