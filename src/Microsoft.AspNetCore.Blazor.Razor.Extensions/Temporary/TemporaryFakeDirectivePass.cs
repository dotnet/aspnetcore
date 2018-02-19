// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    // Until we're able to add real directives, this implements a temporary mechanism whereby we
    // search for lines of the form "@({regex})" (including in the imports sources) and do something
    // with the regex matches. Also we remove the corresponding tokens from the intermediate
    // representation to stop them from interfering with the compiled output on its own.
    internal abstract class TemporaryFakeDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
    {
        private readonly Regex _sourceLineRegex;
        private readonly Regex _tokenRegex;

        protected TemporaryFakeDirectivePass(string syntaxRegexPattern)
        {
            _sourceLineRegex = new Regex($@"^\s*@\({syntaxRegexPattern}\)\s*$");
            _tokenRegex = new Regex($@"^{syntaxRegexPattern}$");
        }

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            // First, remove any matching lines from the intermediate representation
            // in the primary document. Don't need to remove them from imports as they
            // have no effect there anyway.
            var methodNode = documentNode.FindPrimaryMethod();
            var methodNodeChildren = methodNode.Children.ToList();
            foreach (var node in methodNodeChildren)
            {
                if (IsMatchingNode(node))
                {
                    methodNode.Children.Remove(node);
                }
            }

            // Now find the matching lines in the source code (including imports)
            // Need to do this on source, because the imports aren't in the intermediate representation
            var linesVisitor = new RegexSourceLinesVisitor(_sourceLineRegex);
            linesVisitor.Visit(codeDocument);
            if (linesVisitor.MatchedContent.Any())
            {
                HandleMatchedContent(codeDocument, linesVisitor.MatchedContent);
            }
        }

        protected abstract void HandleMatchedContent(RazorCodeDocument codeDocument, IEnumerable<string> matchedContent);

        private bool IsMatchingNode(IntermediateNode node)
            => node.Children.Count == 1
                && node.Children[0] is IntermediateToken intermediateToken
                && _tokenRegex.IsMatch(intermediateToken.Content);

        private class RegexSourceLinesVisitor : SourceLinesVisitor
        {
            private Regex _searchRegex;
            private readonly List<string> _matchedContent = new List<string>();

            public IEnumerable<string> MatchedContent => _matchedContent;

            public RegexSourceLinesVisitor(Regex searchRegex)
            {
                _searchRegex = searchRegex;
            }

            protected override void VisitLine(string line)
            {
                // Pick the most specific by looking for the final one in the sources
                var match = _searchRegex.Match(line);
                if (match.Success)
                {
                    _matchedContent.Add(match.Groups[1].Value);
                }
            }
        }
    }
}
