// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.OpenApi.Commands
{
    internal class AddURLCommand : BaseCommand
    {
        private const string CommandName = "url";
        private const string DefaultOpenAPIDir = "openapi";
        private const string DefaultOpenAPIFile = "openapi.json";

        private const string OutputFileName = "--output-file";

        public AddURLCommand(AddCommand parent)
            : base(parent, CommandName)
        {
            _outputFileOption = Option(OutputFileName, "The destination to download the remote Open API file to.", CommandOptionType.SingleValue);
            _sourceFileArg = Argument(SourceProjectArgName, $"The openapi file to add. This must be a URL to a remote openapi file.", multipleValues: true);
        }

        internal readonly CommandOption _outputFileOption;

        internal readonly CommandArgument _sourceFileArg;

        protected override async Task<int> ExecuteCoreAsync()
        {
            var projectFilePath = ResolveProjectFile(ProjectFileOption);

            var sourceFile = Ensure.NotNullOrEmpty(_sourceFileArg.Value, SourceProjectArgName);

            string outputFile;
            if (_outputFileOption.HasValue())
            {
                outputFile = _outputFileOption.Value();
            }
            else
            {
                outputFile = Path.Combine(DefaultOpenAPIDir, DefaultOpenAPIFile);
            }
            var codeGenerator = CodeGenerator.NSwagCSharp;
            EnsurePackagesInProject(projectFilePath, codeGenerator);

            if (IsUrl(sourceFile))
            {
                var destination = GetFullPath(outputFile);
                // We have to download the file from that URL, save it to a local file, then create a AddServiceLocalReference
                // Use this task https://github.com/aspnet/AspNetCore/commit/91dcbd44c10af893374cfb36dc7a009caa4818d0#diff-ea7515a116529b85ad5aa8e06e4acc8e
                await DownloadToFileAsync(sourceFile, destination, overwrite: false);

                AddServiceReference(OpenApiReference, projectFilePath, outputFile, sourceFile);
            }
            else
            {
                Error.Write($"{SourceProjectArgName} was not valid. Valid values are URLs");
                return 1;
            }

            return 0;
        }

        protected override bool ValidateArguments()
        {
            Ensure.NotNullOrEmpty(_sourceFileArg.Value, SourceProjectArgName);
            return true;
        }
    }
}
