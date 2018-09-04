// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GetDocument.Properties;
using IODirectory = System.IO.Directory;

namespace GetDocument
{
    internal class Project
    {
        private const string MSBuildResourceName = "GetDocument.ServiceProjectReferenceMetadata";

        private Project()
        {
        }

        public string AssemblyName { get; private set; }

        public string AssemblyPath { get; private set; }

        public string AssetsPath { get; private set; }

        public string Configuration { get; private set; }

        public string ConfigPath { get; private set; }

        public string DefaultDocumentName { get; private set; }

        public string DefaultMethod { get; private set; }

        public string DefaultService { get; private set; }

        public string DefaultUri { get; private set; }

        public string DepsPath { get; private set; }

        public string Directory { get; private set; }

        public string ExtensionsPath { get; private set; }

        public string Name { get; private set; }

        public string OutputPath { get; private set; }

        public string Platform { get; private set; }

        public string PlatformTarget { get; private set; }

        public string RuntimeConfigPath { get; private set; }

        public string RuntimeFrameworkVersion { get; private set; }

        public string TargetFramework { get; private set; }

        public string TargetFrameworkMoniker { get; private set; }

        public static Project FromFile(
            string file,
            string buildExtensionsDirectory,
            string framework = null,
            string configuration = null,
            string runtime = null)
        {
            Debug.Assert(!string.IsNullOrEmpty(file), "file is null or empty.");

            if (string.IsNullOrEmpty(buildExtensionsDirectory))
            {
                buildExtensionsDirectory = Path.Combine(Path.GetDirectoryName(file), "obj");
            }

            IODirectory.CreateDirectory(buildExtensionsDirectory);

            var assembly = typeof(Project).Assembly;
            var propsPath = Path.Combine(
                buildExtensionsDirectory,
                Path.GetFileName(file) + ".ServiceProjectReferenceMetadata.props");
            using (var input = assembly.GetManifestResourceStream($"{MSBuildResourceName}.props"))
            {
                using (var output = File.OpenWrite(propsPath))
                {
                    Reporter.WriteVerbose(Resources.WritingFile(propsPath));
                    input.CopyTo(output);
                }
            }

            var targetsPath = Path.ChangeExtension(propsPath, ".targets");
            using (var input = assembly.GetManifestResourceStream($"{MSBuildResourceName}.targets"))
            {
                using (var output = File.OpenWrite(targetsPath))
                {
                    // NB: Copy always in case it changes
                    Reporter.WriteVerbose(Resources.WritingFile(targetsPath));
                    input.CopyTo(output);
                }
            }

            IDictionary<string, string> metadata;
            var metadataPath = Path.GetTempFileName();
            try
            {
                var propertyArg = "/property:ServiceProjectReferenceMetadataPath=" + metadataPath;
                if (!string.IsNullOrEmpty(framework))
                {
                    propertyArg += ";TargetFramework=" + framework;
                }
                if (!string.IsNullOrEmpty(configuration))
                {
                    propertyArg += ";Configuration=" + configuration;
                }
                if (!string.IsNullOrEmpty(runtime))
                {
                    propertyArg += ";RuntimeIdentifier=" + runtime;
                }

                var args = new List<string>
                {
                    "msbuild",
                    "/target:WriteServiceProjectReferenceMetadata",
                    propertyArg,
                    "/verbosity:quiet",
                    "/nologo"
                };

                if (!string.IsNullOrEmpty(file))
                {
                    args.Add(file);
                }

                var exitCode = Exe.Run("dotnet", args);
                if (exitCode != 0)
                {
                    throw new CommandException(Resources.GetMetadataFailed);
                }

                metadata = File.ReadLines(metadataPath).Select(l => l.Split(new[] { ':' }, 2))
                    .ToDictionary(s => s[0], s => s[1].TrimStart());
            }
            finally
            {
                File.Delete(propsPath);
                File.Delete(metadataPath);
                File.Delete(targetsPath);
            }

            var project = new Project
            {
                AssemblyName = metadata[nameof(AssemblyName)],
                AssemblyPath = metadata[nameof(AssemblyPath)],
                AssetsPath = metadata[nameof(AssetsPath)],
                Configuration = metadata[nameof(Configuration)],
                DefaultDocumentName = metadata[nameof(DefaultDocumentName)],
                DefaultMethod = metadata[nameof(DefaultMethod)],
                DefaultService = metadata[nameof(DefaultService)],
                DefaultUri = metadata[nameof(DefaultUri)],
                DepsPath = metadata[nameof(DepsPath)],
                Directory = metadata[nameof(Directory)],
                ExtensionsPath = metadata[nameof(ExtensionsPath)],
                Name = metadata[nameof(Name)],
                OutputPath = metadata[nameof(OutputPath)],
                Platform = metadata[nameof(Platform)],
                PlatformTarget = metadata[nameof(PlatformTarget)] ?? metadata[nameof(Platform)],
                RuntimeConfigPath = metadata[nameof(RuntimeConfigPath)],
                RuntimeFrameworkVersion = metadata[nameof(RuntimeFrameworkVersion)],
                TargetFramework = metadata[nameof(TargetFramework)],
                TargetFrameworkMoniker = metadata[nameof(TargetFrameworkMoniker)],
            };

            if (string.IsNullOrEmpty(project.AssemblyPath))
            {
                throw new CommandException(Resources.GetMetadataValueFailed(nameof(AssemblyPath), "TargetPath"));
            }

            if (string.IsNullOrEmpty(project.Directory))
            {
                throw new CommandException(Resources.GetMetadataValueFailed(nameof(Directory), "ProjectDir"));
            }

            if (string.IsNullOrEmpty(project.OutputPath))
            {
                throw new CommandException(Resources.GetMetadataValueFailed(nameof(OutputPath), "OutDir"));
            }

            if (!Path.IsPathRooted(project.Directory))
            {
                project.Directory = Path.GetFullPath(Path.Combine(IODirectory.GetCurrentDirectory(), project.Directory));
            }

            if (!Path.IsPathRooted(project.AssemblyPath))
            {
                project.AssemblyPath = Path.GetFullPath(Path.Combine(project.Directory, project.AssemblyPath));
            }

            if (!Path.IsPathRooted(project.OutputPath))
            {
                project.OutputPath = Path.GetFullPath(Path.Combine(project.Directory, project.OutputPath));
            }

            // Some document generation tools support non-ASP.NET Core projects.
            // Thus any of the remaining properties may be empty.
            if (!(string.IsNullOrEmpty(project.AssetsPath) || Path.IsPathRooted(project.AssetsPath)))
            {
                project.AssetsPath = Path.GetFullPath(Path.Combine(project.Directory, project.AssetsPath));
            }

            var configPath = $"{project.AssemblyPath}.config";
            if (File.Exists(configPath))
            {
                project.ConfigPath = configPath;
            }

            if (!(string.IsNullOrEmpty(project.DepsPath) || Path.IsPathRooted(project.DepsPath)))
            {
                project.DepsPath = Path.GetFullPath(Path.Combine(project.Directory, project.DepsPath));
            }

            if (!(string.IsNullOrEmpty(project.RuntimeConfigPath) || Path.IsPathRooted(project.RuntimeConfigPath)))
            {
                project.RuntimeConfigPath = Path.GetFullPath(Path.Combine(project.Directory, project.RuntimeConfigPath));
            }

            return project;
        }
    }
}
