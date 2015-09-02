// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc.Razor
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
            [NotNull] IServiceProvider serviceProvider,
            [NotNull] MvcViewOptions options)
        {
            var razorViewEngine = serviceProvider.GetRequiredService<IRazorViewEngine>();
            options.ViewEngines.Add(razorViewEngine);
        }
    }
}