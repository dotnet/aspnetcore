// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Hosting
{
    internal static class HostingEnvironmentExtensions
    {
#pragma warning disable CS0618 // Type or member is obsolete
        internal static void Initialize(this IHostingEnvironment hostingEnvironment, string contentRootPath, WebHostOptions options)
#pragma warning restore CS0618 // Type or member is obsolete
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (string.IsNullOrEmpty(contentRootPath))
            {
                throw new ArgumentException("A valid non-empty content root must be provided.", nameof(contentRootPath));
            }
            if (!Directory.Exists(contentRootPath))
            {
                throw new ArgumentException($"The content root '{contentRootPath}' does not exist.", nameof(contentRootPath));
            }

            hostingEnvironment.ApplicationName = options.ApplicationName;
            hostingEnvironment.ContentRootPath = contentRootPath;
            hostingEnvironment.ContentRootFileProvider = new PhysicalFileProvider(hostingEnvironment.ContentRootPath);

            var webRoot = options.WebRoot;
            if (webRoot == null)
            {
                // Default to /wwwroot if it exists.
                var wwwroot = Path.Combine(hostingEnvironment.ContentRootPath, "wwwroot");
                if (Directory.Exists(wwwroot))
                {
                    hostingEnvironment.WebRootPath = wwwroot;
                }
            }
            else
            {
                hostingEnvironment.WebRootPath = Path.Combine(hostingEnvironment.ContentRootPath, webRoot);
            }

            if (!string.IsNullOrEmpty(hostingEnvironment.WebRootPath))
            {
                hostingEnvironment.WebRootPath = Path.GetFullPath(hostingEnvironment.WebRootPath);
                if (!Directory.Exists(hostingEnvironment.WebRootPath))
                {
                    Directory.CreateDirectory(hostingEnvironment.WebRootPath);
                }
                hostingEnvironment.WebRootFileProvider = new PhysicalFileProvider(hostingEnvironment.WebRootPath);
            }
            else
            {
                hostingEnvironment.WebRootFileProvider = new NullFileProvider();
            }

            hostingEnvironment.EnvironmentName =
                options.Environment ??
                hostingEnvironment.EnvironmentName;
        }

        internal static void Initialize(this IWebHostEnvironment hostingEnvironment, string contentRootPath, WebHostOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (string.IsNullOrEmpty(contentRootPath))
            {
                throw new ArgumentException("A valid non-empty content root must be provided.", nameof(contentRootPath));
            }
            if (!Directory.Exists(contentRootPath))
            {
                throw new ArgumentException($"The content root '{contentRootPath}' does not exist.", nameof(contentRootPath));
            }

            hostingEnvironment.ApplicationName = options.ApplicationName;
            hostingEnvironment.ContentRootPath = contentRootPath;
            hostingEnvironment.ContentRootFileProvider = new PhysicalFileProvider(hostingEnvironment.ContentRootPath);

            var webRoot = options.WebRoot;
            if (webRoot == null)
            {
                // Default to /wwwroot if it exists.
                var wwwroot = Path.Combine(hostingEnvironment.ContentRootPath, "wwwroot");
                if (Directory.Exists(wwwroot))
                {
                    hostingEnvironment.WebRootPath = wwwroot;
                }
            }
            else
            {
                hostingEnvironment.WebRootPath = Path.Combine(hostingEnvironment.ContentRootPath, webRoot);
            }

            if (!string.IsNullOrEmpty(hostingEnvironment.WebRootPath))
            {
                hostingEnvironment.WebRootPath = Path.GetFullPath(hostingEnvironment.WebRootPath);
                if (!Directory.Exists(hostingEnvironment.WebRootPath))
                {
                    Directory.CreateDirectory(hostingEnvironment.WebRootPath);
                }
                hostingEnvironment.WebRootFileProvider = new PhysicalFileProvider(hostingEnvironment.WebRootPath);
            }
            else
            {
                hostingEnvironment.WebRootFileProvider = new NullFileProvider();
            }

            hostingEnvironment.EnvironmentName =
                options.Environment ??
                hostingEnvironment.EnvironmentName;
        }
    }
}
