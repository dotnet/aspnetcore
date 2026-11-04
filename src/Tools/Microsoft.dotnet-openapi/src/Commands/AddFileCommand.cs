// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Openapi.Tools;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.OpenApi.Commands;

internal sealed class AddFileCommand : BaseCommand
{
    private const string CommandName = "file";

    private const string SourceFileArgName = "source-file";

    public AddFileCommand(AddCommand parent, IHttpClientWrapper httpClient)
        : base(parent, CommandName, httpClient)
    {
        _codeGeneratorOption = Option("-c|--code-generator", "The code generator to use. Defaults to 'NSwagCSharp'.", CommandOptionType.SingleValue);
        _sourceFileArg = Argument(SourceFileArgName, $"The OpenAPI file to add. This must be a path to local OpenAPI file(s)", multipleValues: true);
    }

    internal readonly CommandArgument _sourceFileArg;
    internal readonly CommandOption _codeGeneratorOption;

    private readonly string[] ApprovedExtensions = new[] { ".json", ".yaml", ".yml" };

    protected override async Task<int> ExecuteCoreAsync()
    {
        ArgumentException.ThrowIfNullOrEmpty(_sourceFileArg.Value);

        var projectFilePath = ResolveProjectFile(ProjectFileOption);
        var codeGenerator = GetCodeGenerator(_codeGeneratorOption);

        foreach (var sourceFile in _sourceFileArg.Values)
        {
            if (!ApprovedExtensions.Any(e => sourceFile.EndsWith(e, StringComparison.Ordinal)))
            {
                await Warning.WriteLineAsync($"The extension for the given file '{sourceFile}' should have been one of: {string.Join(",", ApprovedExtensions)}.");
                await Warning.WriteLineAsync($"The reference has been added, but may fail at build-time if the format is not correct.");
            }
            await AddOpenAPIReference(OpenApiReference, projectFilePath, sourceFile, codeGenerator);
        }

        return 0;
    }

    private bool IsLocalFile(string file)
    {
        return File.Exists(GetFullPath(file));
    }

    protected override bool ValidateArguments()
    {
        ValidateCodeGenerator(_codeGeneratorOption);

        try
        {
            ArgumentException.ThrowIfNullOrEmpty(_sourceFileArg.Value);
        }
        catch (ArgumentException ex)
        {
            Error.Write(ex.Message);
            return false;
        }

        foreach (var sourceFile in _sourceFileArg.Values)
        {
            if (!IsLocalFile(sourceFile))
            {
                Error.Write($"{SourceFileArgName} of '{sourceFile}' could not be found.");
            }
        }
        return true;
    }
}
