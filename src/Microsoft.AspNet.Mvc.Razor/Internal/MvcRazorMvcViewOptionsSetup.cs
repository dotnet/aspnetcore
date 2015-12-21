// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNet.Mvc.Razor.Internal
{
    /// <summary>
    /// Configures <see cref="MvcViewOptions"/> to use <see cref="RazorViewEngine"/>.
    /// </summary>
    public class MvcRazorMvcViewOptionsSetup : ConfigureOptions<MvcViewOptions>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MvcRazorMvcViewOptionsSetup"/>.
        /// </summary>
        /// <param name="serviceProvider">The application's <see cref="IServiceProvider"/>.</param>
        public MvcRazorMvcViewOptionsSetup(IServiceProvider serviceProvider)
            : base(options => ConfigureMvc(serviceProvider, options))
        {
        }

        /// <summary>
        /// Configures <paramref name="options"/> to use <see cref="RazorViewEngine"/>.
        /// </summary>
        /// <param name="serviceProvider">The application's <see cref="IServiceProvider"/>.</param>
        /// <param name="options">The <see cref="MvcViewOptions"/> to configure.</param>
        public static void ConfigureMvc(
            IServiceProvider serviceProvider,
            MvcViewOptions options)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var razorViewEngine = serviceProvider.GetRequiredService<IRazorViewEngine>();
            options.ViewEngines.Add(razorViewEngine);
        }
    }
}