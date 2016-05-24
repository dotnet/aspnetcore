// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    /// <summary>
    /// Configures <see cref="MvcViewOptions"/> to use <see cref="RazorViewEngine"/>.
    /// </summary>
    public class MvcRazorMvcViewOptionsSetup : ConfigureOptions<MvcViewOptions>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MvcRazorMvcViewOptionsSetup"/>.
        /// </summary>
        /// <param name="razorViewEngine">The <see cref="IRazorViewEngine"/>.</param>
        public MvcRazorMvcViewOptionsSetup(IRazorViewEngine razorViewEngine)
            : base(options => ConfigureMvc(razorViewEngine, options))
        {
        }

        /// <summary>
        /// Configures <paramref name="options"/> to use <see cref="RazorViewEngine"/>.
        /// </summary>
        /// <param name="razorViewEngine">The <see cref="IRazorViewEngine"/>.</param>
        /// <param name="options">The <see cref="MvcViewOptions"/> to configure.</param>
        public static void ConfigureMvc(
            IRazorViewEngine razorViewEngine,
            MvcViewOptions options)
        {
            if (razorViewEngine == null)
            {
                throw new ArgumentNullException(nameof(razorViewEngine));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.ViewEngines.Add(razorViewEngine);
        }
    }
}