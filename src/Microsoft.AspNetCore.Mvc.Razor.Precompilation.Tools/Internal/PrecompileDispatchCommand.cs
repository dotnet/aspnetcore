// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Razor.Precompilation.Design.Internal;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.DotNet.ProjectModel;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Internal;
using NuGet.Frameworks;

namespace Microsoft.AspNetCore.Mvc.Razor.Precompilation.Internal
{
    public class PrecompileDispatchCommand
    {
        private CommonOptions Options { get; } = new CommonOptions();

        private CommandOption FrameworkOption { get; set; }

        private CommandOption ConfigurationOption { get; set; }

        private CommandOption OutputPathOption { get; set; }

        private NuGetFramework TargetFramework { get; set; }

        private CommandOption BuildBasePathOption { get; set; }

        private string ProjectPath { get; set; }

        private string Configuration { get; set; }

        private string OutputPath { get; set; }

        public void Configure(CommandLineApplication app)
        {
            Options.Configure(app);
            FrameworkOption = app.Option(
                "-f|--framework",
                "Target Framework",
                CommandOptionType.SingleValue);

            ConfigurationOption = app.Option(
                "-c|--configuration",
                "Configuration",
                CommandOptionType.SingleValue);

            OutputPathOption = app.Option(
                "-o|--output-path",
                "Published path of the application.",
                CommandOptionType.SingleValue);

            app.OnExecute(() => Execute());
        }

        private int Execute()
        {
            ParseArguments();

            var runtimeContext = GetRuntimeContext();

            var outputPaths = runtimeContext.GetOutputPaths(Configuration);
            var applicationName = Path.GetFileNameWithoutExtension(outputPaths.CompilationFiles.Assembly);
            var dispatchArgs = new List<string>
            {
                ProjectPath,
                PrecompileRunCommand.ApplicationNameTemplate,
                applicationName,
                PrecompileRunCommand.OutputPathTemplate,
                OutputPath,
                CommonOptions.ContentRootTemplate,
                Options.ContentRootOption.Value() ?? Directory.GetCurrentDirectory(),
            };

            if (Options.ConfigureCompilationType.HasValue())
            {
                dispatchArgs.Add(CommonOptions.ConfigureCompilationTypeTemplate);
                dispatchArgs.Add(Options.ConfigureCompilationType.Value());
            }

            var compilerOptions = runtimeContext.ProjectFile.GetCompilerOptions(TargetFramework, Configuration);
            if (!string.IsNullOrEmpty(compilerOptions.KeyFile))
            {
                dispatchArgs.Add(StrongNameOptions.StrongNameKeyPath);
                var keyFilePath = Path.GetFullPath(Path.Combine(runtimeContext.ProjectDirectory, compilerOptions.KeyFile));
                dispatchArgs.Add(keyFilePath);

                if (compilerOptions.DelaySign ?? false)
                {
                    dispatchArgs.Add(StrongNameOptions.DelaySignTemplate);
                }

                if (compilerOptions.PublicSign ?? false)
                {
                    dispatchArgs.Add(StrongNameOptions.PublicSignTemplate);
                }
            }

#if DEBUG
            var commandLineArgs = Environment.GetCommandLineArgs();
            if (commandLineArgs.Length > 1 && commandLineArgs[1] == "--debug")
            {
                dispatchArgs.Insert(0, commandLineArgs[1]);
            }
#endif

            var toolName = typeof(Design.Program).GetTypeInfo().Assembly.GetName().Name;
            var dispatchCommand = DotnetToolDispatcher.CreateDispatchCommand(
                dispatchArgs,
                TargetFramework,
                Configuration,
                outputPath: outputPaths.RuntimeOutputPath,
                buildBasePath: null,
                projectDirectory: ProjectPath,
                toolName: toolName);

            var commandExitCode = dispatchCommand
                .ForwardStdErr(Console.Error)
                .ForwardStdOut(Console.Out)
                .Execute()
                .ExitCode;

            return commandExitCode;
        }

        private void ParseArguments()
        {
            ProjectPath = GetProjectPath();
            Configuration = ConfigurationOption.Value() ?? DotNet.Cli.Utils.Constants.DefaultConfiguration;

            if (!FrameworkOption.HasValue())
            {
                throw new Exception($"Option {FrameworkOption.Template} does not have a value.");
            }
            TargetFramework = NuGetFramework.Parse(FrameworkOption.Value());

            if (!OutputPathOption.HasValue())
            {
                throw new Exception($"Option {OutputPathOption.Template} does not have a value.");
            }
            OutputPath = OutputPathOption.Value();
        }

        private string GetProjectPath()
        {
            string projectPath;
            if (!string.IsNullOrEmpty(Options.ProjectArgument.Value))
            {
                projectPath = Path.GetFullPath(Options.ProjectArgument.Value);
                if (string.Equals(Path.GetFileName(ProjectPath), "project.json", StringComparison.OrdinalIgnoreCase))
                {
                    projectPath = Path.GetDirectoryName(ProjectPath);
                }

                if (!Directory.Exists(projectPath))
                {
                    throw new InvalidOperationException($"Could not find directory {projectPath}.");
                }
            }
            else
            {
                projectPath = Directory.GetCurrentDirectory();
            }

            return projectPath;
        }

        private ProjectContext GetRuntimeContext()
        {
            var workspace = new BuildWorkspace(ProjectReaderSettings.ReadFromEnvironment());

            var projectContext = workspace.GetProjectContext(ProjectPath, TargetFramework);
            if (projectContext == null)
            {
                Debug.Assert(FrameworkOption.HasValue());
                throw new InvalidOperationException($"Project '{ProjectPath}' does not support framework: {FrameworkOption.Value()}");
            }

            var runtimeContext = workspace.GetRuntimeContext(
                projectContext,
                RuntimeEnvironmentRidExtensions.GetAllCandidateRuntimeIdentifiers());
            return runtimeContext;
        }
    }
}
