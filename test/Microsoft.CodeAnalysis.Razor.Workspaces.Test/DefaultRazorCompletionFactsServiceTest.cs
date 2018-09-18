// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Moq;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor
{
    public class DefaultRazorCompletionFactsServiceTest
    {
        private static readonly IReadOnlyList<DirectiveDescriptor> DefaultDirectives = new[]
        {
            CSharpCodeParser.AddTagHelperDirectiveDescriptor,
            CSharpCodeParser.RemoveTagHelperDirectiveDescriptor,
            CSharpCodeParser.TagHelperPrefixDirectiveDescriptor,
        };

        [Fact]
        public void GetDirectiveCompletionItems_ReturnsDefaultDirectivesAsCompletionItems()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("@addTag");

            // Act
            var completionItems = DefaultRazorCompletionFactsService.GetDirectiveCompletionItems(syntaxTree);

            // Assert
            Assert.Collection(
                completionItems,
                item => AssertRazorCompletionItem(DefaultDirectives[0], item),
                item => AssertRazorCompletionItem(DefaultDirectives[1], item),
                item => AssertRazorCompletionItem(DefaultDirectives[2], item));
        }

        [Fact]
        public void GetDirectiveCompletionItems_ReturnsCustomDirectivesAsCompletionItems()
        {
            // Arrange
            var customDirective = DirectiveDescriptor.CreateSingleLineDirective("custom", builder =>
            {
                builder.Description = "My Custom Directive.";
            });
            var syntaxTree = CreateSyntaxTree("@addTag", customDirective);

            // Act
            var completionItems = DefaultRazorCompletionFactsService.GetDirectiveCompletionItems(syntaxTree);

            // Assert
            Assert.Collection(
                completionItems,
                item => AssertRazorCompletionItem(customDirective, item),
                item => AssertRazorCompletionItem(DefaultDirectives[0], item),
                item => AssertRazorCompletionItem(DefaultDirectives[1], item),
                item => AssertRazorCompletionItem(DefaultDirectives[2], item));
        }

        [Fact]
        public void GetDirectiveCompletionItems_UsesDisplayNamesWhenNotNull()
        {
            // Arrange
            var customDirective = DirectiveDescriptor.CreateSingleLineDirective("custom", builder =>
            {
                builder.DisplayName = "different";
                builder.Description = "My Custom Directive.";
            });
            var syntaxTree = CreateSyntaxTree("@addTag", customDirective);

            // Act
            var completionItems = DefaultRazorCompletionFactsService.GetDirectiveCompletionItems(syntaxTree);

            // Assert
            Assert.Collection(
                completionItems,
                item => AssertRazorCompletionItem("different", customDirective, item),
                item => AssertRazorCompletionItem(DefaultDirectives[0], item),
                item => AssertRazorCompletionItem(DefaultDirectives[1], item),
                item => AssertRazorCompletionItem(DefaultDirectives[2], item));
        }

        [Fact]
        public void AtDirectiveCompletionPoint_ReturnsFalseIfSyntaxTreeNull()
        {
            // Act
            var result = DefaultRazorCompletionFactsService.AtDirectiveCompletionPoint(syntaxTree: null, location: new SourceSpan(0, 0));

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AtDirectiveCompletionPoint_ReturnsFalseIfNoOwner()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("@");
            var location = new SourceSpan(2, 0);

            // Act
            var result = DefaultRazorCompletionFactsService.AtDirectiveCompletionPoint(syntaxTree, location);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AtDirectiveCompletionPoint_ReturnsFalseWhenOwnerIsNotExpression()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("@{");
            var location = new SourceSpan(2, 0);

            // Act
            var result = DefaultRazorCompletionFactsService.AtDirectiveCompletionPoint(syntaxTree, location);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AtDirectiveCompletionPoint_ReturnsFalseWhenOwnerIsComplexExpression()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("@DateTime.Now");
            var location = new SourceSpan(2, 0);

            // Act
            var result = DefaultRazorCompletionFactsService.AtDirectiveCompletionPoint(syntaxTree, location);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AtDirectiveCompletionPoint_ReturnsFalseWhenOwnerIsExplicitExpression()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("@(something)");
            var location = new SourceSpan(4, 0);

            // Act
            var result = DefaultRazorCompletionFactsService.AtDirectiveCompletionPoint(syntaxTree, location);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void AtDirectiveCompletionPoint_ReturnsTrueForSimpleImplicitExpressions()
        {
            // Arrange
            var syntaxTree = CreateSyntaxTree("@mod");
            var location = new SourceSpan(2, 0);

            // Act
            var result = DefaultRazorCompletionFactsService.AtDirectiveCompletionPoint(syntaxTree, location);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsDirectiveCompletableToken_ReturnsTrueForCSharpIdentifiers()
        {
            // Arrange
            var csharpToken = SyntaxFactory.Token(SyntaxKind.Identifier, "model");

            // Act
            var result = DefaultRazorCompletionFactsService.IsDirectiveCompletableToken(csharpToken);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsDirectiveCompletableToken_ReturnsTrueForCSharpMarkerTokens()
        {
            // Arrange
            var csharpToken = SyntaxFactory.Token(SyntaxKind.Unknown, string.Empty);

            // Act
            var result = DefaultRazorCompletionFactsService.IsDirectiveCompletableToken(csharpToken);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsDirectiveCompletableToken_ReturnsFalseForNonCSharpTokens()
        {
            // Arrange
            var token = SyntaxFactory.Token(SyntaxKind.Text, string.Empty);

            // Act
            var result = DefaultRazorCompletionFactsService.IsDirectiveCompletableToken(token);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsDirectiveCompletableToken_ReturnsFalseForInvalidCSharpTokens()
        {
            // Arrange
            var csharpToken = SyntaxFactory.Token(SyntaxKind.Tilde, "~");

            // Act
            var result = DefaultRazorCompletionFactsService.IsDirectiveCompletableToken(csharpToken);

            // Assert
            Assert.False(result);
        }

        private static void AssertRazorCompletionItem(string completionDisplayText, DirectiveDescriptor directive, RazorCompletionItem item)
        {
            Assert.Equal(item.DisplayText, completionDisplayText);
            Assert.Equal(item.InsertText, directive.Directive);
            Assert.Equal(directive.Description, item.Description);
        }

        private static void AssertRazorCompletionItem(DirectiveDescriptor directive, RazorCompletionItem item) =>
            AssertRazorCompletionItem(directive.Directive, directive, item);

        private static RazorSyntaxTree CreateSyntaxTree(string text, params DirectiveDescriptor[] directives)
        {
            var sourceDocument = TestRazorSourceDocument.Create(text);
            var options = RazorParserOptions.Create(builder =>
            {
                foreach (var directive in directives)
                {
                    builder.Directives.Add(directive);
                }
            });
            var syntaxTree = RazorSyntaxTree.Parse(sourceDocument, options);
            return syntaxTree;
        }
    }
}
