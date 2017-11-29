// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class RazorSyntaxFactsServiceExtensionsTest
    {
        [Fact]
        public void IsTagHelperSpan_ReturnsTrue()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree(
@"<div>
    <taghelper />
</div>");
            var location = new SourceSpan(9 + Environment.NewLine.Length, 13);
            var service = new DefaultRazorSyntaxFactsService();

            // Act
            var result = service.IsTagHelperSpan(syntaxTree, location);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsTagHelperSpan_ReturnsFalse()
        {
            // Arrange
            var syntaxTree = GetSyntaxTree(
@"<div>
    <taghelper></taghelper>
</div>");
            var location = new SourceSpan(0, 4);
            var service = new DefaultRazorSyntaxFactsService();

            // Act
            var result = service.IsTagHelperSpan(syntaxTree, location);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsTagHelperSpan_NullSyntaxTree_ReturnsFalse()
        {
            // Arrange
            var location = new SourceSpan(0, 4);
            var service = new DefaultRazorSyntaxFactsService();

            // Act
            var result = service.IsTagHelperSpan(null, location);

            // Assert
            Assert.False(result);
        }

        private static RazorSyntaxTree GetSyntaxTree(string source)
        {
            var taghelper = TagHelperDescriptorBuilder.Create("TestTagHelper", "TestAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("taghelper"))
                .TypeName("TestTagHelper")
                .Build();
            var engine = RazorEngine.CreateDesignTime(builder =>
            {
                builder.AddTagHelpers(taghelper);
            });

            var sourceDocument = RazorSourceDocument.Create(source, "test.cshtml");
            var addTagHelperImport = RazorSourceDocument.Create("@addTagHelper *, TestAssembly", "import.cshtml");
            var codeDocument = RazorCodeDocument.Create(sourceDocument, new[] { addTagHelperImport });

            engine.Process(codeDocument);

            return codeDocument.GetSyntaxTree();
        }
    }
}
