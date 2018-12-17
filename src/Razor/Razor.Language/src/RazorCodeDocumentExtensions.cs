// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class RazorCodeDocumentExtensions
    {
        public static TagHelperDocumentContext GetTagHelperContext(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return (TagHelperDocumentContext)document.Items[typeof(TagHelperDocumentContext)];
        }

        public static void SetTagHelperContext(this RazorCodeDocument document, TagHelperDocumentContext context)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            document.Items[typeof(TagHelperDocumentContext)] = context;
        }

        public static RazorSyntaxTree GetSyntaxTree(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return document.Items[typeof(RazorSyntaxTree)] as RazorSyntaxTree;
        }

        public static void SetSyntaxTree(this RazorCodeDocument document, RazorSyntaxTree syntaxTree)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            document.Items[typeof(RazorSyntaxTree)] = syntaxTree;
        }

        public static IReadOnlyList<RazorSyntaxTree> GetImportSyntaxTrees(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return (document.Items[typeof(ImportSyntaxTreesHolder)] as ImportSyntaxTreesHolder)?.SyntaxTrees;
        }

        public static void SetImportSyntaxTrees(this RazorCodeDocument document, IReadOnlyList<RazorSyntaxTree> syntaxTrees)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            document.Items[typeof(ImportSyntaxTreesHolder)] = new ImportSyntaxTreesHolder(syntaxTrees);
        }

        public static DocumentIntermediateNode GetDocumentIntermediateNode(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return document.Items[typeof(DocumentIntermediateNode)] as DocumentIntermediateNode;
        }

        public static void SetDocumentIntermediateNode(this RazorCodeDocument document, DocumentIntermediateNode documentNode)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            document.Items[typeof(DocumentIntermediateNode)] = documentNode;
        }

        public static RazorCSharpDocument GetCSharpDocument(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return (RazorCSharpDocument)document.Items[typeof(RazorCSharpDocument)];
        }

        public static void SetCSharpDocument(this RazorCodeDocument document, RazorCSharpDocument csharp)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            document.Items[typeof(RazorCSharpDocument)] = csharp;
        }

        public static RazorParserOptions GetParserOptions(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return (RazorParserOptions)document.Items[typeof(RazorParserOptions)];
        }

        public static void SetParserOptions(this RazorCodeDocument document, RazorParserOptions parserOptions)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            document.Items[typeof(RazorParserOptions)] = parserOptions;
        }

        public static RazorCodeGenerationOptions GetCodeGenerationOptions(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return (RazorCodeGenerationOptions)document.Items[typeof(RazorCodeGenerationOptions)];
        }

        public static void SetCodeGenerationOptions(this RazorCodeDocument document, RazorCodeGenerationOptions codeGenerationOptions)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            document.Items[typeof(RazorCodeGenerationOptions)] = codeGenerationOptions;
        }

        private class ImportSyntaxTreesHolder
        {
            public ImportSyntaxTreesHolder(IReadOnlyList<RazorSyntaxTree> syntaxTrees)
            {
                SyntaxTrees = syntaxTrees;
            }

            public IReadOnlyList<RazorSyntaxTree> SyntaxTrees { get; }
        }

        private class IncludeSyntaxTreesHolder
        {
            public IncludeSyntaxTreesHolder(IReadOnlyList<RazorSyntaxTree> syntaxTrees)
            {
                SyntaxTrees = syntaxTrees;
            }

            public IReadOnlyList<RazorSyntaxTree> SyntaxTrees { get; }
        }
    }
}
