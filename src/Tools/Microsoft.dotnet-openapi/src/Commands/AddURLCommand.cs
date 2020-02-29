// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Openapi.Tools;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.OpenApi.Commands
{
    internal class AddURLCommand : BaseCommand
    {
        private const string CommandName = "url";

        private const string OutputFileName = "--output-file";
        private const string SourceUrlArgName = "source-URL";

        public AddURLCommand(AddCommand parent, IHttpClientWrapper httpClient)
            : base(parent, CommandName, httpClient)
        {
            _codeGeneratorOption = Option("-c|--code-generator", "The code generator to use. Defaults to 'NSwagCSharp'.", CommandOptionType.SingleValue);
            _outputFileOption = Option(OutputFileName, "The destination to download the remote OpenAPI file to.", CommandOptionType.SingleValue);
            _sourceFileArg = Argument(SourceUrlArgName, $"The OpenAPI file to add. This must be a URL to a remote OpenAPI file.", multipleValues: true);
        }

        internal readonly CommandOption _outputFileOption;

        internal readonly CommandArgument _sourceFileArg;
        internal readonly CommandOption _codeGeneratorOption;

        protected override async Task<int> ExecuteCoreAsync()
        {
            var projectFilePath = ResolveProjectFile(ProjectFileOption);

            var sourceFile = Ensure.NotNullOrEmpty(_sourceFileArg.Value, SourceUrlArgName);
            var codeGenerator = GetCodeGenerator(_codeGeneratorOption);

            // We have to download the file from that URL, save it to a local file, then create a OpenApiReference
            var outputFile = await DownloadGivenOption(sourceFile, _outputFileOption);

            await AddOpenAPIReference(OpenApiReference, projectFilePath, outputFile, codeGenerator, sourceFile);

            return 0;
        }

        protected override bool ValidateArguments()
        {
            ValidateCodeGenerator(_codeGeneratorOption);
            var sourceFile = Ensure.NotNullOrEmpty(_sourceFileArg.Value, SourceUrlArgName);
            if (!IsUrl(sourceFile))
            {
                Error.Write($"{SourceUrlArgName} was not valid. Valid values are URLs");
                return false;
            }
            return true;
        }
    }
}
