// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Builder
{
    internal class WebHostEnvironment : IWebHostEnvironment
    {
        private static readonly NullFileProvider NullFileProvider = new();

        public WebHostEnvironment(Assembly? callingAssembly)
        {
            ContentRootPath = Directory.GetCurrentDirectory();

            ApplicationName = (callingAssembly ?? Assembly.GetEntryAssembly())?.GetName()?.Name ?? string.Empty;
            EnvironmentName = Environments.Production;

            // Default to /wwwroot if it exists.
            var wwwroot = Path.Combine(ContentRootPath, "wwwroot");
            if (Directory.Exists(wwwroot))
            {
                WebRootPath = wwwroot;
            }

            if (this.IsDevelopment())
            {
                StaticWebAssetsLoader.UseStaticWebAssets(this, new Configuration());
            }
        }

        // For testing
        internal WebHostEnvironment()
        {
            ApplicationName = default!;
            EnvironmentName = default!;
        }

        public void ApplyConfigurationSettings(IConfiguration configuration)
        {
            ReadConfigurationSettings(configuration);
            if (this.IsDevelopment())
            {
                StaticWebAssetsLoader.UseStaticWebAssets(this, configuration);
            }
        }

        internal void ReadConfigurationSettings(IConfiguration configuration)
        {
            ApplicationName = configuration[WebHostDefaults.ApplicationKey] ?? ApplicationName;
            EnvironmentName = configuration[WebHostDefaults.EnvironmentKey] ?? EnvironmentName;
            ContentRootPath = configuration[WebHostDefaults.ContentRootKey] ?? ContentRootPath;
            WebRootPath = configuration[WebHostDefaults.WebRootKey] ?? WebRootPath;
        }

        public void ApplyEnvironmentSettings(IWebHostBuilder genericWebHostBuilder)
        {
            genericWebHostBuilder.UseSetting(WebHostDefaults.ApplicationKey, ApplicationName);
            genericWebHostBuilder.UseSetting(WebHostDefaults.EnvironmentKey, EnvironmentName);
            genericWebHostBuilder.UseSetting(WebHostDefaults.ContentRootKey, ContentRootPath);
            genericWebHostBuilder.UseSetting(WebHostDefaults.WebRootKey, WebRootPath);

            genericWebHostBuilder.ConfigureAppConfiguration((context, builder) =>
            {
                CopyPropertiesTo(context.HostingEnvironment);
            });
        }

        internal void CopyPropertiesTo(IWebHostEnvironment destination)
        {
            destination.ApplicationName = ApplicationName;
            destination.EnvironmentName = EnvironmentName;
            destination.ContentRootPath = ContentRootPath;
            destination.WebRootPath = WebRootPath;
        }

        public void ResolveFileProviders(IConfiguration configuration)
        {
            if (Directory.Exists(ContentRootPath))
            {
                ContentRootFileProvider = new PhysicalFileProvider(ContentRootPath);
            }

            if (Directory.Exists(WebRootPath))
            {
                WebRootFileProvider = new PhysicalFileProvider(WebRootPath);
            }

        }

        public string ApplicationName { get; set; }
        public string EnvironmentName { get; set; }

        private IFileProvider _contentRootFileProvider = NullFileProvider;
        public IFileProvider ContentRootFileProvider
        {
            get => _contentRootFileProvider;
            set
            {
                if (_contentRootPath == default(string))
                {
                    throw new InvalidOperationException("Can only set ContenRootFileProvider if ContentRootPath is set.");
                }
                _contentRootFileProvider = value;
            }
        }
        private IFileProvider _webRootFileProvider = NullFileProvider;
        public IFileProvider WebRootFileProvider
        {
            get => _webRootFileProvider;
            set
            {
                if (_contentRootPath == default(string))
                {
                    throw new InvalidOperationException("Can only set ContenRootFileProvider if ContentRootPath is set.");
                }
                _webRootFileProvider = value;
            }
        }

        // ContentRootPath and WebRootPath are set to default! on
        // initialization to match the behavior in HostingEnvironment.
        private string _contentRootPath = default!;
        public string ContentRootPath
        {
            get => _contentRootPath;
            set
            {
                _contentRootPath = value;
                if (Directory.Exists(_contentRootPath))
                {
                    _contentRootFileProvider = new PhysicalFileProvider(_contentRootPath);
                } 
            }
        }

        private string _webRootPath = default!;
        public string WebRootPath
        {
            get => _webRootPath;
            set
            {
                _webRootPath = ResolveWebRootPath(value, ContentRootPath ?? Directory.GetCurrentDirectory());
                if (Directory.Exists(_webRootPath))
                {
                    _webRootFileProvider = new PhysicalFileProvider(_webRootPath);
                }
            }
        }

        private string ResolveWebRootPath(string webRootPath, string contentRootPath)
        {
            if (string.IsNullOrEmpty(webRootPath))
            {
                return Path.GetFullPath(contentRootPath);
            }

            if (Path.IsPathRooted(webRootPath))
            {
                return webRootPath;
            }

            return Path.Combine(Path.GetFullPath(contentRootPath), webRootPath);
        }
    }
}
