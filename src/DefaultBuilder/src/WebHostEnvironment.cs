// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        private IFileProvider _contentRootFileProvider = NullFileProvider;
        private IFileProvider _webRootFileProvider = NullFileProvider;
        // ContentRootPath and WebRootPath are set to default! on
        // initialization to match the behavior in HostingEnvironment.
        private string _contentRootPath = default!;
        private string _webRootPath = default!;

        public WebHostEnvironment(Assembly? callingAssembly = null)
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

        public void ApplyEnvironmentSettings(IWebHostBuilder genericWebHostBuilder, HostBuilder hostBuilder)
        {
            genericWebHostBuilder.UseSetting(WebHostDefaults.ApplicationKey, ApplicationName);
            genericWebHostBuilder.UseSetting(WebHostDefaults.EnvironmentKey, EnvironmentName);
            genericWebHostBuilder.UseSetting(WebHostDefaults.ContentRootKey, ContentRootPath);
            genericWebHostBuilder.UseSetting(WebHostDefaults.WebRootKey, WebRootPath);

            var context = (WebHostBuilderContext)hostBuilder.Properties[typeof(WebHostBuilderContext)];
            CopyPropertiesTo(context.HostingEnvironment);
        }

        internal void CopyPropertiesTo(IWebHostEnvironment destination)
        {
            destination.ApplicationName = ApplicationName;
            destination.EnvironmentName = EnvironmentName;
            destination.ContentRootPath = ContentRootPath;
            destination.WebRootPath = WebRootPath;
        }

        public string ApplicationName { get; set; }
        public string EnvironmentName { get; set; }

        public IFileProvider ContentRootFileProvider
        {
            get => _contentRootFileProvider;
            set => _contentRootFileProvider = value;
        }

        public IFileProvider WebRootFileProvider
        {
            get => _webRootFileProvider;
            set => _webRootFileProvider = value;
        }

        public string ContentRootPath
        {
            get => _contentRootPath;
            set
            {
                // No-op if the value setting does not change
                var targetValue = string.IsNullOrEmpty(value)
                    ? Directory.GetCurrentDirectory()
                    : ResolvePathToRoot(value, AppContext.BaseDirectory);
                if (targetValue == _contentRootPath)
                {
                    return;
                }

                _contentRootPath = targetValue;

                /* Update both file providers if content root path changes */
                if (Directory.Exists(_contentRootPath))
                {
                    _contentRootFileProvider = new PhysicalFileProvider(_contentRootPath);
                }
                if (Directory.Exists(WebRootPath))
                {
                    _webRootFileProvider = new PhysicalFileProvider(WebRootPath);
                }
            }
        }

        public string WebRootPath
        {
            get => ResolvePathToRoot(_webRootPath, ContentRootPath);
            set
            {
                // No-op if the value setting does not change
                var targetValue = string.IsNullOrEmpty(value) ? "wwwroot" : value;
                if (targetValue == _webRootPath)
                {
                    return;
                }

                _webRootPath = targetValue;
                if (Directory.Exists(WebRootPath))
                {
                    _webRootFileProvider = new PhysicalFileProvider(WebRootPath);
                }
            }
        }

        private string ResolvePathToRoot(string relativePath, string basePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return Path.GetFullPath(basePath);
            }

            if (Path.IsPathRooted(relativePath))
            {
                return relativePath;
            }

            return Path.Combine(Path.GetFullPath(basePath), relativePath);
        }
    }
}
