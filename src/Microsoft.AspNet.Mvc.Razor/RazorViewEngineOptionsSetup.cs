// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.FileProviders;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Sets up default options for <see cref="RazorViewEngineOptions"/>.
    /// </summary>
    public class RazorViewEngineOptionsSetup : ConfigureOptions<RazorViewEngineOptions>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RazorViewEngineOptions"/>.
        /// </summary>
        /// <param name="applicationEnvironment"><see cref="IApplicationEnvironment"/> for the application.</param>
        /// <param name="hostingEnvironment"><see cref="IHostingEnvironment"/> for the application.</param>
        public RazorViewEngineOptionsSetup(IApplicationEnvironment applicationEnvironment, IHostingEnvironment hostingEnvironment)
            : base(options => ConfigureRazor(options, applicationEnvironment, hostingEnvironment))
        {
        }

        private static void ConfigureRazor(
            RazorViewEngineOptions razorOptions,
            IApplicationEnvironment applicationEnvironment,
            IHostingEnvironment hostingEnvironment)
        {
            razorOptions.FileProvider = new PhysicalFileProvider(applicationEnvironment.ApplicationBasePath);
            if (hostingEnvironment.IsDevelopment())
            {
                razorOptions.Configuration = "Debug";
            }
            else
            {
                razorOptions.Configuration = "Release";
            }
        }
    }
}