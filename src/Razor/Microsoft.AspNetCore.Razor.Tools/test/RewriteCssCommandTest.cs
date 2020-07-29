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
        public void RespectsDeepCombinator()
        {
            // Arrange/act
            var result = RewriteCssCommand.AddScopeToSelectors(@"
    .first ::deep .second { color: red; }
    a ::deep b, c ::deep d { color: blue; }
", "TestScope");

            // Assert
            Assert.Equal(@"
    .first[TestScope]  .second { color: red; }
    a[TestScope]  b, c[TestScope]  d { color: blue; }
", result);
        }

        [Fact]
        public void RespectsDeepCombinatorWithDirectDescendant()
        {
            // Arrange/act
            var result = RewriteCssCommand.AddScopeToSelectors(@"
    a  >  ::deep b { color: red; }
    c ::deep  >  d { color: blue; }
", "TestScope");

            // Assert
            Assert.Equal(@"
    a[TestScope]  >   b { color: red; }
    c[TestScope]   >  d { color: blue; }
", result);
        }

        [Fact]
        public void RespectsDeepCombinatorWithAdjacentSibling()
        {
            // Arrange/act
            var result = RewriteCssCommand.AddScopeToSelectors(@"
    a + ::deep b { color: red; }
    c ::deep + d { color: blue; }
", "TestScope");

            // Assert
            Assert.Equal(@"
    a[TestScope] +  b { color: red; }
    c[TestScope]  + d { color: blue; }
", result);
        }

        [Fact]
        public void RespectsDeepCombinatorWithGeneralSibling()
        {
            // Arrange/act
            var result = RewriteCssCommand.AddScopeToSelectors(@"
    a ~ ::deep b { color: red; }
    c ::deep ~ d { color: blue; }
", "TestScope");

            // Assert
            Assert.Equal(@"
    a[TestScope] ~  b { color: red; }
    c[TestScope]  ~ d { color: blue; }
", result);
        }

        [Fact]
        public void IgnoresMultipleDeepCombinators()
        {
            // Arrange/act
            var result = RewriteCssCommand.AddScopeToSelectors(@"
    .first ::deep .second ::deep .third { color:red; }
", "TestScope");

            // Assert
            Assert.Equal(@"
    .first[TestScope]  .second ::deep .third { color:red; }
", result);
        }

        [Fact]
        public void RespectsDeepCombinatorWithSpacesAndComments()
        {
            // Arrange/act
            var result = RewriteCssCommand.AddScopeToSelectors(@"
    .a .b /* comment ::deep 1 */  ::deep  /* comment ::deep 2 */  .c /* ::deep */ .d { color: red; }
    ::deep * { color: blue; } /* Leading deep combinator */
    another ::deep { color: green }  /* Trailing deep combinator */
", "TestScope");

            // Assert
            Assert.Equal(@"
    .a .b[TestScope] /* comment ::deep 1 */    /* comment ::deep 2 */  .c /* ::deep */ .d { color: red; }
    [TestScope] * { color: blue; } /* Leading deep combinator */
    another[TestScope]  { color: green }  /* Trailing deep combinator */
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

        [Fact]
        public void AddsScopeToKeyframeNames()
        {
            // Arrange/act
            var result = RewriteCssCommand.AddScopeToSelectors(@"
    @keyframes my-animation { /* whatever */ }
", "TestScope");

            // Assert
            Assert.Equal(@"
    @keyframes my-animation-TestScope { /* whatever */ }
", result);
        }

        [Fact]
        public void RewritesAnimationNamesWhenMatchingKnownKeyframes()
        {
            // Arrange/act
            var result = RewriteCssCommand.AddScopeToSelectors(@"
    .myclass {
        color: red;
        animation: /* ignore comment */ my-animation 1s infinite;
    }

    .another-thing { animation-name: different-animation; }

    h1 { animation: unknown-animation; } /* Should not be scoped */

    @keyframes my-animation { /* whatever */ }
    @keyframes different-animation { /* whatever */ }
    @keyframes unused-animation { /* whatever */ }
", "TestScope");

            // Assert
            Assert.Equal(@"
    .myclass[TestScope] {
        color: red;
        animation: /* ignore comment */ my-animation-TestScope 1s infinite;
    }

    .another-thing[TestScope] { animation-name: different-animation-TestScope; }

    h1[TestScope] { animation: unknown-animation; } /* Should not be scoped */

    @keyframes my-animation-TestScope { /* whatever */ }
    @keyframes different-animation-TestScope { /* whatever */ }
    @keyframes unused-animation-TestScope { /* whatever */ }
", result);
        }

        [Fact]
        public void RewritesMultipleAnimationNames()
        {
            // Arrange/act
            var result = RewriteCssCommand.AddScopeToSelectors(@"
    .myclass1 { animation-name: my-animation , different-animation }
    .myclass2 { animation: 4s linear 0s alternate my-animation infinite, different-animation 0s }
    @keyframes my-animation { }
    @keyframes different-animation { }
", "TestScope");

            // Assert
            Assert.Equal(@"
    .myclass1[TestScope] { animation-name: my-animation-TestScope , different-animation-TestScope }
    .myclass2[TestScope] { animation: 4s linear 0s alternate my-animation-TestScope infinite, different-animation-TestScope 0s }
    @keyframes my-animation-TestScope { }
    @keyframes different-animation-TestScope { }
", result);
        }
    }
}
