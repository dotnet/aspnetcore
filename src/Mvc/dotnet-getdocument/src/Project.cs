// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using IODirectory = System.IO.Directory;

namespace Microsoft.Extensions.ApiDescription.Tool
{
    internal class Project
    {
        private const string ResourceFilename = "ServiceProjectReferenceMetadata.targets";
        private const string MSBuildResourceName = "Microsoft.Extensions.ApiDescription.Tool." + ResourceFilename;

        private Project()
        {
        }

        public string AssemblyName { get; private set; }

        public string ConfigPath { get; private set; }

        public string Configuration { get; private set; }

        public string DefaultDocumentName { get; private set; }

        public string DefaultMethod { get; private set; }

        public string DefaultService { get; private set; }

        public string OutputPath { get; private set; }

        public string Platform { get; private set; }

        public string PlatformTarget { get; private set; }

        public string ProjectAssetsFile { get; private set; }

        public string ProjectDepsFilePath { get; private set; }

        public string ProjectDirectory { get; private set; }

        public string ProjectExtensionsPath { get; private set; }

        public string ProjectName { get; private set; }

        public string ProjectRuntimeConfigFilePath { get; private set; }

        public string RuntimeFrameworkVersion { get; private set; }

        public string RuntimeIdentifier { get; private set; }

        public string TargetFramework { get; private set; }

        public string TargetFrameworkMoniker { get; private set; }

        public string TargetPath { get; private set; }

        public static Project FromFile(
            string projectFile,
            string buildExtensionsDirectory,
            string framework = null,
            string configuration = null,
            string runtime = null)
        {
            if (string.IsNullOrEmpty(projectFile))
            {
                throw new ArgumentNullException(nameof(projectFile));
            }

            if (string.IsNullOrEmpty(buildExtensionsDirectory))
            {
                buildExtensionsDirectory = Path.Combine(Path.GetDirectoryName(projectFile), "obj");
            }

            IODirectory.CreateDirectory(buildExtensionsDirectory);

            var assembly = typeof(Project).Assembly;
            var targetsPath = Path.Combine(
                buildExtensionsDirectory,
                $"{Path.GetFileName(projectFile)}.{ResourceFilename}");
            using (var input = assembly.GetManifestResourceStream(MSBuildResourceName))
            {
                using (var output = File.OpenWrite(targetsPath))
                {
                    // NB: Copy always in case it changes
                    Reporter.WriteVerbose(Resources.FormatWritingFile(targetsPath));
                    input.CopyTo(output);

                    output.Flush();
                }
            }

            IDictionary<string, string> metadata;
            var metadataPath = Path.GetTempFileName();
            try
            {
                var args = new List<string>
                {
                    "msbuild",
                    "/target:WriteServiceProjectReferenceMetadata",
                    "/verbosity:quiet",
                    "/nologo",
                    "/nodeReuse:false",
                    $"/property:ServiceProjectReferenceMetadataPath={metadataPath}",
                    projectFile,
                };

                if (!string.IsNullOrEmpty(framework))
                {
                    args.Add($"/property:TargetFramework={framework}");
                }

                if (!string.IsNullOrEmpty(configuration))
                {
                    args.Add($"/property:Configuration={configuration}");
                }

                if (!string.IsNullOrEmpty(runtime))
                {
                    args.Add($"/property:RuntimeIdentifier={runtime}");
                }

                var exitCode = Exe.Run("dotnet", args);
                if (exitCode != 0)
                {
                    throw new CommandException(Resources.GetMetadataFailed);
                }

                metadata = File
                    .ReadLines(metadataPath)
                    .Select(l => l.Split(new[] { ':' }, 2))
                    .ToDictionary(s => s[0], s => s[1].TrimStart());
            }
            finally
            {
                // Ignore errors about in-use files. Should still be marked for delete after process cleanup.
                try
                {
                    File.Delete(metadataPath);
                }
                catch (UnauthorizedAccessException)
                {
                }

                try
                {
                    File.Delete(targetsPath);
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            var project = new Project
            {
                DefaultDocumentName = metadata[nameof(DefaultDocumentName)],
                DefaultMethod = metadata[nameof(DefaultMethod)],
                DefaultService = metadata[nameof(DefaultService)],

                AssemblyName = metadata[nameof(AssemblyName)],
                Configuration = metadata[nameof(Configuration)],
                OutputPath = metadata[nameof(OutputPath)],
                Platform = metadata[nameof(Platform)],
                PlatformTarget = metadata[nameof(PlatformTarget)] ?? metadata[nameof(Platform)],
                ProjectAssetsFile = metadata[nameof(ProjectAssetsFile)],
                ProjectDepsFilePath = metadata[nameof(ProjectDepsFilePath)],
                ProjectDirectory = metadata[nameof(ProjectDirectory)],
                ProjectExtensionsPath = metadata[nameof(ProjectExtensionsPath)],
                ProjectName = metadata[nameof(ProjectName)],
                ProjectRuntimeConfigFilePath = metadata[nameof(ProjectRuntimeConfigFilePath)],
                RuntimeFrameworkVersion = metadata[nameof(RuntimeFrameworkVersion)],
                RuntimeIdentifier = metadata[nameof(RuntimeIdentifier)],
                TargetFramework = metadata[nameof(TargetFramework)],
                TargetFrameworkMoniker = metadata[nameof(TargetFrameworkMoniker)],
                TargetPath = metadata[nameof(TargetPath)],
            };

            if (string.IsNullOrEmpty(project.OutputPath))
            {
                throw new CommandException(
                    Resources.FormatGetMetadataValueFailed(nameof(OutputPath), nameof(OutputPath)));
            }

            if (string.IsNullOrEmpty(project.ProjectDirectory))
            {
                throw new CommandException(
                    Resources.FormatGetMetadataValueFailed(nameof(ProjectDirectory), "MSBuildProjectDirectory"));
            }

            if (string.IsNullOrEmpty(project.TargetPath))
            {
                throw new CommandException(
                    Resources.FormatGetMetadataValueFailed(nameof(TargetPath), nameof(TargetPath)));
            }

            if (!Path.IsPathRooted(project.ProjectDirectory))
            {
                project.OutputPath = Path.GetFullPath(
                    Path.Combine(IODirectory.GetCurrentDirectory(), project.ProjectDirectory));
            }

            if (!Path.IsPathRooted(project.OutputPath))
            {
                project.OutputPath = Path.GetFullPath(Path.Combine(project.ProjectDirectory, project.OutputPath));
            }

            if (!Path.IsPathRooted(project.ProjectExtensionsPath))
            {
                project.ProjectExtensionsPath = Path.GetFullPath(
                    Path.Combine(project.ProjectDirectory, project.ProjectExtensionsPath));
            }

            if (!Path.IsPathRooted(project.TargetPath))
            {
                project.TargetPath = Path.GetFullPath(Path.Combine(project.OutputPath, project.TargetPath));
            }

            // Some document generation tools support non-ASP.NET Core projects. Any of the remaining properties may
            // thus be null empty.
            var configPath = $"{project.TargetPath}.config";
            if (File.Exists(configPath))
            {
                project.ConfigPath = configPath;
            }

            if (!(string.IsNullOrEmpty(project.ProjectAssetsFile) || Path.IsPathRooted(project.ProjectAssetsFile)))
            {
                project.ProjectAssetsFile = Path.GetFullPath(
                    Path.Combine(project.ProjectDirectory, project.ProjectAssetsFile));
            }

            if (!(string.IsNullOrEmpty(project.ProjectDepsFilePath) || Path.IsPathRooted(project.ProjectDepsFilePath)))
            {
                project.ProjectDepsFilePath = Path.GetFullPath(
                    Path.Combine(project.ProjectDirectory, project.ProjectDepsFilePath));
            }

            if (!(string.IsNullOrEmpty(project.ProjectRuntimeConfigFilePath) ||
                Path.IsPathRooted(project.ProjectRuntimeConfigFilePath)))
            {
                project.ProjectRuntimeConfigFilePath = Path.GetFullPath(
                    Path.Combine(project.OutputPath, project.ProjectRuntimeConfigFilePath));
            }

            return project;
        }
    }
}
