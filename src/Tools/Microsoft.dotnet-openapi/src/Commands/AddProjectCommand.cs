// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Openapi.Tools;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.DotNet.OpenApi.Commands
{
    internal class AddProjectCommand : BaseCommand
    {
        private const string CommandName = "project";

        private const string SourceProjectArgName = "source-project";

        public AddProjectCommand(BaseCommand parent, IHttpClientWrapper httpClient)
            : base(parent, CommandName, httpClient)
        {
            _codeGeneratorOption = Option("-c|--code-generator", "The code generator to use. Defaults to 'NSwagCSharp'.", CommandOptionType.SingleValue);
            _sourceProjectArg = Argument(SourceProjectArgName, $"The OpenAPI project to add. This must be the path to project file(s) containing OpenAPI endpoints", multipleValues: true);
        }

        internal readonly CommandArgument _sourceProjectArg;
        internal readonly CommandOption _codeGeneratorOption;

        protected override async Task<int> ExecuteCoreAsync()
        {
            var projectFilePath = ResolveProjectFile(ProjectFileOption);

            var codeGenerator = GetCodeGenerator(_codeGeneratorOption);

            foreach (var sourceFile in _sourceProjectArg.Values)
            {
                await AddOpenAPIReference(OpenApiProjectReference, projectFilePath, sourceFile, codeGenerator);
            }

            return 0;
        }

        protected override bool ValidateArguments()
        {
            ValidateCodeGenerator(_codeGeneratorOption);
            foreach (var sourceFile in _sourceProjectArg.Values)
            {
                if (!IsProjectFile(sourceFile))
                {
                    throw new ArgumentException($"{SourceProjectArgName} of '{sourceFile}' was not valid. Valid values must be project file(s)");
                }
            }

            Ensure.NotNullOrEmpty(_sourceProjectArg.Value, SourceProjectArgName);
            return true;
        }
    }
}
