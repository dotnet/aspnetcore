// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class DiagnosticRazorIntegrationTest : RazorIntegrationTestBase
    {
        [Fact]
        public void RejectsEndTagWithNoStartTag()
        {
            // Arrange/Act
            var result = CompileToCSharp(
                "Line1\nLine2\nLine3</mytag>");

            // Assert
            Assert.Collection(result.Diagnostics,
                item =>
                {
                    Assert.Equal("BL9981", item.Id);
                    Assert.Equal("Unexpected closing tag 'mytag' with no matching start tag.", item.GetMessage());
                });
        }

        [Fact]
        public void RejectsEndTagWithDifferentNameToStartTag()
        {
            // Arrange/Act
            var result = CompileToCSharp(
                $"@{{\n" +
                $"   var abc = 123;\n" +
                $"}}\n" +
                $"<root>\n" +
                $"    <other />\n" +
                $"    text\n" +
                $"    <child>more text</root>\n" +
                $"</child>\n");

            // Assert
            Assert.Collection(result.Diagnostics,
                item =>
                {
                    Assert.Equal("BL9982", item.Id);
                    Assert.Equal("Mismatching closing tag. Found 'child' but expected 'root'.", item.GetMessage());
                    Assert.Equal(6, item.Span.LineIndex);
                    Assert.Equal(20, item.Span.CharacterIndex);
                });
        }
    }
}
