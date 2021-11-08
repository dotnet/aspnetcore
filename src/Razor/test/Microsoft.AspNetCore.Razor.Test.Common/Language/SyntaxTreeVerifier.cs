// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Razor.Language;

// Verifies recursively that a syntax tree has no gaps in terms of position/location.
internal class SyntaxTreeVerifier
{
    public static void Verify(RazorSyntaxTree syntaxTree, bool ensureFullFidelity = true)
    {
        var verifier = new Verifier(syntaxTree.Source);
        verifier.Visit(syntaxTree.Root);

        if (ensureFullFidelity)
        {
            var syntaxTreeString = syntaxTree.Root.ToFullString();
            var builder = new StringBuilder(syntaxTree.Source.Length);
            for (var i = 0; i < syntaxTree.Source.Length; i++)
            {
                builder.Append(syntaxTree.Source[i]);
            }
            var sourceString = builder.ToString();

            // Make sure the syntax tree contains all of the text in the document.
            Assert.Equal(sourceString, syntaxTreeString);

            // Ensure all source is locatable
            for (var i = 0; i < syntaxTree.Source.Length; i++)
            {
                var span = new SourceSpan(i, 0);
                var location = new SourceChange(span, string.Empty);
                var owner = syntaxTree.Root.LocateOwner(location);

                if (owner == null)
                {
                    var snippetStartIndex = Math.Max(0, i - 10);
                    var snippetStartLength = i - snippetStartIndex;
                    var snippetStart = new char[snippetStartLength];
                    syntaxTree.Source.CopyTo(snippetStartIndex, snippetStart, 0, snippetStartLength);

                    var snippetEndIndex = Math.Min(syntaxTree.Source.Length - 1, i + 10);
                    var snippetEndLength = snippetEndIndex - i;
                    var snippetEnd = new char[snippetEndLength];
                    syntaxTree.Source.CopyTo(i, snippetEnd, 0, snippetEndLength);

                    var snippet = new char[snippetStart.Length + snippetEnd.Length + 1];
                    snippetStart.CopyTo(snippet, 0);
                    snippet[snippetStart.Length] = '|';
                    snippetEnd.CopyTo(snippet, snippetStart.Length + 1);

                    var snippetString = new string(snippet);

                    throw new XunitException(
$@"Could not locate Syntax Node owner at position '{i}':
{snippetString}");
                }
            }
        }
    }

    private class Verifier : SyntaxWalker
    {
        private readonly RazorSourceDocument _source;
        private SourceLocation _currentLocation;

        public Verifier(RazorSourceDocument source)
        {
            _currentLocation = new SourceLocation(source.FilePath, 0, 0, 0);
            _source = source;
        }

        // public SourceLocationTracker SourceLocationTracker => _tracker;

        public override void VisitToken(SyntaxToken token)
        {
            if (token != null && !token.IsMissing && token.Kind != SyntaxKind.Marker)
            {
                var start = token.GetSourceLocation(_source);
                if (!start.Equals(_currentLocation))
                {
                    throw new InvalidOperationException($"Token starting at {start} should start at {_currentLocation} - {token} ");
                }

                _currentLocation = SourceLocationTracker.Advance(_currentLocation, token.Content);
            }

            base.VisitToken(token);
        }
    }
}
