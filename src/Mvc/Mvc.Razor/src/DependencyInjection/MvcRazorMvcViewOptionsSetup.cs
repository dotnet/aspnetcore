// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Configures <see cref="MvcViewOptions"/> to use <see cref="RazorViewEngine"/>.
    /// </summary>
    internal class MvcRazorMvcViewOptionsSetup : IConfigureOptions<MvcViewOptions>
    {
        private readonly IRazorViewEngine _razorViewEngine;

        /// <summary>
        /// Initializes a new instance of <see cref="MvcRazorMvcViewOptionsSetup"/>.
        /// </summary>
        /// <param name="razorViewEngine">The <see cref="IRazorViewEngine"/>.</param>
        public MvcRazorMvcViewOptionsSetup(IRazorViewEngine razorViewEngine)
        {
            if (razorViewEngine == null)
            {
                throw new ArgumentNullException(nameof(razorViewEngine));
            }

            _razorViewEngine = razorViewEngine;
        }

        /// <summary>
        /// Configures <paramref name="options"/> to use <see cref="RazorViewEngine"/>.
        /// </summary>
        /// <param name="options">The <see cref="MvcViewOptions"/> to configure.</param>
        public void Configure(MvcViewOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.ViewEngines.Add(_razorViewEngine);
        }
    }
}