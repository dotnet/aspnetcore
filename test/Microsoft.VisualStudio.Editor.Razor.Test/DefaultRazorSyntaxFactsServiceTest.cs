// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultRazorSyntaxFactsServiceTest
    {
        [Fact]
        public void GetClassifiedSpans_ReturnsExpectedSpans()
        {
            // Arrange
            var expectedSpans = new[]
            {
                new ClassifiedSpan(new SourceSpan("test.cshtml", 0, 0, 0, 5), new SourceSpan("test.cshtml", 0, 0, 0, 5), SpanKind.Markup, BlockKind.Tag, AcceptedCharacters.Any),
                new ClassifiedSpan(new SourceSpan("test.cshtml", 5, 0, 5, 6), new SourceSpan("test.cshtml", 0, 0, 0, 42), SpanKind.Markup, BlockKind.Markup, AcceptedCharacters.Any),
                new ClassifiedSpan(new SourceSpan("test.cshtml", 34, 1, 27, 2), new SourceSpan("test.cshtml", 0, 0, 0, 42), SpanKind.Markup, BlockKind.Markup, AcceptedCharacters.Any),
                new ClassifiedSpan(new SourceSpan("test.cshtml", 36, 2, 0, 6), new SourceSpan("test.cshtml", 36, 2, 0, 6), SpanKind.Markup, BlockKind.Tag, AcceptedCharacters.Any),
            };
            var codeDocument = GetCodeDocument(
@"<div>
    <taghelper></taghelper>
</div>");
            var syntaxTree = codeDocument.GetSyntaxTree();
            var service = new DefaultRazorSyntaxFactsService();

            // Act
            var spans = service.GetClassifiedSpans(syntaxTree);

            // Assert
            Assert.Equal(expectedSpans, spans);
        }

        [Fact]
        public void GetClassifiedSpans_ReturnsAttributeSpansInDocumentOrder()
        {
            // Arrange
            var expectedSpans = new[]
            {
                new ClassifiedSpan(new SourceSpan("test.cshtml", 14, 0, 14, 1), new SourceSpan("test.cshtml", 0, 0, 0, 49), SpanKind.Code, BlockKind.Tag, AcceptedCharacters.AnyExceptNewline),
                new ClassifiedSpan(new SourceSpan("test.cshtml", 23, 0, 23, 2), new SourceSpan("test.cshtml", 0, 0, 0, 49), SpanKind.Markup, BlockKind.Tag, AcceptedCharacters.Any),
                new ClassifiedSpan(new SourceSpan("test.cshtml", 32, 0, 32, 4), new SourceSpan("test.cshtml", 0, 0, 0, 49), SpanKind.Code, BlockKind.Tag, AcceptedCharacters.AnyExceptNewline),
            };
            var codeDocument = GetCodeDocument(
@"<taghelper id=1 class=""th"" show=true></taghelper>");
            var syntaxTree = codeDocument.GetSyntaxTree();
            var service = new DefaultRazorSyntaxFactsService();

            // Act
            var spans = service.GetClassifiedSpans(syntaxTree);

            // Assert
            Assert.Equal(expectedSpans, spans);
        }

        [Fact]
        public void GetTagHelperSpans_ReturnsExpectedSpans()
        {
            // Arrange
            var codeDocument = GetCodeDocument(
@"<div>
    <taghelper></taghelper>
</div>");
            var tagHelperContext = codeDocument.GetTagHelperContext();
            var expectedSourceSpan = new SourceSpan("test.cshtml", 11, 1, 4, 23);
            var syntaxTree = codeDocument.GetSyntaxTree();
            var service = new DefaultRazorSyntaxFactsService();

            // Act
            var spans = service.GetTagHelperSpans(syntaxTree);

            // Assert
            var actualSpan = Assert.Single(spans);
            Assert.Equal(expectedSourceSpan, actualSpan.Span);
            Assert.Equal(tagHelperContext.TagHelpers, actualSpan.TagHelpers);
            Assert.Equal(tagHelperContext.Prefix, actualSpan.Binding.TagHelperPrefix);
            Assert.Equal("div", actualSpan.Binding.ParentTagName);
        }

        private static RazorCodeDocument GetCodeDocument(string source)
        {
            var taghelper = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly")
                .BoundAttributeDescriptor(attr => attr.Name("show").TypeName("System.Boolean"))
                .BoundAttributeDescriptor(attr => attr.Name("id").TypeName("System.Int32"))
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("taghelper"))
                .TypeName("TestTagHelper")
                .Build();
            var engine = RazorProjectEngine.Create();

            var sourceDocument = TestRazorSourceDocument.Create(source, normalizeNewLines: true);
            var importDocument = TestRazorSourceDocument.Create("@addTagHelper *, TestAssembly", filePath: "import.cshtml", relativePath: "import.cshtml");

            var codeDocument = engine.ProcessDesignTime(sourceDocument, importSources: new []{ importDocument }, new []{ taghelper });

            return codeDocument;
        }
    }
}
