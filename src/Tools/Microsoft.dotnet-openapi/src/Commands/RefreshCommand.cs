// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;
using Microsoft.DotNet.Openapi.Tools;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.OpenApi.Commands
{
    internal class RefreshCommand : BaseCommand
    {
        private const string CommandName = "refresh";

        private const string SourceURLArgName = "source-URL";

        public RefreshCommand(Application parent, IHttpClientWrapper httpClient) : base(parent, CommandName, httpClient)
        {
            _sourceFileArg = Argument(SourceURLArgName, $"The OpenAPI reference to refresh.");
        }

        internal readonly CommandArgument _sourceFileArg;

        protected override async Task<int> ExecuteCoreAsync()
        {
            var projectFile = ResolveProjectFile(ProjectFileOption);

            var sourceFile = Ensure.NotNullOrEmpty(_sourceFileArg.Value, SourceURLArgName);

            var destination = FindReferenceFromUrl(projectFile, sourceFile);
            await DownloadToFileAsync(sourceFile, destination, overwrite: true);

            return 0;
        }

        private string FindReferenceFromUrl(FileInfo projectFile, string url)
        {
            var project = LoadProject(projectFile);
            var openApiReferenceItems = project.GetItems(OpenApiReference);

            foreach (ProjectItem item in openApiReferenceItems)
            {
                var attrUrl = item.GetMetadataValue(SourceUrlAttrName);
                if (string.Equals(attrUrl, url, StringComparison.Ordinal))
                {
                    return item.EvaluatedInclude;
                }
            }

            throw new ArgumentException("There was no OpenAPI reference to refresh with the given URL.");
        }

        protected override bool ValidateArguments()
        {
            var sourceFile = Ensure.NotNullOrEmpty(_sourceFileArg.Value, SourceURLArgName);
            if (!IsUrl(sourceFile))
            {
                throw new ArgumentException($"'dotnet openapi refresh' must be given a URL");
            }

            return true;
        }
    }
}
