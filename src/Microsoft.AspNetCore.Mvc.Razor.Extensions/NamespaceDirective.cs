// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public static class NamespaceDirective
    {
        private static readonly char[] Separators = new char[] { '\\', '/' };

        public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
            "namespace",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.AddNamespaceToken();
                builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
            });

        public static void Register(IRazorEngineBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException();
            }

            builder.AddDirective(Directive);
            builder.Features.Add(new Pass());
        }

        // internal for testing
        internal class Pass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
        {
            protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
            {
                if (documentNode.DocumentKind != RazorPageDocumentClassifierPass.RazorPageDocumentKind &&
                    documentNode.DocumentKind != MvcViewDocumentClassifierPass.MvcViewDocumentKind)
                {
                    // Not a page. Skip.
                    return;
                }

                var visitor = new Visitor();
                visitor.Visit(documentNode);

                var directive = visitor.LastNamespaceDirective;
                if (directive == null)
                {
                    // No namespace set. Skip.
                    return;
                }

                var @namespace = visitor.FirstNamespace;
                if (@namespace == null)
                {
                    // No namespace node. Skip.
                    return;
                }

                if (TryComputeNamespace(codeDocument.Source.FilePath, directive, out var computedNamespace))
                {
                    // Beautify the class name since we're using a hierarchy for namespaces.
                    var @class = visitor.FirstClass;
                    var prefix = CSharpIdentifier.SanitizeClassName(Path.GetFileNameWithoutExtension(codeDocument.Source.FilePath));
                    if (@class != null && documentNode.DocumentKind == RazorPageDocumentClassifierPass.RazorPageDocumentKind)
                    {
                        @class.ClassName = prefix + "_Page";
                    }
                    else if (@class != null && documentNode.DocumentKind == MvcViewDocumentClassifierPass.MvcViewDocumentKind)
                    {
                        @class.ClassName = prefix + "_View";
                    }
                }

                @namespace.Content = computedNamespace;
            }
        }

        // internal for testing.
        //
        // This code does a best-effort attempt to compute a namespace 'suffix' - the path difference between
        // where the @namespace directive appears and where the current document is on disk.
        //
        // In the event that these two source either don't have FileNames set or don't follow a coherent hierarchy,
        // we will just use the namespace verbatim.
        internal static bool TryComputeNamespace(string source, DirectiveIntermediateNode directive, out string @namespace)
        {
            var directiveSource = NormalizeDirectory(directive.Source?.FilePath);

            var baseNamespace = directive.Tokens.FirstOrDefault()?.Content;
            if (string.IsNullOrEmpty(baseNamespace))
            {
                // The namespace directive was incomplete.
                @namespace = string.Empty;
                return false;
            }

            if (string.IsNullOrEmpty(source) || directiveSource == null)
            {
                // No sources, can't compute a suffix.
                @namespace = baseNamespace;
                return false;
            }

            // We're specifically using OrdinalIgnoreCase here because Razor treats all paths as case-insensitive.
            if (!source.StartsWith(directiveSource, StringComparison.OrdinalIgnoreCase) ||
                source.Length <= directiveSource.Length)
            {
                // The imports are not from the directory hierarchy, can't compute a suffix.
                @namespace = baseNamespace;
                return false;
            }

            // OK so that this point we know that the 'imports' file containing this directive is in the directory
            // hierarchy of this soure file. This is the case where we can append a suffix to the baseNamespace.
            //
            // Everything so far has just been defensiveness on our part.

            var builder = new StringBuilder(baseNamespace);

            var segments = source.Substring(directiveSource.Length).Split(Separators);

            // Skip the last segment because it's the FileName.
            for (var i = 0; i < segments.Length - 1; i++)
            {
                builder.Append('.');
                builder.Append(CSharpIdentifier.SanitizeClassName(segments[i]));
            }

            @namespace = builder.ToString();
            return true;
        }

        // We want to normalize the path of the file containing the '@namespace' directive to just the containing
        // directory with a trailing separator.
        //
        // Not using Path.GetDirectoryName here because it doesn't meet these requirements, and we want to handle
        // both 'view engine' style paths and absolute paths.
        //
        // We also don't normalize the separators here. We expect that all documents are using a consistent style of path.
        // 
        // If we can't normalize the path, we just return null so it will be ignored.
        private static string NormalizeDirectory(string path)
        {
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

        private class Visitor : IntermediateNodeWalker
        {
            public ClassDeclarationIntermediateNode FirstClass { get; private set; }

            public NamespaceDeclarationIntermediateNode FirstNamespace { get; private set; }

            // We want the last one, so get them all and then .
            public DirectiveIntermediateNode LastNamespaceDirective { get; private set; }

            public override void VisitNamespaceDeclaration(NamespaceDeclarationIntermediateNode node)
            {
                if (FirstNamespace == null)
                {
                    FirstNamespace = node;
                }

                base.VisitNamespaceDeclaration(node);
            }

            public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
            {
                if (FirstClass == null)
                {
                    FirstClass = node;
                }

                base.VisitClassDeclaration(node);
            }

            public override void VisitDirective(DirectiveIntermediateNode node)
            {
                if (node.Directive == Directive)
                {
                    LastNamespaceDirective = node;
                }

                base.VisitDirective(node);
            }
        }
    }
}
