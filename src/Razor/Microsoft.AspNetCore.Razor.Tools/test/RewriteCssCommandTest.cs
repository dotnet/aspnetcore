// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Tools
{
    public class RewriteCssCommandTest
    {
        [Fact]
        public void HandlesEmptyFile()
        {
            // Arrange/act
            var result = RewriteCssCommand.AddScopeToSelectors(string.Empty, "TestScope");

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void AddsScopeAfterSelector()
        {
            // Arrange/act
            var result = RewriteCssCommand.AddScopeToSelectors(@"
    .myclass { color: red; }
", "TestScope");

            // Assert
            Assert.Equal(@"
    .myclass[TestScope] { color: red; }
", result);
        }

        [Fact]
        public void HandlesMultipleSelectors()
        {
            // Arrange/act
            var result = RewriteCssCommand.AddScopeToSelectors(@"
    .first, .second { color: red; }
    .third { color: blue; }
", "TestScope");

            // Assert
            Assert.Equal(@"
    .first[TestScope], .second[TestScope] { color: red; }
    .third[TestScope] { color: blue; }
", result);
        }

        [Fact]
        public void HandlesComplexSelectors()
        {
            // Arrange/act
            var result = RewriteCssCommand.AddScopeToSelectors(@"
    .first div > li, body .second:not(.fancy)[attr~=whatever] { color: red; }
", "TestScope");

            // Assert
            Assert.Equal(@"
    .first div > li[TestScope], body .second:not(.fancy)[attr~=whatever][TestScope] { color: red; }
", result);
        }

        [Fact]
        public void HandlesSpacesAndCommentsWithinSelectors()
        {
            // Arrange/act
            var result = RewriteCssCommand.AddScopeToSelectors(@"
    .first /* space at end {} */ div , .myclass /* comment at end */ { color: red; }
", "TestScope");

            // Assert
            Assert.Equal(@"
    .first /* space at end {} */ div[TestScope] , .myclass[TestScope] /* comment at end */ { color: red; }
", result);
        }

        [Fact]
        public void HandlesAtBlocks()
        {
            // Arrange/act
            var result = RewriteCssCommand.AddScopeToSelectors(@"
    .myclass { color: red; }

    @media only screen and (max-width: 600px) {
        .another .thing {
            content: 'This should not be a selector: .fake-selector { color: red }'
        }
    }
", "TestScope");

            // Assert
            Assert.Equal(@"
    .myclass[TestScope] { color: red; }

    @media only screen and (max-width: 600px) {
        .another .thing[TestScope] {
            content: 'This should not be a selector: .fake-selector { color: red }'
        }
    }
", result);
        }
    }
}
