// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RazorRuntimeCompilationMvcBuilderExtensions
    {
        /// <summary>
        /// Configures <see cref="IMvcBuilder" /> to support runtime compilation of Razor views and Razor Pages.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder" />.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddRazorRuntimeCompilation(this IMvcBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            RazorRuntimeCompilationMvcCoreBuilderExtensions.AddServices(builder.Services);
            return builder;
        }

        /// <summary>
        /// Configures <see cref="IMvcBuilder" /> to support runtime compilation of Razor views and Razor Pages.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder" />.</param>
        /// <param name="setupAction">An action to configure the <see cref="MvcRazorRuntimeCompilationOptions"/>.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder AddRazorRuntimeCompilation(this IMvcBuilder builder, Action<MvcRazorRuntimeCompilationOptions> setupAction)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            RazorRuntimeCompilationMvcCoreBuilderExtensions.AddServices(builder.Services);
            builder.Services.Configure(setupAction);
            return builder;
        }
    }
}
