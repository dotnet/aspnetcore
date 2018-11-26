// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System;
using System.IO;

namespace Microsoft.AspNetCore.SpaServices.StaticFiles
{
    /// <summary>
    /// Provides an implementation of <see cref="ISpaStaticFileProvider"/> that supplies
    /// physical files at a location configured using <see cref="SpaStaticFilesOptions"/>.
    /// </summary>
    internal class DefaultSpaStaticFileProvider : ISpaStaticFileProvider
    {
        private IFileProvider _fileProvider;

        public DefaultSpaStaticFileProvider(
            IServiceProvider serviceProvider,
            SpaStaticFilesOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrEmpty(options.RootPath))
            {
                throw new ArgumentException($"The {nameof(options.RootPath)} property " +
                    $"of {nameof(options)} cannot be null or empty.");
            }

            var env = serviceProvider.GetRequiredService<IHostingEnvironment>();
            var absoluteRootPath = Path.Combine(
                env.ContentRootPath,
                options.RootPath);

            // PhysicalFileProvider will throw if you pass a non-existent path,
            // but we don't want that scenario to be an error because for SPA
            // scenarios, it's better if non-existing directory just means we
            // don't serve any static files.
            if (Directory.Exists(absoluteRootPath))
            {
                _fileProvider = new PhysicalFileProvider(absoluteRootPath);
            }
        }

        public IFileProvider FileProvider => _fileProvider;
    }
}
