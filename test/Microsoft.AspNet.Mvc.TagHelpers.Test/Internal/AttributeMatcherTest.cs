// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Mvc.TagHelpers.Internal
{
    public class AttributeMatcherTest
    {
        [Fact]
        public void DetermineMode_FindsFullModeMatchWithSingleAttribute()
        {
            // Arrange
            var modeInfo = new[]
            {
                ModeAttributes.Create("mode0", new [] { "first-attr" })
            };
            var attributes = new TagHelperAttributeList
            {
                ["first-attr"] = "value",
                ["not-in-any-mode"] = "value"
            };
            var context = MakeTagHelperContext(attributes);

            // Act
            var modeMatch = AttributeMatcher.DetermineMode(context, modeInfo);

            // Assert
            Assert.Collection(modeMatch.FullMatches, match =>
            {
                Assert.Equal("mode0", match.Mode);
                Assert.Collection(match.PresentAttributes, attribute => Assert.Equal("first-attr", attribute));
            });
            Assert.Empty(modeMatch.PartialMatches);
            Assert.Empty(modeMatch.PartiallyMatchedAttributes);
        }

        [Fact]
        public void DetermineMode_FindsFullModeMatchWithMultipleAttributes()
        {
            // Arrange
            var modeInfo = new[]
            {
                ModeAttributes.Create("mode0", new [] { "first-attr", "second-attr" })
            };
            var attributes = new TagHelperAttributeList
            {
                ["first-attr"] = "value",
                ["second-attr"] = "value",
                ["not-in-any-mode"] = "value"
            };
            var context = MakeTagHelperContext(attributes);

            // Act
            var modeMatch = AttributeMatcher.DetermineMode(context, modeInfo);

            // Assert
            Assert.Collection(modeMatch.FullMatches, match =>
            {
                Assert.Equal("mode0", match.Mode);
                Assert.Collection(match.PresentAttributes,
                    attribute => Assert.Equal("first-attr", attribute),
                    attribute => Assert.Equal("second-attr", attribute)
                );
            });
            Assert.Empty(modeMatch.PartialMatches);
            Assert.Empty(modeMatch.PartiallyMatchedAttributes);
        }

        [Fact]
        public void DetermineMode_FindsFullAndPartialModeMatchWithMultipleAttribute()
        {
            // Arrange
            var modeInfo = new[]
            {
                ModeAttributes.Create("mode0", new [] { "second-attr" }),
                ModeAttributes.Create("mode1", new [] { "first-attr", "third-attr" }),
                ModeAttributes.Create("mode2", new [] { "first-attr", "second-attr", "third-attr" }),
                ModeAttributes.Create("mode3", new [] { "fourth-attr" })
            };
            var attributes = new TagHelperAttributeList
            {
                ["second-attr"] = "value",
                ["third-attr"] = "value",
                ["not-in-any-mode"] = "value"
            };
            var context = MakeTagHelperContext(attributes);

            // Act
            var modeMatch = AttributeMatcher.DetermineMode(context, modeInfo);

            // Assert
            Assert.Collection(modeMatch.FullMatches, match =>
            {
                Assert.Equal("mode0", match.Mode);
                Assert.Collection(match.PresentAttributes, attribute => Assert.Equal("second-attr", attribute));
            });
            Assert.Collection(modeMatch.PartialMatches,
                match =>
                {
                    Assert.Equal("mode1", match.Mode);
                    Assert.Collection(match.PresentAttributes, attribute => Assert.Equal("third-attr", attribute));
                    Assert.Collection(match.MissingAttributes, attribute => Assert.Equal("first-attr", attribute));
                },
                match =>
                {
                    Assert.Equal("mode2", match.Mode);
                    Assert.Collection(match.PresentAttributes,
                        attribute => Assert.Equal("second-attr", attribute),
                        attribute => Assert.Equal("third-attr", attribute)
                    );
                    Assert.Collection(match.MissingAttributes, attribute => Assert.Equal("first-attr", attribute));
                });
            Assert.Collection(modeMatch.PartiallyMatchedAttributes, attribute => Assert.Equal("third-attr", attribute));
        }

        private static TagHelperContext MakeTagHelperContext(
            TagHelperAttributeList attributes = null,
            string content = null)
        {
            attributes = attributes ?? new TagHelperAttributeList();

            return new TagHelperContext(
                attributes,
                items: new Dictionary<object, object>(),
                uniqueId: Guid.NewGuid().ToString("N"),
                getChildContentAsync: () =>
                {
                    var tagHelperContent = new DefaultTagHelperContent();
                    tagHelperContent.Append(content);
                    return Task.FromResult((TagHelperContent)tagHelperContent);
                });
        }
    }
}