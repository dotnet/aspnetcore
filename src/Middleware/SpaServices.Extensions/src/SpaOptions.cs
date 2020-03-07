// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.SpaServices
{
    /// <summary>
    /// Describes options for hosting a Single Page Application (SPA).
    /// </summary>
    public class SpaOptions
    {
        private PathString _defaultPage = "/index.html";
        private string _packageManagerCommand = "npm";

        /// <summary>
        /// Constructs a new instance of <see cref="SpaOptions"/>.
        /// </summary>
        public SpaOptions()
        {
        }

        /// <summary>
        /// Constructs a new instance of <see cref="SpaOptions"/>.
        /// </summary>
        /// <param name="copyFromOptions">An instance of <see cref="SpaOptions"/> from which values should be copied.</param>
        internal SpaOptions(SpaOptions copyFromOptions)
        {
            _defaultPage = copyFromOptions.DefaultPage;
            _packageManagerCommand = copyFromOptions.PackageManagerCommand;
            DefaultPageStaticFileOptions = copyFromOptions.DefaultPageStaticFileOptions;
            SourcePath = copyFromOptions.SourcePath;
            DevServerPort = copyFromOptions.DevServerPort;
        }

        /// <summary>
        /// Gets or sets the URL of the default page that hosts your SPA user interface.
        /// The default value is <c>"/index.html"</c>.
        /// </summary>
        public PathString DefaultPage
        {
            get => _defaultPage;
            set
            {
                if (string.IsNullOrEmpty(value.Value))
                {
                    throw new ArgumentException($"The value for {nameof(DefaultPage)} cannot be null or empty.");
                }

                _defaultPage = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="StaticFileOptions"/> that supplies content
        /// for serving the SPA's default page.
        ///
        /// If not set, a default file provider will read files from the
        /// <see cref="IHostingEnvironment.WebRootPath"/>, which by default is
        /// the <c>wwwroot</c> directory.
        /// </summary>
        public StaticFileOptions DefaultPageStaticFileOptions { get; set; }

        /// <summary>
        /// Gets or sets the path, relative to the application working directory,
        /// of the directory that contains the SPA source files during
        /// development. The directory may not exist in published applications.
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// Controls whether the development server should be used with a dynamic or fixed port.
        /// </summary>
        public int DevServerPort { get; set; } = default(int);

        /// <summary>
        /// Gets or sets the name of the package manager executable, (e.g npm,
        /// yarn) to run the SPA.
        ///
        /// The default value is 'npm'.
        /// </summary>
        public string PackageManagerCommand
        {
            get => _packageManagerCommand;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException($"The value for {nameof(PackageManagerCommand)} cannot be null or empty.");
                }

                _packageManagerCommand = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum duration that a request will wait for the SPA
        /// to become ready to serve to the client.
        /// </summary>
        public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromSeconds(120);
    }
}
