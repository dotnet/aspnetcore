// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorParsingPhase : RazorEnginePhaseBase, IRazorParsingPhase
    {
        private IRazorParserOptionsFeature[] _parserOptionsCallbacks;

        protected override void OnIntialized()
        {
            _parserOptionsCallbacks = Engine.Features.OfType<IRazorParserOptionsFeature>().ToArray();
        }

        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var options = RazorParserOptions.CreateDefaultOptions();
            for (var i = 0; i < _parserOptionsCallbacks.Length; i++)
            {
                _parserOptionsCallbacks[i].Configure(options);
            }

            var syntaxTree = RazorSyntaxTree.Parse(codeDocument.Source, options);
            codeDocument.SetSyntaxTree(syntaxTree);

            var importSyntaxTrees = new RazorSyntaxTree[codeDocument.Imports.Count];
            for (var i = 0; i < codeDocument.Imports.Count; i++)
            {
                importSyntaxTrees[i] = RazorSyntaxTree.Parse(codeDocument.Imports[i], options);
            }
            codeDocument.SetImportSyntaxTrees(importSyntaxTrees);
        }
    }
}
