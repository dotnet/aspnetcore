// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
#if NETCOREAPP
using System.Runtime.Loader;
#endif
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.ApiDescription.Tool.Commands;

internal sealed class GetDocumentCommand : ProjectCommandBase
{
    private CommandOption _fileListPath;
    private CommandOption _output;
    private CommandOption _openApiVersion;
    private CommandOption _documentName;
    private CommandOption _fileName;

    public GetDocumentCommand(IConsole console) : base(console)
    {
    }

    public override void Configure(CommandLineApplication command)
    {
        base.Configure(command);

        _fileListPath = command.Option("--file-list <Path>", Resources.FileListDescription);
        _output = command.Option("--output <Directory>", Resources.OutputDescription);
        _openApiVersion = command.Option("--openapi-version <Version>", Resources.OpenApiVersionDescription);
        _documentName = command.Option("--document-name <Name>", Resources.DocumentNameDescription);
        _fileName = command.Option("--file-name <Name>", Resources.FileNameDescription);
    }

    protected override void Validate()
    {
        base.Validate();

        if (!_fileListPath.HasValue())
        {
            throw new CommandException(Resources.FormatMissingOption(_fileListPath.LongName));
        }

        if (!_output.HasValue())
        {
            throw new CommandException(Resources.FormatMissingOption(_output.LongName));
        }

        // No need to validate --openapi-version, we'll fallback to whatever is configured by
        // the runtime in the event that none is provided.

        // No need to validate --document-name, we'll fallback to generating OpenAPI files for
        // documents registered in the application in the event that none is provided.
    }

    protected override int Execute()
    {
        var thisAssembly = typeof(GetDocumentCommand).Assembly;

        var toolsDirectory = ToolsDirectory.Value();
        var packagedAssemblies = Directory
            .EnumerateFiles(toolsDirectory, "*.dll")
            .Except([Path.GetFullPath(thisAssembly.Location)])
            .ToDictionary(Path.GetFileNameWithoutExtension, path => new AssemblyInfo(path));

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

#if NETCOREAPP
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

#elif NETFRAMEWORK
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
#error Target frameworks need to be updated.
#endif

        // Now safe to reference the application's code.
        try
        {
            var assemblyPath = AssemblyPath.Value();
            var context = new GetDocumentCommandContext
            {
                AssemblyPath = assemblyPath,
                AssemblyName = Path.GetFileNameWithoutExtension(assemblyPath),
                FileListPath = _fileListPath.Value(),
                OutputDirectory = _output.Value(),
                OpenApiVersion = _openApiVersion.Value(),
                DocumentName = _documentName.Value(),
                ProjectName = ProjectName.Value(),
                Reporter = Reporter,
                FileName = _fileName.Value()
            };

            return new GetDocumentCommandWorker(context).Process();
        }
        catch (Exception ex)
        {
            Reporter.WriteError(ex.ToString());
            return 2;
        }
    }

    private sealed class AssemblyInfo
    {
        public AssemblyInfo(string path)
        {
            Path = path;
        }

        public string Path { get; }

        public Assembly Assembly { get; set; }
    }
}
