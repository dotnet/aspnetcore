// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.ProjectSystem
{
    public class GeneratedCodeContainerTest
    {
        [Fact]
        public void TryGetLinePositionSpan_SpanWithinSourceMapping_ReturnsTrue()
        {
            // Arrange
            var content = @"
@{
    var x = SomeClass.SomeProperty;
}
";
            var sourceText = SourceText.From(content);
            var codeDocument = GetCodeDocument(content);
            var generatedCode = codeDocument.GetCSharpDocument().GeneratedCode;

            var container = new GeneratedCodeContainer();
            container.SetOutput(sourceText, codeDocument);

            // TODO: Make writing these tests a little less manual.
            // Position of `SomeProperty` in the generated code.
            var symbol = "SomeProperty";
            var span = new TextSpan(generatedCode.IndexOf(symbol), symbol.Length);

            // Position of `SomeProperty` in the source code.
            var expectedLineSpan = new LinePositionSpan(new LinePosition(2, 22), new LinePosition(2, 34));

            // Act
            var result = container.TryGetLinePositionSpan(span, out var lineSpan);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedLineSpan, lineSpan);
        }

        [Fact]
        public void TryGetLinePositionSpan_SpanOutsideSourceMapping_ReturnsFalse()
        {
            // Arrange
            var content = @"
@{
    var x = SomeClass.SomeProperty;
}
";
            var sourceText = SourceText.From(content);
            var codeDocument = GetCodeDocument(content);
            var generatedCode = codeDocument.GetCSharpDocument().GeneratedCode;

            var container = new GeneratedCodeContainer();
            container.SetOutput(sourceText, codeDocument);

            // Position of `ExecuteAsync` in the generated code.
            var symbol = "ExecuteAsync";
            var span = new TextSpan(generatedCode.IndexOf(symbol), symbol.Length);

            // Act
            var result = container.TryGetLinePositionSpan(span, out var lineSpan);

            // Assert
            Assert.False(result);
        }

        private static RazorCodeDocument GetCodeDocument(string content)
        {
            var sourceProjectItem = new TestRazorProjectItem("test.cshtml")
            {
                Content = content,
            };

            var engine = RazorProjectEngine.Create();
            var codeDocument = engine.ProcessDesignTime(sourceProjectItem);
            return codeDocument;
        }
    }
}
