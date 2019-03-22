// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class RazorCodeDocumentExtensions
    {
        private static readonly char[] PathSeparators = new char[] { '/', '\\' };
        private static readonly char[] NamespaceSeparators = new char[] { '.' };

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

        internal static IReadOnlyList<TagHelperDescriptor> GetTagHelpers(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return (document.Items[typeof(TagHelpersHolder)] as TagHelpersHolder)?.TagHelpers;
        }

        internal static void SetTagHelpers(this RazorCodeDocument document, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            document.Items[typeof(TagHelpersHolder)] = new TagHelpersHolder(tagHelpers);
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

        public static string GetFileKind(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return (string)document.Items[typeof(FileKinds)];
        }

        public static void SetFileKind(this RazorCodeDocument document, string fileKind)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            document.Items[typeof(FileKinds)] = fileKind;
        }

        // In general documents will have a relative path (relative to the project root).
        // We can only really compute a nice class/namespace when we know a relative path.
        //
        // However all kinds of thing are possible in tools. We shouldn't barf here if the document isn't 
        // set up correctly.
        internal static bool TryComputeNamespaceAndClass(this RazorCodeDocument document, out string @namespace, out string @class)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var filePath = document.Source.FilePath;
            var relativePath = document.Source.RelativePath;
            if (filePath == null || relativePath == null || filePath.Length <= relativePath.Length)
            {
                @namespace = null;
                @class = null;
                return false;
            }

            filePath = NormalizePath(filePath);
            relativePath = NormalizePath(relativePath);
            var options = document.GetCodeGenerationOptions() ?? document.GetDocumentIntermediateNode()?.Options;
            var rootNamespace = options?.RootNamespace;
            if (string.IsNullOrEmpty(rootNamespace))
            {
                @namespace = null;
                @class = null;
                return false;
            }

            var builder = new StringBuilder();

            // Sanitize the base namespace, but leave the dots.
            var segments = rootNamespace.Split(NamespaceSeparators, StringSplitOptions.RemoveEmptyEntries);
            builder.Append(CSharpIdentifier.SanitizeIdentifier(segments[0]));
            for (var i = 1; i < segments.Length; i++)
            {
                builder.Append('.');
                builder.Append(CSharpIdentifier.SanitizeIdentifier(segments[i]));
            }

            segments = relativePath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);

            // Skip the last segment because it's the FileName.
            for (var i = 0; i < segments.Length - 1; i++)
            {
                builder.Append('.');
                builder.Append(CSharpIdentifier.SanitizeIdentifier(segments[i]));
            }

            @namespace = builder.ToString();
            @class = CSharpIdentifier.SanitizeIdentifier(Path.GetFileNameWithoutExtension(relativePath));

            return true;
        }

        private static string NormalizePath(string path)
        {
            path = path.Replace('\\', '/');

            return path;
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

        private class TagHelpersHolder
        {
            public TagHelpersHolder(IReadOnlyList<TagHelperDescriptor> tagHelpers)
            {
                TagHelpers = tagHelpers;
            }

            public IReadOnlyList<TagHelperDescriptor> TagHelpers { get; }
        }
    }
}
