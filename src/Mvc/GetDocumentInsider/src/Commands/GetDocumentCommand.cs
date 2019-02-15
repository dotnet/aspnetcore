// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
#if NETCOREAPP2_0
using System.Runtime.Loader;
#endif
using Microsoft.DotNet.Cli.CommandLine;

namespace Microsoft.Extensions.ApiDescription.Tool.Commands
{
    internal class GetDocumentCommand : ProjectCommandBase
    {
        internal const string FallbackDocumentName = "v1";
        internal const string FallbackMethod = "GenerateAsync";
        internal const string FallbackService = "Microsoft.Extensions.ApiDescription.IDocumentProvider";

        private CommandOption _documentName;
        private CommandOption _method;
        private CommandOption _output;
        private CommandOption _service;

        public override void Configure(CommandLineApplication command)
        {
            base.Configure(command);

            _documentName = command.Option(
                "--documentName <Name>",
                Resources.FormatDocumentDescription(FallbackDocumentName));
            _method = command.Option("--method <Name>", Resources.FormatMethodDescription(FallbackMethod));
            _output = command.Option("--output <Path>", Resources.OutputDescription);
            _service = command.Option("--service <QualifiedName>", Resources.FormatServiceDescription(FallbackService));
        }

        protected override void Validate()
        {
            base.Validate();

            if (!_output.HasValue())
            {
                throw new CommandException(Resources.FormatMissingOption(_output.LongName));
            }

            if (_method.HasValue() && !_service.HasValue())
            {
                throw new CommandException(Resources.FormatMissingOption(_service.LongName));
            }

            if (_service.HasValue() && !_method.HasValue())
            {
                throw new CommandException(Resources.FormatMissingOption(_method.LongName));
            }
        }

        protected override int Execute()
        {
            var thisAssembly = typeof(GetDocumentCommand).Assembly;

            var toolsDirectory = ToolsDirectory.Value();
            var packagedAssemblies = Directory
                .EnumerateFiles(toolsDirectory, "*.dll")
                .Except(new[] { Path.GetFullPath(thisAssembly.Location) })
                .ToDictionary(path => Path.GetFileNameWithoutExtension(path), path => new AssemblyInfo(path));

            // Explicitly load all assemblies we need first to preserve target project as much as possible. This
            // executable is always run in the target project's context (either through location or .deps.json file).
            foreach (var keyValuePair in packagedAssemblies)
            {
                try
                {
                    keyValuePair.Value.Assembly = Assembly.Load(new AssemblyName(keyValuePair.Key));
                }
                catch
                {
                    // Ignore all failures because missing assemblies should be loadable from tools directory.
                }
            }

#if NETCOREAPP2_0
            AssemblyLoadContext.Default.Resolving += (loadContext, assemblyName) =>
            {
                var name = assemblyName.Name;
                if (!packagedAssemblies.TryGetValue(name, out var info))
                {
                    return null;
                }

                var assemblyPath = info.Path;
                if (!File.Exists(assemblyPath))
                {
                    throw new InvalidOperationException(
                        $"Referenced assembly '{name}' was not found in '{toolsDirectory}'.");
                }

                return loadContext.LoadFromAssemblyPath(assemblyPath);
            };

#elif NET461
            AppDomain.CurrentDomain.AssemblyResolve += (source, eventArgs) =>
            {
                var assemblyName = new AssemblyName(eventArgs.Name);
                var name = assemblyName.Name;
                if (!packagedAssemblies.TryGetValue(name, out var info))
                {
                    return null;
                }

                var assembly = info.Assembly;
                if (assembly != null)
                {
                    // Loaded already
                    return assembly;
                }

                var assemblyPath = info.Path;
                if (!File.Exists(assemblyPath))
                {
                    throw new InvalidOperationException(
                        $"Referenced assembly '{name}' was not found in '{toolsDirectory}'.");
                }

                return Assembly.LoadFile(assemblyPath);
            };
#else
#error target frameworks need to be updated.
#endif

            // Now safe to reference the application's code.
            try
            {
                var assemblyPath = AssemblyPath.Value();
                var context = new GetDocumentCommandContext
                {
                    AssemblyPath = assemblyPath,
                    AssemblyDirectory = Path.GetDirectoryName(assemblyPath),
                    AssemblyName = Path.GetFileNameWithoutExtension(assemblyPath),
                    DocumentName = _documentName.Value(),
                    Method = _method.Value(),
                    OutputPath = _output.Value(),
                    Service = _service.Value(),
                };

                return GetDocumentCommandWorker.Process(context);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        private class AssemblyInfo
        {
            public AssemblyInfo(string path)
            {
                Path = path;
            }

            public string Path { get; }

            public Assembly Assembly { get; set; }
        }
    }
}
