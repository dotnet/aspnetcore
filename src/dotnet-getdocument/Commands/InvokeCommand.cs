// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.DotNet.Cli.CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Extensions.ApiDescription.Client.Commands
{
    internal class InvokeCommand : HelpCommandBase
    {
        private const string InsideManName = "GetDocument.Insider";

        private CommandOption _configuration;
        private CommandOption _framework;
        private CommandOption _msbuildprojectextensionspath;
        private CommandOption _output;
        private CommandOption _project;
        private CommandOption _runtime;
        private IList<string> _args;

        public override void Configure(CommandLineApplication command)
        {
            var options = new ProjectOptions();
            options.Configure(command);

            _project = options.Project;
            _framework = options.Framework;
            _configuration = options.Configuration;
            _runtime = options.Runtime;
            _msbuildprojectextensionspath = options.MSBuildProjectExtensionsPath;

            _output = command.Option("--output <Path>", Resources.OutputDescription);
            command.VersionOption("--version", ProductInfo.GetVersion);
            _args = command.RemainingArguments;

            base.Configure(command);
        }

        protected override int Execute()
        {
            var projectFile = FindProjects(
                _project.Value(),
                Resources.NoProject,
                Resources.MultipleProjects);
            Reporter.WriteVerbose(Resources.FormatUsingProject(projectFile));

            var project = Project.FromFile(
                projectFile,
                _msbuildprojectextensionspath.Value(),
                _framework.Value(),
                _configuration.Value(),
                _runtime.Value());
            if (!File.Exists(project.AssemblyPath))
            {
                throw new CommandException(Resources.MustBuild);
            }

            var thisPath = Path.GetFullPath(Path.GetDirectoryName(typeof(InvokeCommand).Assembly.Location));

            string executable = null;
            var cleanupExecutable = false;
            try
            {
                string toolsDirectory;
                var args = new List<string>();
                var targetFramework = new FrameworkName(project.TargetFrameworkMoniker);
                switch (targetFramework.Identifier)
                {
                    case ".NETFramework":
                        cleanupExecutable = true;
                        executable = Path.Combine(project.OutputPath, InsideManName + ".exe");
                        toolsDirectory = Path.Combine(
                            thisPath,
                            project.PlatformTarget == "x86" ? "net461-x86" : "net461");

                        var executableSource = Path.Combine(toolsDirectory, InsideManName + ".exe");
                        File.Copy(executableSource, executable, overwrite: true);

                        if (!string.IsNullOrEmpty(project.ConfigPath))
                        {
                            File.Copy(project.ConfigPath, executable + ".config", overwrite: true);
                        }
                        break;

                    case ".NETCoreApp":
                        executable = "dotnet";
                        toolsDirectory = Path.Combine(thisPath, "netcoreapp2.0");

                        if (targetFramework.Version < new Version(2, 0))
                        {
                            throw new CommandException(
                                Resources.FormatNETCoreApp1Project(project.Name, targetFramework.Version));
                        }

                        args.Add("exec");
                        args.Add("--depsFile");
                        args.Add(project.DepsPath);

                        if (!string.IsNullOrEmpty(project.AssetsPath))
                        {
                            using (var reader = new JsonTextReader(File.OpenText(project.AssetsPath)))
                            {
                                var projectAssets = JToken.ReadFrom(reader);
                                var packageFolders = projectAssets["packageFolders"]
                                    .Children<JProperty>()
                                    .Select(p => p.Name);

                                foreach (var packageFolder in packageFolders)
                                {
                                    args.Add("--additionalProbingPath");
                                    args.Add(packageFolder.TrimEnd(Path.DirectorySeparatorChar));
                                }
                            }
                        }

                        if (File.Exists(project.RuntimeConfigPath))
                        {
                            args.Add("--runtimeConfig");
                            args.Add(project.RuntimeConfigPath);
                        }
                        else if (!string.IsNullOrEmpty(project.RuntimeFrameworkVersion))
                        {
                            args.Add("--fx-version");
                            args.Add(project.RuntimeFrameworkVersion);
                        }

                        args.Add(Path.Combine(toolsDirectory, InsideManName + ".dll"));
                        break;

                    case ".NETStandard":
                        throw new CommandException(Resources.FormatNETStandardProject(project.Name));

                    default:
                        throw new CommandException(
                            Resources.FormatUnsupportedFramework(project.Name, targetFramework.Identifier));
                }

                args.AddRange(_args);
                args.Add("--assembly");
                args.Add(project.AssemblyPath);
                args.Add("--tools-directory");
                args.Add(toolsDirectory);

                if (!(args.Contains("--method") || string.IsNullOrEmpty(project.DefaultMethod)))
                {
                    args.Add("--method");
                    args.Add(project.DefaultMethod);
                }

                if (!(args.Contains("--service") || string.IsNullOrEmpty(project.DefaultService)))
                {
                    args.Add("--service");
                    args.Add(project.DefaultService);
                }

                if (_output.HasValue())
                {
                    args.Add("--output");
                    args.Add(Path.GetFullPath(_output.Value()));
                }

                if (Reporter.IsVerbose)
                {
                    args.Add("--verbose");
                }

                if (Reporter.NoColor)
                {
                    args.Add("--no-color");
                }

                if (Reporter.PrefixOutput)
                {
                    args.Add("--prefix-output");
                }

                return Exe.Run(executable, args, project.Directory);
            }
            finally
            {
                if (cleanupExecutable && !string.IsNullOrEmpty(executable))
                {
                    File.Delete(executable);
                    File.Delete(executable + ".config");
                }
            }
        }

        private static string FindProjects(
            string path,
            string errorWhenNoProject,
            string errorWhenMultipleProjects)
        {
            var specified = true;
            if (path == null)
            {
                specified = false;
                path = Directory.GetCurrentDirectory();
            }
            else if (!Directory.Exists(path)) // It's not a directory
            {
                return path;
            }

            var projectFiles = Directory
                .EnumerateFiles(path, "*.*proj", SearchOption.TopDirectoryOnly)
                .Where(f => !string.Equals(Path.GetExtension(f), ".xproj", StringComparison.OrdinalIgnoreCase))
                .Take(2)
                .ToList();
            if (projectFiles.Count == 0)
            {
                throw new CommandException(
                    specified
                        ? Resources.FormatNoProjectInDirectory(path)
                        : errorWhenNoProject);
            }
            if (projectFiles.Count != 1)
            {
                throw new CommandException(
                    specified
                        ? Resources.FormatMultipleProjectsInDirectory(path)
                        : errorWhenMultipleProjects);
            }

            return projectFiles[0];
        }
    }
}
