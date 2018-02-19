// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    // This only exists to support the temporary fake Blazor directives and can
    // be removed once we are able to implement proper Blazor-specific directives
    internal abstract class SourceLinesVisitor
    {
        /// <summary>
        /// Visits each line in the document's imports (in order), followed by
        /// each line in the document's primary syntax tree.
        /// </summary>
        public void Visit(RazorCodeDocument codeDocument)
        {
            foreach (var import in codeDocument.GetImportSyntaxTrees())
            {
                VisitSyntaxTree(import);
            }

            VisitSyntaxTree(codeDocument.GetSyntaxTree());
        }

        protected abstract void VisitLine(string line);

        private void VisitSyntaxTree(RazorSyntaxTree syntaxTree)
        {
            var sourceDocument = syntaxTree.Source;
            foreach (var line in new SourceLinesEnumerable(sourceDocument))
            {
                VisitLine(line);
            }
        }
    }
}
