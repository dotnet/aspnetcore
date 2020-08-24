// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class RazorCodeDocumentExtensions
    {
        private static readonly char[] PathSeparators = new char[] { '/', '\\' };
        private static readonly char[] NamespaceSeparators = new char[] { '.' };
        private static object CssScopeKey = new object();

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

        internal static RazorHtmlDocument GetHtmlDocument(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var razorHtmlObj = document.Items[typeof(RazorHtmlDocument)];
            if (razorHtmlObj == null)
            {
                var razorHtmlDocument = RazorHtmlWriter.GetHtmlDocument(document);
                if (razorHtmlDocument != null)
                {
                    document.Items[typeof(RazorHtmlDocument)] = razorHtmlDocument;
                    return razorHtmlDocument;
                }
            }

            return (RazorHtmlDocument)razorHtmlObj;
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

        public static string GetCssScope(this RazorCodeDocument document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return (string)document.Items[CssScopeKey];
        }

        public static void SetCssScope(this RazorCodeDocument document, string cssScope)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            document.Items[CssScopeKey] = cssScope;
        }

        // In general documents will have a relative path (relative to the project root).
        // We can only really compute a nice namespace when we know a relative path.
        //
        // However all kinds of thing are possible in tools. We shouldn't barf here if the document isn't 
        // set up correctly.
        public static bool TryComputeNamespace(this RazorCodeDocument document, bool fallbackToRootNamespace, out string @namespace)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var filePath = document.Source.FilePath;
            var relativePath = document.Source.RelativePath;
            if (filePath == null || relativePath == null || filePath.Length < relativePath.Length)
            {
                @namespace = null;
                return false;
            }

            relativePath = NormalizePath(relativePath);
            // If the document or it's imports contains a @namespace directive, we want to use that over the root namespace.
            var baseNamespace = string.Empty;
            var appendSuffix = true;
            var lastNamespaceContent = string.Empty;
            var lastNamespaceLocation = SourceSpan.Undefined;
            var importSyntaxTrees = document.GetImportSyntaxTrees();
            if (importSyntaxTrees != null)
            {
                // ImportSyntaxTrees is usually set. Just being defensive.
                foreach (var importSyntaxTree in importSyntaxTrees)
                {
                    if (importSyntaxTree != null && NamespaceVisitor.TryGetLastNamespaceDirective(importSyntaxTree, out var importNamespaceContent, out var importNamespaceLocation))
                    {
                        lastNamespaceContent = importNamespaceContent;
                        lastNamespaceLocation = importNamespaceLocation;
                    }
                }
            }

            var syntaxTree = document.GetSyntaxTree();
            if (syntaxTree != null && NamespaceVisitor.TryGetLastNamespaceDirective(syntaxTree, out var namespaceContent, out var namespaceLocation))
            {
                lastNamespaceContent = namespaceContent;
                lastNamespaceLocation = namespaceLocation;
            }

            // If there are multiple @namespace directives in the heirarchy,
            // we want to pick the closest one to the current document.
            if (!string.IsNullOrEmpty(lastNamespaceContent))
            {
                baseNamespace = lastNamespaceContent;
                var directiveLocationDirectory = NormalizeDirectory(lastNamespaceLocation.FilePath);

                // We're specifically using OrdinalIgnoreCase here because Razor treats all paths as case-insensitive.
                if (!document.Source.FilePath.StartsWith(directiveLocationDirectory, StringComparison.OrdinalIgnoreCase) ||
                    document.Source.FilePath.Length <= directiveLocationDirectory.Length)
                {
                    // The most relevant directive is not from the directory hierarchy, can't compute a suffix.
                    appendSuffix = false;
                }
                else
                {
                    // We know that the document containing the namespace directive is in the current document's heirarchy.
                    // Let's compute the actual relative path that we'll use to compute the namespace suffix.
                    relativePath = document.Source.FilePath.Substring(directiveLocationDirectory.Length);
                }
            }
            else if (fallbackToRootNamespace)
            {
                var options = document.GetCodeGenerationOptions() ?? document.GetDocumentIntermediateNode()?.Options;
                baseNamespace = options?.RootNamespace;
                appendSuffix = true;
            }

            if (string.IsNullOrEmpty(baseNamespace))
            {
                // There was no valid @namespace directive and we couldn't compute the RootNamespace.
                @namespace = null;
                return false;
            }

            var builder = new StringBuilder();

            // Sanitize the base namespace, but leave the dots.
            var segments = baseNamespace.Split(NamespaceSeparators, StringSplitOptions.RemoveEmptyEntries);
            builder.Append(CSharpIdentifier.SanitizeIdentifier(segments[0]));
            for (var i = 1; i < segments.Length; i++)
            {
                builder.Append('.');
                builder.Append(CSharpIdentifier.SanitizeIdentifier(segments[i]));
            }

            if (appendSuffix)
            {
                // If we get here, we already have a base namespace and the relative path that should be used as the namespace suffix.
                segments = relativePath.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);

                // Skip the last segment because it's the FileName.
                for (var i = 0; i < segments.Length - 1; i++)
                {
                    builder.Append('.');
                    builder.Append(CSharpIdentifier.SanitizeIdentifier(segments[i]));
                }
            }

            @namespace = builder.ToString();

            return true;

            // We want to normalize the path of the file containing the '@namespace' directive to just the containing
            // directory with a trailing separator.
            //
            // Not using Path.GetDirectoryName here because it doesn't meet these requirements, and we want to handle
            // both 'view engine' style paths and absolute paths.
            //
            // We also don't normalize the separators here. We expect that all documents are using a consistent style of path.
            // 
            // If we can't normalize the path, we just return null so it will be ignored.
            string NormalizeDirectory(string path)
            {
                char[] Separators = new char[] { '\\', '/' };
                if (string.IsNullOrEmpty(path))
                {
                    return null;
                }

                var lastSeparator = path.LastIndexOfAny(Separators);
                if (lastSeparator == -1)
                {
                    return null;
                }

                // Includes the separator
                return path.Substring(0, lastSeparator + 1);
            }
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

        private class NamespaceVisitor : SyntaxWalker
        {
            private readonly RazorSourceDocument _source;

            private NamespaceVisitor(RazorSourceDocument source)
            {
                _source = source;
            }

            public string LastNamespaceContent { get; set; }

            public SourceSpan LastNamespaceLocation { get; set; }

            public static bool TryGetLastNamespaceDirective(
                RazorSyntaxTree syntaxTree,
                out string namespaceDirectiveContent,
                out SourceSpan namespaceDirectiveSpan)
            {
                var visitor = new NamespaceVisitor(syntaxTree.Source);
                visitor.Visit(syntaxTree.Root);
                if (string.IsNullOrEmpty(visitor.LastNamespaceContent))
                {
                    namespaceDirectiveContent = null;
                    namespaceDirectiveSpan = SourceSpan.Undefined;
                    return false;
                }

                namespaceDirectiveContent = visitor.LastNamespaceContent;
                namespaceDirectiveSpan = visitor.LastNamespaceLocation;
                return true;
            }

            public override void VisitRazorDirective(RazorDirectiveSyntax node)
            {
                if (node != null && node.DirectiveDescriptor == NamespaceDirective.Directive)
                {
                    var directiveContent = node.Body?.GetContent();

                    // In practice, this should never be null and always start with 'namespace'. Just being defensive here.
                    if (directiveContent != null && directiveContent.StartsWith(NamespaceDirective.Directive.Directive))
                    {
                        LastNamespaceContent = directiveContent.Substring(NamespaceDirective.Directive.Directive.Length).Trim();
                        LastNamespaceLocation = node.GetSourceSpan(_source);
                    }
                }

                base.VisitRazorDirective(node);
            }
        }
    }
}
