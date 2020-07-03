// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Css.Parser.Parser;
using Microsoft.Css.Parser.Tokens;
using Microsoft.Css.Parser.TreeItems;
using Microsoft.Css.Parser.TreeItems.AtDirectives;
using Microsoft.Css.Parser.TreeItems.Selectors;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class RewriteCssCommand : CommandBase
    {
        public RewriteCssCommand(Application parent)
            : base(parent, "rewritecss")
        {
            Sources = Option("-s", "Files to rewrite", CommandOptionType.MultipleValue);
            Outputs = Option("-o", "Output file paths", CommandOptionType.MultipleValue);
            CssScopes = Option("-c", "CSS scope identifiers", CommandOptionType.MultipleValue);
        }

        public CommandOption Sources { get; }

        public CommandOption Outputs { get; }

        public CommandOption CssScopes { get; }

        protected override bool ValidateArguments()
        {
            if (Sources.Values.Count != Outputs.Values.Count)
            {
                Error.WriteLine($"{Sources.Description} has {Sources.Values.Count}, but {Outputs.Description} has {Outputs.Values.Count} values.");
                return false;
            }

            if (Sources.Values.Count != CssScopes.Values.Count)
            {
                Error.WriteLine($"{Sources.Description} has {Sources.Values.Count}, but {CssScopes.Description} has {CssScopes.Values.Count} values.");
                return false;
            }

            return true;
        }

        protected override Task<int> ExecuteCoreAsync()
        {
            Parallel.For(0, Sources.Values.Count, i =>
            {
                var source = Sources.Values[i];
                var output = Outputs.Values[i];
                var cssScope = CssScopes.Values[i];

                var inputText = File.ReadAllText(source);
                var rewrittenCss = AddScopeToSelectors(inputText, cssScope);
                File.WriteAllText(output, rewrittenCss);
            });

            return Task.FromResult(ExitCodeSuccess);
        }

        // Public for tests
        public static string AddScopeToSelectors(string inputText, string cssScope)
        {
            var cssParser = new DefaultParserFactory().CreateParser();
            var stylesheet = cssParser.Parse(inputText, insertComments: false);

            var resultBuilder = new StringBuilder();
            var previousInsertionPosition = 0;

            var scopeInsertionPositionsVisitor = new FindScopeInsertionPositionsVisitor(stylesheet);
            scopeInsertionPositionsVisitor.Visit();
            foreach (var (currentInsertionPosition, insertionType) in scopeInsertionPositionsVisitor.InsertionPositions)
            {
                resultBuilder.Append(inputText.Substring(previousInsertionPosition, currentInsertionPosition - previousInsertionPosition));
                
                switch (insertionType)
                {
                    case ScopeInsertionType.Selector:
                        resultBuilder.AppendFormat("[{0}]", cssScope);
                        break;
                    case ScopeInsertionType.KeyframesName:
                        resultBuilder.AppendFormat("-{0}", cssScope);
                        break;
                    default:
                        throw new NotImplementedException($"Unknown insertion type: '{insertionType}'");
                }

                
                previousInsertionPosition = currentInsertionPosition;
            }

            resultBuilder.Append(inputText.Substring(previousInsertionPosition));

            return resultBuilder.ToString();
        }

        private static bool TryFindKeyframesIdentifier(AtDirective atDirective, out ParseItem identifier)
        {
            var keyword = atDirective.Keyword;
            if (string.Equals(keyword?.Text, "keyframes", StringComparison.OrdinalIgnoreCase))
            {
                var nextSiblingText = keyword.NextSibling?.Text;
                if (!string.IsNullOrEmpty(nextSiblingText))
                {
                    identifier = keyword.NextSibling;
                    return true;
                }
            }

            identifier = null;
            return false;
        }

        private enum ScopeInsertionType
        {
            Selector,
            KeyframesName,
        }

        private class FindScopeInsertionPositionsVisitor : Visitor
        {
            public List<(int, ScopeInsertionType)> InsertionPositions { get; } = new List<(int, ScopeInsertionType)>();

            private readonly HashSet<string> _keyframeIdentifiers;

            public FindScopeInsertionPositionsVisitor(ComplexItem root) : base(root)
            {
                // Before we start, we need to know the full set of keyframe names declared in this document
                var keyframesIdentifiersVisitor = new FindKeyframesIdentifiersVisitor(root);
                keyframesIdentifiersVisitor.Visit();
                _keyframeIdentifiers = keyframesIdentifiersVisitor.KeyframesIdentifiers
                    .Select(x => x.Text)
                    .ToHashSet(StringComparer.Ordinal); // Keyframe names are case-sensitive
            }

            protected override void VisitSelector(Selector selector)
            {
                // For a ruleset like ".first child, .second { ... }", we'll see two selectors:
                //   ".first child," containing two simple selectors: ".first" and "child"
                //   ".second", containing one simple selector: ".second"
                // Our goal is to insert immediately after the final simple selector within each selector
                var lastSimpleSelector = selector.Children.OfType<SimpleSelector>().LastOrDefault();
                if (lastSimpleSelector != null)
                {
                    InsertionPositions.Add((lastSimpleSelector.AfterEnd, ScopeInsertionType.Selector));
                }
            }

            protected override void VisitAtDirective(AtDirective item)
            {
                // Whenever we see "@keyframes something { ... }", we want to insert right after "something"
                if (TryFindKeyframesIdentifier(item, out var identifier))
                {
                    InsertionPositions.Add((identifier.AfterEnd, ScopeInsertionType.KeyframesName));
                }
                else
                {
                    VisitDefault(item);
                }
            }

            protected override void VisitDeclaration(Declaration item)
            {
                switch (item.PropertyNameText)
                {
                    case "animation":
                    case "animation-name":
                        // The first two tokens are <propertyname> and <colon> (otherwise we wouldn't be here).
                        // After that, any of the subsequent tokens might be the animation name.
                        // Unfortunately the rules for determining which token is the animation name are very
                        // complex - https://developer.mozilla.org/en-US/docs/Web/CSS/animation#Syntax
                        // Fortunately we only want to rewrite animation names that are explicitly declared in
                        // the same document (we don't want to add scopes to references to global keyframes)
                        // so it's sufficient just to match known animation names.
                        var animationNameTokens = item.Children.Skip(2).OfType<TokenItem>()
                            .Where(x => x.TokenType == CssTokenType.Identifier && _keyframeIdentifiers.Contains(x.Text));
                        foreach (var token in animationNameTokens)
                        {
                            InsertionPositions.Add((token.AfterEnd, ScopeInsertionType.KeyframesName));
                        }
                        break;
                    default:
                        // We don't need to do anything else with other declaration types
                        break;
                }
            }
        }

        private class FindKeyframesIdentifiersVisitor : Visitor
        {
            public FindKeyframesIdentifiersVisitor(ComplexItem root) : base(root)
            {
            }

            public List<ParseItem> KeyframesIdentifiers { get; } = new List<ParseItem>();

            protected override void VisitAtDirective(AtDirective item)
            {
                if (TryFindKeyframesIdentifier(item, out var identifier))
                {
                    KeyframesIdentifiers.Add(identifier);
                }
                else
                {
                    VisitDefault(item);
                }
            }
        }

        private class Visitor
        {
            private readonly ComplexItem _root;

            public Visitor(ComplexItem root)
            {
                _root = root ?? throw new ArgumentNullException(nameof(root));
            }

            public void Visit()
            {
                VisitDefault(_root);
            }

            protected virtual void VisitSelector(Selector item)
            {
                VisitDefault(item);
            }

            protected virtual void VisitAtDirective(AtDirective item)
            {
                VisitDefault(item);
            }

            protected virtual void VisitDeclaration(Declaration item)
            {
                VisitDefault(item);
            }

            protected virtual void VisitDefault(ParseItem item)
            {
                if (item is ComplexItem complexItem)
                {
                    VisitDescendants(complexItem);
                }
            }

            private void VisitDescendants(ComplexItem container)
            {
                foreach (var child in container.Children)
                {
                    switch (child)
                    {
                        case Selector selector:
                            VisitSelector(selector);
                            break;
                        case AtDirective atDirective:
                            VisitAtDirective(atDirective);
                            break;
                        case Declaration declaration:
                            VisitDeclaration(declaration);
                            break;
                        default:
                            VisitDefault(child);
                            break;
                    }
                }
            }
        }
    }
}
