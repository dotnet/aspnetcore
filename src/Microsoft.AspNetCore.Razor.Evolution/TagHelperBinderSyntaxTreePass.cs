// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class TagHelperBinderSyntaxTreePass : IRazorSyntaxTreePass
    {
        private static HashSet<char> InvalidNonWhitespaceNameCharacters = new HashSet<char>(new[]
        {
            '@', '!', '<', '/', '?', '[', '>', ']', '=', '"', '\'', '*'
        });

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
            var visitor = new DirectiveVisitor();
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

            var errorSink = new ErrorSink();
            var directives = visitor.Directives;
            var descriptors = (IReadOnlyList<TagHelperDescriptor>)resolver.Resolve(errorSink).ToList();

            descriptors = ProcessDirectives(directives, descriptors, errorSink);

            if (descriptors.Count == 0)
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

        internal IReadOnlyList<TagHelperDescriptor> ProcessDirectives(
            IReadOnlyList<TagHelperDirectiveDescriptor> directives,
            IReadOnlyList<TagHelperDescriptor> tagHelpers,
            ErrorSink errorSink)
        {
            var matches = new HashSet<TagHelperDescriptor>(TagHelperDescriptorComparer.Default);

            // We only support a single prefix directive.
            TagHelperDirectiveDescriptor prefixDirective = null;

            for (var i = 0; i < directives.Count; i++)
            {
                var directive = directives[i];

                ParsedDirective parsed;
                switch (directive.DirectiveType)
                {
                    case TagHelperDirectiveType.AddTagHelper:

                        parsed = ParseAddOrRemoveDirective(directive, errorSink);
                        if (parsed == null)
                        {
                            // Skip this one, it's an error
                            break;
                        }

                        if (!AssemblyContainsTagHelpers(parsed.AssemblyName, tagHelpers))
                        {
                            errorSink.OnError(
                                parsed.AssemblyNameLocation,
                                Resources.FormatTagHelperAssemblyCouldNotBeResolved(parsed.AssemblyName),
                                parsed.AssemblyName.Length);

                            // Skip this one, it's an error
                            break;
                        }

                        for (var j = 0; j < tagHelpers.Count; j++)
                        {
                            var tagHelper = tagHelpers[j];
                            if (MatchesDirective(tagHelper, parsed))
                            {
                                matches.Add(tagHelper);
                            }
                        }

                        break;

                    case TagHelperDirectiveType.RemoveTagHelper:

                        parsed = ParseAddOrRemoveDirective(directive, errorSink);
                        if (parsed == null)
                        {
                            // Skip this one, it's an error
                            break;
                        }


                        if (!AssemblyContainsTagHelpers(parsed.AssemblyName, tagHelpers))
                        {
                            errorSink.OnError(
                                parsed.AssemblyNameLocation,
                                Resources.FormatTagHelperAssemblyCouldNotBeResolved(parsed.AssemblyName),
                                parsed.AssemblyName.Length);

                            // Skip this one, it's an error
                            break;
                        }

                        for (var j = 0; j < tagHelpers.Count; j++)
                        {
                            var tagHelper = tagHelpers[j];
                            if (MatchesDirective(tagHelper, parsed))
                            {
                                matches.Remove(tagHelper);
                            }
                        }

                        break;

                    case TagHelperDirectiveType.TagHelperPrefix:

                        // We only expect to see a single one of these per file, but that's enforced at another level.
                        prefixDirective = directive;

                        break;
                }
            }

            var prefix = prefixDirective?.DirectiveText;
            if (prefix != null && !IsValidTagHelperPrefix(prefix, prefixDirective.Location, errorSink))
            {
                prefix = null;
            }

            return PrefixDescriptors(prefix, matches);
        }

        private bool AssemblyContainsTagHelpers(string assemblyName, IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            for (var i = 0; i < tagHelpers.Count; i++)
            {
                if (string.Equals(tagHelpers[i].AssemblyName, assemblyName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        // Internal for testing
        internal ParsedDirective ParseAddOrRemoveDirective(TagHelperDirectiveDescriptor directive, ErrorSink errorSink)
        {
            var text = directive.DirectiveText;
            var lookupStrings = text?.Split(new[] { ',' });

            // Ensure that we have valid lookupStrings to work with. The valid format is "typeName, assemblyName"
            if (lookupStrings == null ||
                lookupStrings.Any(string.IsNullOrWhiteSpace) ||
                lookupStrings.Length != 2)
            {
                errorSink.OnError(
                    directive.Location,
                    Resources.FormatInvalidTagHelperLookupText(text),
                    Math.Max(text.Length, 1));

                return null;
            }

            var trimmedAssemblyName = lookupStrings[1].Trim();

            // + 1 is for the comma separator in the lookup text.
            var assemblyNameIndex =
                lookupStrings[0].Length + 1 + lookupStrings[1].IndexOf(trimmedAssemblyName, StringComparison.Ordinal);
            var assemblyNamePrefix = directive.DirectiveText.Substring(0, assemblyNameIndex);
            var assemblyNameLocation = new SourceLocation(
                directive.Location.FilePath,
                directive.Location.AbsoluteIndex + assemblyNameIndex,
                directive.Location.LineIndex,
                directive.Location.CharacterIndex + assemblyNameIndex);

            return new ParsedDirective
            {
                TypePattern = lookupStrings[0].Trim(),
                AssemblyName = trimmedAssemblyName,
                AssemblyNameLocation = assemblyNameLocation,
            };
        }

        // Internal for testing
        internal bool IsValidTagHelperPrefix(
            string prefix,
            SourceLocation directiveLocation,
            ErrorSink errorSink)
        {
            foreach (var character in prefix)
            {
                // Prefixes are correlated with tag names, tag names cannot have whitespace.
                if (char.IsWhiteSpace(character) ||  InvalidNonWhitespaceNameCharacters.Contains(character))
                {
                    errorSink.OnError(
                        directiveLocation,
                        Resources.FormatInvalidTagHelperPrefixValue(
                            SyntaxConstants.CSharp.TagHelperPrefixKeyword,
                            character,
                            prefix),
                        prefix.Length);

                    return false;
                }
            }

            return true;
        }

        private static IReadOnlyList<TagHelperDescriptor> PrefixDescriptors(
            string prefix,
            IEnumerable<TagHelperDescriptor> descriptors)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                return descriptors.Select(descriptor => new TagHelperDescriptor(descriptor)
                {
                    Prefix = prefix
                }).ToList();
            }

            return descriptors.ToList();
        }

        private static bool MatchesDirective(TagHelperDescriptor descriptor, ParsedDirective lookupInfo)
        {
            if (!string.Equals(descriptor.AssemblyName, lookupInfo.AssemblyName, StringComparison.Ordinal))
            {
                return false;
            }

            if (lookupInfo.TypePattern.EndsWith("*", StringComparison.Ordinal))
            {
                if (lookupInfo.TypePattern.Length == 1)
                {
                    // TypePattern is "*".
                    return true;
                }

                var lookupTypeName = lookupInfo.TypePattern.Substring(0, lookupInfo.TypePattern.Length - 1);

                return descriptor.TypeName.StartsWith(lookupTypeName, StringComparison.Ordinal);
            }

            return string.Equals(descriptor.TypeName, lookupInfo.TypePattern, StringComparison.Ordinal);
        }

        private static int GetErrorLength(string directiveText)
        {
            var nonNullLength = directiveText == null ? 1 : directiveText.Length;
            var normalizeEmptyStringLength = Math.Max(nonNullLength, 1);

            return normalizeEmptyStringLength;
        }

        private IReadOnlyList<RazorError> CombineErrors(IReadOnlyList<RazorError> errors1, IReadOnlyList<RazorError> errors2)
        {
            var combinedErrors = new List<RazorError>(errors1.Count + errors2.Count);
            combinedErrors.AddRange(errors1);
            combinedErrors.AddRange(errors2);

            return combinedErrors;
        }

        internal class ParsedDirective
        {
            public string AssemblyName { get; set; }

            public string TypePattern { get; set; }

            public SourceLocation AssemblyNameLocation { get; set; }
        }

        private class DirectiveVisitor : ParserVisitor
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
