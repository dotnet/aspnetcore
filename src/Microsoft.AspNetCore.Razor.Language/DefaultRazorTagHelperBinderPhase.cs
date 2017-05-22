// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorTagHelperBinderPhase : RazorEnginePhaseBase, IRazorTagHelperBinderPhase
    {
        private static HashSet<char> InvalidNonWhitespaceNameCharacters = new HashSet<char>(new[]
        {
            '@', '!', '<', '/', '?', '[', '>', ']', '=', '"', '\'', '*'
        });

        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var syntaxTree = codeDocument.GetSyntaxTree();
            ThrowForMissingDocumentDependency(syntaxTree);

            var feature = Engine.Features.OfType<ITagHelperFeature>().FirstOrDefault();
            if (feature == null)
            {
                // No feature, nothing to do.
                return;
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

            var errorList = new List<RazorDiagnostic>();
            var descriptors = feature.GetDescriptors();

            var directives = visitor.Directives;
            descriptors = ProcessDirectives(directives, descriptors);

            var tagHelperPrefix = ProcessTagHelperPrefix(directives, codeDocument);
            var root = syntaxTree.Root;

            var context = TagHelperDocumentContext.Create(tagHelperPrefix, descriptors);
            codeDocument.SetTagHelperContext(context);

            if (descriptors.Count == 0)
            {
                // No descriptors, no-op.
                return;
            }

            var errorSink = new ErrorSink();
            var rewriter = new TagHelperParseTreeRewriter(tagHelperPrefix, descriptors);
            root = rewriter.Rewrite(root, errorSink);

            // Temporary code while we're still using legacy diagnostics in the SyntaxTree.
            errorList.AddRange(errorSink.Errors.Select(error => RazorDiagnostic.Create(error)));

            errorList.AddRange(descriptors.SelectMany(d => d.GetAllDiagnostics()));

            var diagnostics = CombineErrors(syntaxTree.Diagnostics, errorList);

            var newSyntaxTree = RazorSyntaxTree.Create(root, syntaxTree.Source, diagnostics, syntaxTree.Options);
            codeDocument.SetSyntaxTree(newSyntaxTree);
        }

        // Internal for testing
        internal string ProcessTagHelperPrefix(List<TagHelperDirectiveDescriptor> directives, RazorCodeDocument codeDocument)
        {
            // We only support a single prefix directive.
            TagHelperDirectiveDescriptor prefixDirective = null;
            for (var i = 0; i < directives.Count; i++)
            {
                if (directives[i].DirectiveType == TagHelperDirectiveType.TagHelperPrefix)
                {
                    // We only expect to see a single one of these per file, but that's enforced at another level.
                    prefixDirective = directives[i];
                }
            }

            var prefix = prefixDirective?.DirectiveText;
            if (prefix != null && !IsValidTagHelperPrefix(prefix, prefixDirective.Location, prefixDirective.Diagnostics))
            {
                prefix = null;
            }

            if (!string.IsNullOrEmpty(prefix))
            {
                return prefixDirective.DirectiveText;
            }

            return null;
        }

        internal IReadOnlyList<TagHelperDescriptor> ProcessDirectives(
            IReadOnlyList<TagHelperDirectiveDescriptor> directives,
            IReadOnlyList<TagHelperDescriptor> tagHelpers)
        {
            var matches = new HashSet<TagHelperDescriptor>(TagHelperDescriptorComparer.Default);

            for (var i = 0; i < directives.Count; i++)
            {
                var directive = directives[i];

                ParsedDirective parsed;
                switch (directive.DirectiveType)
                {
                    case TagHelperDirectiveType.AddTagHelper:

                        parsed = ParseAddOrRemoveDirective(directive);
                        if (parsed == null)
                        {
                            // Skip this one, it's an error
                            break;
                        }

                        if (!AssemblyContainsTagHelpers(parsed.AssemblyName, tagHelpers))
                        {
                            // No tag helpers in the assembly.
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

                        parsed = ParseAddOrRemoveDirective(directive);
                        if (parsed == null)
                        {
                            // Skip this one, it's an error
                            break;
                        }


                        if (!AssemblyContainsTagHelpers(parsed.AssemblyName, tagHelpers))
                        {
                            // No tag helpers in the assembly.
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
                }
            }

            return matches.ToArray();
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
        internal ParsedDirective ParseAddOrRemoveDirective(TagHelperDirectiveDescriptor directive)
        {
            var text = directive.DirectiveText;
            var lookupStrings = text?.Split(new[] { ',' });

            // Ensure that we have valid lookupStrings to work with. The valid format is "typeName, assemblyName"
            if (lookupStrings == null ||
                lookupStrings.Any(string.IsNullOrWhiteSpace) ||
                lookupStrings.Length != 2)
            {
                directive.Diagnostics.Add(
                    RazorDiagnostic.Create(
                        new RazorError(
                            Resources.FormatInvalidTagHelperLookupText(text),
                            directive.Location,
                            Math.Max(text.Length, 1))));

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
            List<RazorDiagnostic> diagnostics)
        {
            foreach (var character in prefix)
            {
                // Prefixes are correlated with tag names, tag names cannot have whitespace.
                if (char.IsWhiteSpace(character) ||  InvalidNonWhitespaceNameCharacters.Contains(character))
                {
                    diagnostics.Add(
                        RazorDiagnostic.Create(
                            new RazorError(
                                Resources.FormatInvalidTagHelperPrefixValue(SyntaxConstants.CSharp.TagHelperPrefixKeyword, character, prefix),
                                directiveLocation,
                                prefix.Length)));

                    return false;
                }
            }

            return true;
        }

        private static bool MatchesDirective(TagHelperDescriptor descriptor, ParsedDirective lookupInfo)
        {
            if (!string.Equals(descriptor.AssemblyName, lookupInfo.AssemblyName, StringComparison.Ordinal))
            {
                return false;
            }

            if (!descriptor.IsDefaultKind())
            {
                // We only understand default TagHelperDescriptors.
                return false;
            }

            var descriptorTypeName = descriptor.GetTypeName();

            if (lookupInfo.TypePattern.EndsWith("*", StringComparison.Ordinal))
            {
                if (lookupInfo.TypePattern.Length == 1)
                {
                    // TypePattern is "*".
                    return true;
                }

                var lookupTypeName = lookupInfo.TypePattern.Substring(0, lookupInfo.TypePattern.Length - 1);

                return descriptorTypeName.StartsWith(lookupTypeName, StringComparison.Ordinal);
            }

            return string.Equals(descriptorTypeName, lookupInfo.TypePattern, StringComparison.Ordinal);
        }

        private static int GetErrorLength(string directiveText)
        {
            var nonNullLength = directiveText == null ? 1 : directiveText.Length;
            var normalizeEmptyStringLength = Math.Max(nonNullLength, 1);

            return normalizeEmptyStringLength;
        }

        private IReadOnlyList<RazorDiagnostic> CombineErrors(IReadOnlyList<RazorDiagnostic> errors1, IReadOnlyList<RazorDiagnostic> errors2)
        {
            var combinedErrors = new List<RazorDiagnostic>(errors1.Count + errors2.Count);
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
                var directive = CreateDirective(span, chunkGenerator.LookupText, TagHelperDirectiveType.AddTagHelper, chunkGenerator.Diagnostics);
                Directives.Add(directive);
            }

            public override void VisitRemoveTagHelperSpan(RemoveTagHelperChunkGenerator chunkGenerator, Span span)
            {
                var directive = CreateDirective(span, chunkGenerator.LookupText, TagHelperDirectiveType.RemoveTagHelper, chunkGenerator.Diagnostics);
                Directives.Add(directive);
            }

            public override void VisitTagHelperPrefixDirectiveSpan(TagHelperPrefixDirectiveChunkGenerator chunkGenerator, Span span)
            {
                var directive = CreateDirective(span, chunkGenerator.Prefix, TagHelperDirectiveType.TagHelperPrefix, chunkGenerator.Diagnostics);
                Directives.Add(directive);
            }

            private TagHelperDirectiveDescriptor CreateDirective(
                Span span,
                string directiveText,
                TagHelperDirectiveType directiveType,
                List<RazorDiagnostic> diagnostics)
            {
                directiveText = directiveText.Trim();
                if (directiveText.Length >= 2 &&
                    directiveText.StartsWith("\"", StringComparison.Ordinal) &&
                    directiveText.EndsWith("\"", StringComparison.Ordinal))
                {
                    directiveText = directiveText.Substring(1, directiveText.Length - 2);
                }

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
                    Diagnostics = diagnostics,
                };

                return directiveDescriptor;
            }
        }
    }
}
