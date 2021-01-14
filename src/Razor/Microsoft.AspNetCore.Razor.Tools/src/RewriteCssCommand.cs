// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;
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
        private const string DeepCombinatorText = "::deep";
        private readonly static TimeSpan _regexTimeout = TimeSpan.FromSeconds(1);
        private readonly static Regex _deepCombinatorRegex = new Regex($@"^{DeepCombinatorText}\s*", RegexOptions.None, _regexTimeout);
        private readonly static Regex _trailingCombinatorRegex = new Regex(@"\s+[\>\+\~]$", RegexOptions.None, _regexTimeout);

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
            var allDiagnostics = new ConcurrentQueue<RazorDiagnostic>();

            Parallel.For(0, Sources.Values.Count, i =>
            {
                var source = Sources.Values[i];
                var output = Outputs.Values[i];
                var cssScope = CssScopes.Values[i];

                using var inputSourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                var inputSourceText = SourceText.From(inputSourceStream);

                var rewrittenCss = AddScopeToSelectors(source, inputSourceText, cssScope, out var diagnostics);
                if (diagnostics.Any())
                {
                    foreach (var diagnostic in diagnostics)
                    {
                        allDiagnostics.Enqueue(diagnostic);
                    }
                }
                else
                {
                    File.WriteAllText(output, rewrittenCss);
                }
            });

            foreach (var diagnostic in allDiagnostics)
            {
                Error.WriteLine(diagnostic.ToString());
            }

            return Task.FromResult(allDiagnostics.Any() ? ExitCodeFailure : ExitCodeSuccess);
        }

        // Public for tests
        public static string AddScopeToSelectors(string filePath, string inputSource, string cssScope, out IEnumerable<RazorDiagnostic> diagnostics)
            => AddScopeToSelectors(filePath, SourceText.From(inputSource), cssScope, out diagnostics);

        private static string AddScopeToSelectors(string filePath, SourceText inputSourceText, string cssScope, out IEnumerable<RazorDiagnostic> diagnostics)
        {
            var cssParser = new DefaultParserFactory().CreateParser();
            var inputText = inputSourceText.ToString();
            var stylesheet = cssParser.Parse(inputText, insertComments: false);

            var resultBuilder = new StringBuilder();
            var previousInsertionPosition = 0;
            var foundDiagnostics = new List<RazorDiagnostic>();

            var ensureNoImportsVisitor = new EnsureNoImports(filePath, inputSourceText, stylesheet, foundDiagnostics);
            ensureNoImportsVisitor.Visit();

            var scopeInsertionPositionsVisitor = new FindScopeInsertionEdits(stylesheet);
            scopeInsertionPositionsVisitor.Visit();
            foreach (var edit in scopeInsertionPositionsVisitor.Edits)
            {
                resultBuilder.Append(inputText.Substring(previousInsertionPosition, edit.Position - previousInsertionPosition));
                previousInsertionPosition = edit.Position;

                switch (edit)
                {
                    case InsertSelectorScopeEdit _:
                        resultBuilder.AppendFormat(CultureInfo.InvariantCulture, "[{0}]", cssScope);
                        break;
                    case InsertKeyframesNameScopeEdit _:
                        resultBuilder.AppendFormat(CultureInfo.InvariantCulture, "-{0}", cssScope);
                        break;
                    case DeleteContentEdit deleteContentEdit:
                        previousInsertionPosition += deleteContentEdit.DeleteLength;
                        break;
                    default:
                        throw new NotImplementedException($"Unknown edit type: '{edit}'");
                }
            }

            resultBuilder.Append(inputText.Substring(previousInsertionPosition));

            diagnostics = foundDiagnostics;
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

        private class FindScopeInsertionEdits : Visitor
        {
            public List<CssEdit> Edits { get; } = new List<CssEdit>();

            private readonly HashSet<string> _keyframeIdentifiers;

            public FindScopeInsertionEdits(ComplexItem root) : base(root)
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

                // If there's a deep combinator among the sequence of simple selectors, we consider that to signal
                // the end of the set of simple selectors for us to look at, plus we strip it out
                var allSimpleSelectors = selector.Children.OfType<SimpleSelector>();
                var firstDeepCombinator = allSimpleSelectors.FirstOrDefault(s => _deepCombinatorRegex.IsMatch(s.Text));

                var lastSimpleSelector = allSimpleSelectors.TakeWhile(s => s != firstDeepCombinator).LastOrDefault();
                if (lastSimpleSelector != null)
                {
                    Edits.Add(new InsertSelectorScopeEdit { Position = FindPositionToInsertInSelector(lastSimpleSelector) });
                }
                else if (firstDeepCombinator != null)
                {
                    // For a leading deep combinator, we want to insert the scope attribute at the start
                    // Otherwise the result would be a CSS rule that isn't scoped at all
                    Edits.Add(new InsertSelectorScopeEdit { Position = firstDeepCombinator.Start });
                }

                // Also remove the deep combinator if we matched one
                if (firstDeepCombinator != null)
                {
                    Edits.Add(new DeleteContentEdit { Position = firstDeepCombinator.Start, DeleteLength = DeepCombinatorText.Length });
                }
            }

            private int FindPositionToInsertInSelector(SimpleSelector lastSimpleSelector)
            {
                var children = lastSimpleSelector.Children;
                for (var i  = 0; i < children.Count; i++)
                {
                    switch (children[i])
                    {
                        // Selectors like "a > ::deep b" get parsed as [[a][>]][::deep][b], and we want to
                        // insert right after the "a". So if we're processing a SimpleSelector like [[a][>]],
                        // consider the ">" to signal the "insert before" position.
                        case TokenItem t when IsTrailingCombinator(t.TokenType):

                        // Similarly selectors like "a::before" get parsed as [[a][::before]], and we want to
                        // insert right after the "a".  So if we're processing a SimpleSelector like [[a][::before]],
                        // consider the pseudoelement to signal the "insert before" position.
                        case PseudoElementSelector:
                        case PseudoElementFunctionSelector:
                        case PseudoClassSelector s when IsSingleColonPseudoElement(s):
                            // Insert after the previous token if there is one, otherwise before the whole thing
                            return i > 0 ? children[i - 1].AfterEnd : lastSimpleSelector.Start;
                    }
                }

                // Since we didn't find any children that signal the insert-before position,
                // insert after the whole thing
                return lastSimpleSelector.AfterEnd;
            }

            private static bool IsSingleColonPseudoElement(PseudoClassSelector selector)
            {
                // See https://developer.mozilla.org/en-US/docs/Web/CSS/Pseudo-elements
                // Normally, pseudoelements require a double-colon prefix. However the following "original set"
                // of pseudoelements also support single-colon prefixes for back-compatibility with older versions
                // of the W3C spec. Our CSS parser sees them as pseudoselectors rather than pseudoelements, so
                // we have to special-case them. The single-colon option doesn't exist for other more modern
                // pseudoelements.
                var selectorText = selector.Text;
                return string.Equals(selectorText, ":after", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(selectorText, ":before", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(selectorText, ":first-letter", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(selectorText, ":first-line", StringComparison.OrdinalIgnoreCase);
            }

            private static bool IsTrailingCombinator(CssTokenType tokenType)
            {
                switch (tokenType)
                {
                    case CssTokenType.Plus:
                    case CssTokenType.Tilde:
                    case CssTokenType.Greater:
                        return true;
                    default:
                        return false;
                }
            }

            protected override void VisitAtDirective(AtDirective item)
            {
                // Whenever we see "@keyframes something { ... }", we want to insert right after "something"
                if (TryFindKeyframesIdentifier(item, out var identifier))
                {
                    Edits.Add(new InsertKeyframesNameScopeEdit { Position = identifier.AfterEnd });
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
                            Edits.Add(new InsertKeyframesNameScopeEdit { Position = token.AfterEnd });
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

        private class EnsureNoImports : Visitor
        {
            private readonly string _filePath;
            private readonly SourceText _sourceText;
            private readonly List<RazorDiagnostic> _diagnostics;

            public EnsureNoImports(string filePath, SourceText sourceText, ComplexItem root, List<RazorDiagnostic> diagnostics) : base(root)
            {
                _filePath = filePath;
                _sourceText = sourceText;
                _diagnostics = diagnostics;
            }

            protected override void VisitAtDirective(AtDirective item)
            {
                if (item.Children.Count >= 2
                    && item.Children[0] is TokenItem firstChild
                    && firstChild.TokenType == CssTokenType.At
                    && item.Children[1] is TokenItem secondChild
                    && string.Equals(secondChild.Text, "import", StringComparison.OrdinalIgnoreCase))
                {
                    var linePosition = _sourceText.Lines.GetLinePosition(item.Start);
                    var sourceSpan = new SourceSpan(_filePath, item.Start, linePosition.Line, linePosition.Character, item.Length);
                    _diagnostics.Add(RazorDiagnosticFactory.CreateCssRewriting_ImportNotAllowed(sourceSpan));
                }

                base.VisitAtDirective(item);
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

        private abstract class CssEdit
        {
            public int Position { get; set; }
        }

        private class InsertSelectorScopeEdit : CssEdit
        {
        }

        private class InsertKeyframesNameScopeEdit : CssEdit
        {
        }

        private class DeleteContentEdit : CssEdit
        {
            public int DeleteLength { get; set; }
        }
    }
}
