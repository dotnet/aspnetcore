// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class TagHelperBinderSyntaxTreePass : IRazorSyntaxTreePass
    {
        public RazorEngine Engine { get; set; }

        public int Order => 150;

        public RazorSyntaxTree Execute(RazorCodeDocument codeDocument, RazorSyntaxTree syntaxTree)
        {
            var resolver = Engine.Features.OfType<TagHelperFeature>().FirstOrDefault()?.Resolver;
            if (resolver == null)
            {
                // No resolver, nothing to do.
                return syntaxTree;
            }

            // We need to find directives in all of the *imports* as well as in the main razor file
            //
            // The imports come logically before the main razor file and are in the order they
            // should be processed.
            var visitor = new Visitor();
            var imports = codeDocument.GetImportSyntaxTrees();
            if (imports != null)
            {
                for (var i = 0; i < imports.Count; i++)
                {
                    var import = imports[i];
                    visitor.VisitBlock(import.Root);
                }
            }
            
            visitor.VisitBlock(syntaxTree.Root);

            var directives = visitor.Directives;
            var errorSink = new ErrorSink();
            var descriptors = resolver.Resolve(new TagHelperDescriptorResolutionContext(directives, errorSink)).ToArray();
            if (descriptors.Length == 0)
            {
                // No TagHelpers, add any errors if we have them.
                if (errorSink.Errors.Count > 0)
                {
                    var errors = CombineErrors(syntaxTree.Diagnostics, errorSink.Errors);
                    return RazorSyntaxTree.Create(syntaxTree.Root, syntaxTree.Source, errors, syntaxTree.Options);
                }

                return syntaxTree;
            }

            var descriptorProvider = new TagHelperDescriptorProvider(descriptors);
            var rewriter = new TagHelperParseTreeRewriter(descriptorProvider);
            var rewrittenRoot = rewriter.Rewrite(syntaxTree.Root, errorSink);
            var diagnostics = syntaxTree.Diagnostics;

            if (errorSink.Errors.Count > 0)
            {
                diagnostics = CombineErrors(diagnostics, errorSink.Errors);
            }

            var newSyntaxTree = RazorSyntaxTree.Create(rewrittenRoot, syntaxTree.Source, diagnostics, syntaxTree.Options);
            return newSyntaxTree;
        }

        private IReadOnlyList<RazorError> CombineErrors(IReadOnlyList<RazorError> errors1, IReadOnlyList<RazorError> errors2)
        {
            var combinedErrors = new List<RazorError>(errors1.Count + errors2.Count);
            combinedErrors.AddRange(errors1);
            combinedErrors.AddRange(errors2);

            return combinedErrors;
        }

        private class Visitor : ParserVisitor
        {
            public List<TagHelperDirectiveDescriptor> Directives { get; } = new List<TagHelperDirectiveDescriptor>();

            public override void VisitAddTagHelperSpan(AddTagHelperChunkGenerator chunkGenerator, Span span)
            {
                Directives.Add(CreateDirective(span, chunkGenerator.LookupText, TagHelperDirectiveType.AddTagHelper));
            }

            public override void VisitRemoveTagHelperSpan(RemoveTagHelperChunkGenerator chunkGenerator, Span span)
            {
                Directives.Add(CreateDirective(span, chunkGenerator.LookupText, TagHelperDirectiveType.RemoveTagHelper));
            }

            public override void VisitTagHelperPrefixDirectiveSpan(TagHelperPrefixDirectiveChunkGenerator chunkGenerator, Span span)
            {
                Directives.Add(CreateDirective(span, chunkGenerator.Prefix, TagHelperDirectiveType.TagHelperPrefix));
            }

            private TagHelperDirectiveDescriptor CreateDirective(
                Span span,
                string directiveText,
                TagHelperDirectiveType directiveType)
            {
                directiveText = directiveText.Trim();

                // If this is the "string literal" form of a directive, we'll need to postprocess the location
                // and content.
                //
                // Ex: @addTagHelper "*, Microsoft.AspNetCore.CoolLibrary"
                //                    ^                                 ^
                //                  Start                              End
                var directiveStart = span.Start;
                if (span.Symbols.Count == 1 && (span.Symbols[0] as CSharpSymbol)?.Type == CSharpSymbolType.StringLiteral)
                {
                    var offset = span.Content.IndexOf(directiveText, StringComparison.Ordinal);

                    // This is safe because inside one of these directives all of the text needs to be on the
                    // same line.
                    var original = span.Start;
                    directiveStart = new SourceLocation(
                        original.FilePath,
                        original.AbsoluteIndex + offset,
                        original.LineIndex,
                        original.CharacterIndex + offset);
                }

                var directiveDescriptor = new TagHelperDirectiveDescriptor
                {
                    DirectiveText = directiveText,
                    Location = directiveStart,
                    DirectiveType = directiveType,
                };

                return directiveDescriptor;
            }
        }
    }
}
