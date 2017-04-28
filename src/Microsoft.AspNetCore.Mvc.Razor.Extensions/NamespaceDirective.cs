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

        public static readonly DirectiveDescriptor Directive = DirectiveDescriptorBuilder.Create("namespace").AddNamespace().Build();

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
        internal class Pass : RazorIRPassBase, IRazorDirectiveClassifierPass
        {
            public override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
            {
                if (irDocument.DocumentKind != RazorPageDocumentClassifierPass.RazorPageDocumentKind &&
                    irDocument.DocumentKind != MvcViewDocumentClassifierPass.MvcViewDocumentKind)
                {
                    // Not a page. Skip.
                    return;
                }

                var visitor = new Visitor();
                visitor.Visit(irDocument);

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
                
                if (TryComputeNamespace(codeDocument.Source.FileName, directive, out var computedNamespace))
                {
                    // Beautify the class name since we're using a hierarchy for namespaces.
                    var @class = visitor.FirstClass;
                    if (@class != null && irDocument.DocumentKind == RazorPageDocumentClassifierPass.RazorPageDocumentKind)
                    {
                        @class.Name = Path.GetFileNameWithoutExtension(codeDocument.Source.FileName) + "_Page";
                    }
                    else if (@class != null && irDocument.DocumentKind == MvcViewDocumentClassifierPass.MvcViewDocumentKind)
                    {
                        @class.Name = Path.GetFileNameWithoutExtension(codeDocument.Source.FileName) + "_View";
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
        // In the event that these two source either don't have filenames set or don't follow a coherent hierarchy,
        // we will just use the namespace verbatim.
        internal static bool TryComputeNamespace(string source, DirectiveIRNode directive, out string @namespace)
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

            // Skip the last segment because it's the filename.
            for (var i = 0; i < segments.Length - 1; i++)
            {
                builder.Append('.');
                builder.Append(segments[i]);
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

        private class Visitor : RazorIRNodeWalker
        {
            public ClassDeclarationIRNode FirstClass { get; private set; }

            public NamespaceDeclarationIRNode FirstNamespace { get; private set; }

            // We want the last one, so get them all and then .
            public DirectiveIRNode LastNamespaceDirective { get; private set; }

            public override void VisitNamespace(NamespaceDeclarationIRNode node)
            {
                if (FirstNamespace == null)
                {
                    FirstNamespace = node;
                }

                base.VisitNamespace(node);
            }

            public override void VisitClass(ClassDeclarationIRNode node)
            {
                if (FirstClass == null)
                {
                    FirstClass = node;
                }

                base.VisitClass(node);
            }

            public override void VisitDirective(DirectiveIRNode node)
            {
                if (node.Descriptor == Directive)
                {
                    LastNamespaceDirective = node;
                }

                base.VisitDirective(node);
            }
        }
    }
}
