// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.DataAnnotations;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring MVC data annotations localization.
    /// </summary>
    public static class MvcDataAnnotationsMvcBuilderExtensions
    {
        /// <summary>
        /// Adds MVC data annotations localization to the application.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddDataAnnotationsLocalization(this IMvcBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return AddDataAnnotationsLocalization(builder, setupAction: null);
        }

        /// <summary>
        /// Adds MVC data annotations localization to the application.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/>.</param>
        /// <param name="setupAction">The action to configure <see cref="MvcDataAnnotationsLocalizationOptions"/>.
        /// </param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddDataAnnotationsLocalization(
            this IMvcBuilder builder,
            Action<MvcDataAnnotationsLocalizationOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            DataAnnotationsLocalizationServices.AddDataAnnotationsLocalizationServices(
                builder.Services,
                setupAction);

            return builder;
        }
    }
}
