// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class DiagnosticRazorIntegrationTest : RazorIntegrationTestBase
    {
        [Fact]
        public void TemporaryComponentSyntaxRejectsParametersExpressedAsPlainHtmlAttributes()
        {
            // This is a temporary syntax restriction. Currently you can write:
            //    <c:MyComponent MyParam=@("My value") />
            // ... but are *not* allowed to write:
            //    <c:MyComponent MyParam="My value" />
            // This is because until we get the improved taghelper-based tooling,
            // we're using AngleSharp to parse the plain HTML attributes, and it
            // suffers from limitations:
            //  * Loses the casing of attribute names (MyParam becomes myparam)
            //  * Doesn't recognize MyBool=true as an bool (becomes mybool="true"),
            //    plus equivalent for other primitives like enum values
            // So to avoid people getting runtime errors, we're currently imposing
            // the compile-time restriction that component params have to be given
            // as C# expressions, e.g., MyBool=@true and MyString=@("Hello")

            // Arrange/Act
            var result = CompileToCSharp(
                $"Line 1\n" +
                $"Some text <c:MyComponent MyParam=\"My value\" />");

            // Assert
            Assert.Collection(
                result.Diagnostics,
                item =>
                {
                    Assert.Equal("BL9980", item.Id);
                    Assert.Equal(
                        $"Wrong syntax for 'myparam' on 'c:MyComponent': As a temporary " +
                        $"limitation, component attributes must be expressed with C# syntax. For " +
                        $"example, SomeParam=@(\"Some value\") is allowed, but SomeParam=\"Some value\" " +
                        $"is not.", item.GetMessage());
                    Assert.Equal(1, item.Span.LineIndex);
                    Assert.Equal(10, item.Span.CharacterIndex);
                });
        }

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
