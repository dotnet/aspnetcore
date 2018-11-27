// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class MarkupElementRewriterTest : CsHtmlMarkupParserTestBase
    {
        [Fact]
        public void Rewrites_ValidTags()
        {
            // Arrange
            var content = @"
<div>Foo</div>
<p>Bar</p>
";

            // Act & Assert
            RewriterTest(content);
        }

        [Fact]
        public void Rewrites_ValidNestedTags()
        {
            // Arrange
            var content = @"
<div>
    Foo
    <p>Bar</p>
    Baz
</div>";

            // Act & Assert
            RewriterTest(content);
        }

        [Fact]
        public void Rewrites_ValidNestedTagsMixedWithCode()
        {
            // Arrange
            var content = @"
<div>
    Foo
    <p>@Bar</p>
    @{ var x = Bar; }
</div>
";

            // Act & Assert
            RewriterTest(content);
        }

        [Fact]
        public void Rewrites_EndTagsWithMissingStartTags()
        {
            // Arrange
            var content = @"
Foo</div>
";

            // Act & Assert
            RewriterTest(content);
        }

        [Fact]
        public void Rewrites_StartTagsWithMissingEndTags()
        {
            // Arrange
            var content = @"
<div>
    Foo
    <p>
        Bar
        </strong>
";

            // Act & Assert
            RewriterTest(content);
        }

        [Fact]
        public void Rewrites_SelfClosingTags()
        {
            // Arrange
            var content = @"
<br/>Foo<custom />
";

            // Act & Assert
            RewriterTest(content);
        }

        [Fact]
        public void Rewrites_MalformedTags_RecoversSuccessfully()
        {
            // Arrange
            var content = @"
<div>content</span>footer</div>
";

            // Act & Assert
            RewriterTest(content);
        }

        [Fact]
        public void Rewrites_MisplacedEndTags_RecoversSuccessfully()
        {
            // Arrange
            var content = @"
<div>content<span>footer</div></span>
";

            // Act & Assert
            RewriterTest(content);
        }

        [Fact]
        public void Rewrites_DoesNotSpecialCase_VoidTags()
        {
            // Arrange
            var content = @"
<input>Foo</input>
";

            // Act & Assert
            RewriterTest(content);
        }

        [Fact]
        public void Rewrites_IncompleteTags()
        {
            // Arrange
            var content = @"
<<div>>Foo</</div><   >
";

            // Act & Assert
            RewriterTest(content);
        }

        private void RewriterTest(string input)
        {
            var syntaxTree = ParseDocument(input, designTime: false);
            var rewritten = MarkupElementRewriter.AddMarkupElements(syntaxTree);
            BaselineTest(rewritten);

            var unrewritten = MarkupElementRewriter.RemoveMarkupElements(rewritten);
            Assert.Equal(syntaxTree.Root.SerializedValue, unrewritten.Root.SerializedValue);
        }
    }
}