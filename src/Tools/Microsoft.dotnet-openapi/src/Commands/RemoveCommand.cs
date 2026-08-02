// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.DotNet.Openapi.Tools;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.OpenApi.Commands;

internal sealed class RemoveCommand : BaseCommand
{
    private const string CommandName = "remove";

    private const string SourceArgName = "source";

    public RemoveCommand(Application parent, IHttpClientWrapper httpClient) : base(parent, CommandName, httpClient)
    {
        _sourceProjectArg = Argument(SourceArgName, "The OpenAPI reference to remove. Must represent a reference which is already in this project", multipleValues: true);
    }

    internal readonly CommandArgument _sourceProjectArg;

    protected override Task<int> ExecuteCoreAsync()
    {
        ArgumentException.ThrowIfNullOrEmpty(_sourceProjectArg.Value);

        var projectFile = ResolveProjectFile(ProjectFileOption);
        var sourceFile = _sourceProjectArg.Value;

        if (IsProjectFile(sourceFile))
        {
            RemoveServiceReference(OpenApiProjectReference, projectFile, sourceFile);
        }
        else
        {
            var file = RemoveServiceReference(OpenApiReference, projectFile, sourceFile);

            if (file != null)
            {
                File.Delete(GetFullPath(file));
            }
        }

        return Task.FromResult(0);
    }

    private string RemoveServiceReference(string tagName, FileInfo projectFile, string sourceFile)
    {
        var project = LoadProject(projectFile);
        var openApiReferenceItems = project.GetItems(tagName);

        foreach (ProjectItem item in openApiReferenceItems)
        {
            var include = item.EvaluatedInclude;
            var sourceUrl = item.HasMetadata(SourceUrlAttrName) ? item.GetMetadataValue(SourceUrlAttrName) : null;
            if (string.Equals(include, sourceFile, StringComparison.OrdinalIgnoreCase)
                || string.Equals(sourceUrl, sourceFile, StringComparison.OrdinalIgnoreCase))
            {
                project.RemoveItem(item);
                project.Save();
                return include;
            }
        }

        Warning.Write($"No OpenAPI reference was found with the file '{sourceFile}'");
        return null;
    }

    protected override bool ValidateArguments()
    {
        ArgumentException.ThrowIfNullOrEmpty(_sourceProjectArg.Value);
        return true;
    }
}
