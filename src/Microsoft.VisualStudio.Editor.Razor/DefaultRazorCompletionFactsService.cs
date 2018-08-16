// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.VisualStudio.Editor.Razor
{
    [System.Composition.Shared]
    [Export(typeof(RazorCompletionFactsService))]
    internal class DefaultRazorCompletionFactsService : RazorCompletionFactsService
    {
        private static readonly IEnumerable<DirectiveDescriptor> DefaultDirectives = new[]
        {
            CSharpCodeParser.AddTagHelperDirectiveDescriptor,
            CSharpCodeParser.RemoveTagHelperDirectiveDescriptor,
            CSharpCodeParser.TagHelperPrefixDirectiveDescriptor,
        };

        public override IReadOnlyList<RazorCompletionItem> GetCompletionItems(RazorSyntaxTree syntaxTree, SourceSpan location)
        {
            var completionItems = new List<RazorCompletionItem>();

            if (AtDirectiveCompletionPoint(syntaxTree, location))
            {
                var directiveCompletions = GetDirectiveCompletionItems(syntaxTree);
                completionItems.AddRange(directiveCompletions);
            }

            return completionItems;
        }

        // Internal for testing
        internal static List<RazorCompletionItem> GetDirectiveCompletionItems(RazorSyntaxTree syntaxTree)
        {
            var directives = syntaxTree.Options.Directives.Concat(DefaultDirectives);
            var completionItems = new List<RazorCompletionItem>();
            foreach (var directive in directives)
            {
                var completionDisplayText = directive.DisplayName ?? directive.Directive;
                var completionItem = new RazorCompletionItem(
                    completionDisplayText,
                    directive.Directive,
                    directive.Description,
                    RazorCompletionItemKind.Directive);
                completionItems.Add(completionItem);
            }

            return completionItems;
        }

        // Internal for testing
        internal static bool AtDirectiveCompletionPoint(RazorSyntaxTree syntaxTree, SourceSpan location)
        {
            if (syntaxTree == null)
            {
                return false;
            }

            var change = new SourceChange(location, string.Empty);
            var owner = syntaxTree.Root.LocateOwner(change);

            if (owner == null)
            {
                return false;
            }

            if (owner.ChunkGenerator is ExpressionChunkGenerator &&
                owner.Tokens.All(IsDirectiveCompletableToken) &&
                // Do not provide IntelliSense for explicit expressions. Explicit expressions will usually look like:
                // [@] [(] [DateTime.Now] [)]
                owner.Parent?.Children.Count > 1 &&
                owner.Parent.Children[1] == owner)
            {
                return true;
            }

            return false;
        }

        // Internal for testing
        internal static bool IsDirectiveCompletableToken(SyntaxToken token)
        {
            return token.Kind == SyntaxKind.Identifier ||
                // Marker symbol
                token.Kind == SyntaxKind.Unknown;
        }
    }
}
