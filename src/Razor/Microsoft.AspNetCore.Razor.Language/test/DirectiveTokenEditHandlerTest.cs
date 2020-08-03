// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test
{
    public class DirectiveTokenEditHandlerTest
    {
        [Theory]
        [InlineData(0, 4, "")] // "Namespace"
        [InlineData(4, 0, "Other")] // "SomeOtherNamespace"
        [InlineData(0, 4, "Other")] // "OtherNamespace"
        public void CanAcceptChange_ProvisionallyAcceptsNonWhitespaceChanges(int index, int length, string newText)
        {
            // Arrange
            var directiveTokenHandler = new TestDirectiveTokenEditHandler();
            directiveTokenHandler.AcceptedCharacters = AcceptedCharactersInternal.NonWhitespace;

            var target = GetSyntaxNode(directiveTokenHandler, "SomeNamespace");
            
            var sourceChange = new SourceChange(index, length, newText);

            // Act
            var result = directiveTokenHandler.CanAcceptChange(target, sourceChange);

            // Assert
            Assert.Equal(PartialParseResultInternal.Accepted | PartialParseResultInternal.Provisional, result);
        }

        [Theory]
        [InlineData(4, 1, "")] // "SomeNamespace"
        [InlineData(9, 0, " ")] // "Some Name space"
        [InlineData(9, 5, " Space")] // "Some Name Space"
        public void CanAcceptChange_RejectsWhitespaceChanges(int index, int length, string newText)
        {
            // Arrange
            var directiveTokenHandler = new TestDirectiveTokenEditHandler();
            directiveTokenHandler.AcceptedCharacters = AcceptedCharactersInternal.NonWhitespace;

            var target = GetSyntaxNode(directiveTokenHandler, "Some Namespace");

            var sourceChange = new SourceChange(index, length, newText);

            // Act
            var result = directiveTokenHandler.CanAcceptChange(target, sourceChange);

            // Assert
            Assert.Equal(PartialParseResultInternal.Rejected, result);
        }

        private static CSharpStatementLiteralSyntax GetSyntaxNode(DirectiveTokenEditHandler editHandler, string content)
        {
            var builder = SyntaxListBuilder<SyntaxToken>.Create();
            var tokens = CSharpLanguageCharacteristics.Instance.TokenizeString(content).ToArray();
            foreach (var token in tokens)
            {
                builder.Add((SyntaxToken)token.CreateRed());
            }
            var node = SyntaxFactory.CSharpStatementLiteral(builder.ToList());

            var context = new SpanContext(SpanChunkGenerator.Null, editHandler);

            return node.WithSpanContext(context);
        }

        private class TestDirectiveTokenEditHandler : DirectiveTokenEditHandler
        {
            public TestDirectiveTokenEditHandler() : base(content => TestTokenizer(content))
            {
            }

            public new PartialParseResultInternal CanAcceptChange(SyntaxNode target, SourceChange change)
                => base.CanAcceptChange(target, change);

            internal static IEnumerable<Syntax.InternalSyntax.SyntaxToken> TestTokenizer(string str)
            {
                yield return Syntax.InternalSyntax.SyntaxFactory.Token(SyntaxKind.Marker, str);
            }
        }
    }
}
