// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.Cli.Utils;
using NuGet.Frameworks;

namespace Microsoft.AspNetCore.Server.IISIntegration.Tools
{
    public class PublishIISCommand
    {
        private readonly string _publishFolder;
        private readonly string _projectPath;
        private readonly string _framework;
        private readonly string _configuration;

        public PublishIISCommand(string publishFolder, string framework, string configuration, string projectPath)
        {
            _publishFolder = publishFolder;
            _projectPath = projectPath;
            _framework = framework;
            _configuration = configuration;
        }

        public int Run()
        {
            var applicationBasePath = GetApplicationBasePath();

            XDocument webConfigXml = null;
            var webConfigPath = Path.Combine(_publishFolder, "web.config");
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
                Reporter.Output.WriteLine($"No web.config found. Creating '{webConfigPath}'");
            }

            var projectContext = GetProjectContext(applicationBasePath, _framework);
            var isPortable = !projectContext.TargetFramework.IsDesktop() && projectContext.IsPortable;
            var applicationName =
                projectContext.ProjectFile.GetCompilerOptions(projectContext.TargetFramework, _configuration).OutputName +
                (isPortable ? ".dll" : ".exe");
            var transformedConfig = WebConfigTransform.Transform(webConfigXml, applicationName, ConfigureForAzure(), isPortable);

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

        private static ProjectContext GetProjectContext(string applicationBasePath, string framework)
        {
            var project = ProjectReader.GetProject(Path.Combine(applicationBasePath, "project.json"));

            return new ProjectContextBuilder()
                .WithProject(project)
                .WithTargetFramework(framework)
                .Build();
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
