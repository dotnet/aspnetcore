// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using Microsoft.DotNet.Cli.Utils;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.Tools.PublishIIS
{
    public class PublishIISCommand
    {
        private readonly string _publishFolder;
        private readonly string _projectPath;
        private readonly string _webRoot;

        public PublishIISCommand(string publishFolder, string projectPath, string webRoot)
        {
            _publishFolder = publishFolder;
            _projectPath = projectPath;
            _webRoot = webRoot;
        }

        public int Run()
        {
            var applicationBasePath = GetApplicationBasePath();
            var webRoot = GetWebRoot(applicationBasePath);

            XDocument webConfigXml = null;
            var webRootDirectory = Path.Combine(_publishFolder, webRoot);
            var webConfigPath = Path.Combine(webRootDirectory, "web.config");
            if (File.Exists(webConfigPath))
            {
                Reporter.Output.WriteLine($"Updating web.config at '{webConfigPath}'");

                try
                {
                    webConfigXml = XDocument.Load(webConfigPath);
                }
                catch (XmlException) { }
            }
            else
            {
                if (!Directory.Exists(webRootDirectory))
                {
                    Reporter.Output.WriteLine($"No webroot directory found. Creating '{webRootDirectory}'");
                    Directory.CreateDirectory(webRootDirectory);
                }

                Reporter.Output.WriteLine($"No web.config found. Creating '{webConfigPath}'");
            }

            var applicationName = GetApplicationName(applicationBasePath) + ".exe";
            var transformedConfig = WebConfigTransform.Transform(webConfigXml, applicationName, ConfigureForAzure());

            using (var f = new FileStream(webConfigPath, FileMode.Create))
            {
                transformedConfig.Save(f);
            }

            return 0;
        }

        private string GetApplicationBasePath()
        {
            if (!string.IsNullOrEmpty(_projectPath))
            {
                var fullProjectPath = Path.GetFullPath(_projectPath);

                return Path.GetFileName(fullProjectPath) == "project.json"
                    ? Path.GetDirectoryName(fullProjectPath)
                    : fullProjectPath;
            }

            return Directory.GetCurrentDirectory();
        }

        private string GetApplicationName(string applicationBasePath)
        {
            return ProjectReader.GetProject(Path.Combine(applicationBasePath, "project.json")).Name;
        }

        private string GetWebRoot(string applicationBasePath)
        {
            if (!string.IsNullOrEmpty(_webRoot))
            {
                return _webRoot;
            }

            var builder = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(applicationBasePath, "hosting.json"), optional: true);

            var webroot = builder.Build()["webroot"];

            if (!string.IsNullOrEmpty(webroot))
            {
                return webroot;
            }

            if (Directory.Exists(Path.Combine(applicationBasePath, "wwwroot")))
            {
                return "wwwroot";
            }

            return string.Empty;
        }

        private static bool ConfigureForAzure()
        {
            var configureForAzureValue = Environment.GetEnvironmentVariable("DOTNET_CONFIGURE_AZURE");
            return string.Equals(configureForAzureValue, "true", StringComparison.Ordinal) ||
                string.Equals(configureForAzureValue, "1", StringComparison.Ordinal) ||
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
        }
    }
}
