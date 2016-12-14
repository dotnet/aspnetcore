// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.IntegrationTests
{
    public class TagHelpersIntegrationTest : IntegrationTestBase
    {
        [Fact]
        public void SimpleTagHelpers()
        {
            // Arrange
            var descriptors = new[]
            {
                new TagHelperDescriptor
                {
                    TagName = "input",
                    TypeName = "InputTagHelper"
                }
            };

            var engine = RazorEngine.Create(
                builder => builder.Features.Add(new TagHelperFeature(new TestTagHelperDescriptorResolver(descriptors))));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
        }

        [Fact]
        public void TagHelpersWithBoundAttributes()
        {
            // Arrange
            var descriptors = new[]
            {
                new TagHelperDescriptor
                {
                    TagName = "input",
                    TypeName = "InputTagHelper",
                    Attributes = new[] { new TagHelperAttributeDescriptor
                    {
                        Name = "bound",
                        PropertyName = "FooProp",
                        TypeName = "System.String"
                    } }
                }
            };

            var engine = RazorEngine.Create(
                builder => builder.Features.Add(new TagHelperFeature(new TestTagHelperDescriptorResolver(descriptors))));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
        }

        [Fact]
        public void NestedTagHelpers()
        {
            // Arrange
            var descriptors = new[]
            {
                new TagHelperDescriptor
                {
                    TagName = "p",
                    TypeName = "PTagHelper"
                },
                new TagHelperDescriptor
                {
                    TagName = "form",
                    TypeName = "FormTagHelper"
                },
                new TagHelperDescriptor
                {
                    TagName = "input",
                    TypeName = "InputTagHelper",
                    Attributes = new[] { new TagHelperAttributeDescriptor
                    {
                        Name = "value",
                        PropertyName = "FooProp",
                        TypeName = "System.String"
                    } }
                }
            };

            var engine = RazorEngine.Create(
                builder => builder.Features.Add(new TagHelperFeature(new TestTagHelperDescriptorResolver(descriptors))));
            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
        }

        private class TestTagHelperDescriptorResolver : ITagHelperDescriptorResolver
        {
            private readonly IEnumerable<TagHelperDescriptor> _descriptors;

            public TestTagHelperDescriptorResolver(IEnumerable<TagHelperDescriptor> descriptors)
            {
                _descriptors = descriptors;
            }

            public IEnumerable<TagHelperDescriptor> Resolve(TagHelperDescriptorResolutionContext resolutionContext)
            {
                return _descriptors;
            }
        }
    }
}
