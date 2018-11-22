// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Components.Build.Test
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

        // This used to be a sugar syntax for lambdas, but we don't support that anymore
        [Fact]
        public void OldCodeBlockAttributeSyntax_ReportsError()
        {
            // Arrange/Act
            var generated = CompileToCSharp(@"
<elem attr=@{ DidInvokeCode = true; } />
@functions {
    public bool DidInvokeCode { get; set; } = false;
}");

            // Assert
            var diagnostic = Assert.Single(generated.Diagnostics);
            Assert.Equal("BL9979", diagnostic.Id);
        }

        [Fact]
        public void RejectsScriptTag()
        {
            // Arrange/Act
            var result = CompileToCSharp(@"Hello
<div>
    <script src='anything'>
        something
    </script>
</div>
Goodbye");

            // Assert
            Assert.Collection(result.Diagnostics,
                item =>
                {
                    Assert.Equal("BL9992", item.Id);
                    Assert.Equal("Script tags should not be placed inside components because they cannot be updated dynamically. To fix this, move the script tag to the 'index.html' file or another static location. For more information see https://go.microsoft.com/fwlink/?linkid=872131", item.GetMessage());
                    Assert.Equal(2, item.Span.LineIndex);
                    Assert.Equal(4, item.Span.CharacterIndex);
                });
        }
    }
}
