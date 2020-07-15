// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language
{
#pragma warning disable CS0618 // Type or member is obsolete
    internal class DefaultRazorParsingPhase : RazorEnginePhaseBase, IRazorParsingPhase
    {
        private IRazorParserOptionsFeature _optionsFeature;

        protected override void OnIntialized()
        {
            _optionsFeature = GetRequiredFeature<IRazorParserOptionsFeature>();
        }

        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var options = codeDocument.GetParserOptions() ??_optionsFeature.GetOptions();
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
#pragma warning restore CS0618 // Type or member is obsolete
}
