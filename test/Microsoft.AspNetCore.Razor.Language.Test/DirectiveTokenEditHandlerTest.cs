// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Legacy;
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
            var factory = new SpanFactory();
            var directiveTokenHandler = new TestDirectiveTokenEditHandler();
            var target = factory.Span(SpanKindInternal.Code, "SomeNamespace", markup: false)
                .With(directiveTokenHandler)
                .Accepts(AcceptedCharactersInternal.NonWhiteSpace);
            var sourceChange = new SourceChange(index, length, newText);

            // Act
            var result = directiveTokenHandler.CanAcceptChange(target, sourceChange);

            // Assert
            Assert.Equal(PartialParseResult.Accepted | PartialParseResult.Provisional, result);
        }

        [Theory]
        [InlineData(4, 1, "")] // "SomeNamespace"
        [InlineData(9, 0, " ")] // "Some Name space"
        [InlineData(9, 5, " Space")] // "Some Name Space"
        public void CanAcceptChange_RejectsWhitespaceChanges(int index, int length, string newText)
        {
            // Arrange
            var factory = new SpanFactory();
            var directiveTokenHandler = new TestDirectiveTokenEditHandler();
            var target = factory.Span(SpanKindInternal.Code, "Some Namespace", markup: false)
                .With(directiveTokenHandler)
                .Accepts(AcceptedCharactersInternal.NonWhiteSpace);
            var sourceChange = new SourceChange(index, length, newText);

            // Act
            var result = directiveTokenHandler.CanAcceptChange(target, sourceChange);

            // Assert
            Assert.Equal(PartialParseResult.Rejected, result);
        }

        private class TestDirectiveTokenEditHandler : DirectiveTokenEditHandler
        {
            public TestDirectiveTokenEditHandler() : base(content => SpanConstructor.TestTokenizer(content))
            {
            }

            public new PartialParseResult CanAcceptChange(Span target, SourceChange change)
                => base.CanAcceptChange(target, change);
        }
    }
}
