// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor
{
    public class DefaultTagHelperFactsServiceTest
    {
        // Purposefully not thoroughly testing DefaultTagHelperFactsService.GetTagHelperBinding because it's a pass through
        // into TagHelperDescriptorProvider.GetTagHelperBinding.

        [Fact]
        public void GetTagHelperBinding_DoesNotAllowOptOutCharacterPrefix()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                    .Build()
            };
            var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);
            var service = new DefaultTagHelperFactsService();

            // Act
            var binding = service.GetTagHelperBinding(documentContext, "!a", Enumerable.Empty<KeyValuePair<string, string>>(), parentTag: null);

            // Assert
            Assert.Null(binding);
        }

        [Fact]
        public void GetTagHelperBinding_WorksAsExpected()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                            .RequireTagName("a")
                            .RequireAttributeDescriptor(attribute => attribute.Name("asp-for")))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                            .Name("asp-for")
                            .TypeName(typeof(string).FullName)
                            .PropertyName("AspFor"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                            .Name("asp-route")
                            .TypeName(typeof(IDictionary<string, string>).Namespace + "IDictionary<string, string>")
                            .PropertyName("AspRoute")
                            .AsDictionaryAttribute("asp-route-", typeof(string).FullName))
                    .Build(),
                TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("input"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                            .Name("asp-for")
                            .TypeName(typeof(string).FullName)
                            .PropertyName("AspFor"))
                    .Build(),
            };
            var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);
            var service = new DefaultTagHelperFactsService();
            var attributes = new[]
            {
                new KeyValuePair<string, string>("asp-for", "Name")
            };

            // Act
            var binding = service.GetTagHelperBinding(documentContext, "a", attributes, parentTag: "p");

            // Assert
            var descriptor = Assert.Single(binding.Descriptors);
            Assert.Equal(documentDescriptors[0], descriptor, TagHelperDescriptorComparer.CaseSensitive);
            var boundRule = Assert.Single(binding.GetBoundRules(descriptor));
            Assert.Equal(documentDescriptors[0].TagMatchingRules.First(), boundRule, TagMatchingRuleDescriptorComparer.CaseSensitive);
        }

        [Fact]
        public void GetBoundTagHelperAttributes_MatchesPrefixedAttributeName()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("a"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                            .Name("asp-for")
                            .TypeName(typeof(string).FullName)
                            .PropertyName("AspFor"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                            .Name("asp-route")
                            .TypeName(typeof(IDictionary<string, string>).Namespace + "IDictionary<string, string>")
                            .PropertyName("AspRoute")
                            .AsDictionaryAttribute("asp-route-", typeof(string).FullName))
                    .Build()
            };
            var expectedAttributeDescriptors = new[]
            {
                documentDescriptors[0].BoundAttributes.Last()
            };
            var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);
            var service = new DefaultTagHelperFactsService();
            var binding = service.GetTagHelperBinding(documentContext, "a", Enumerable.Empty<KeyValuePair<string, string>>(), parentTag: null);

            // Act
            var descriptors = service.GetBoundTagHelperAttributes(documentContext, "asp-route-something", binding);

            // Assert
            Assert.Equal(expectedAttributeDescriptors, descriptors, BoundAttributeDescriptorComparer.CaseSensitive);
        }

        [Fact]
        public void GetBoundTagHelperAttributes_MatchesAttributeName()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("input"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                            .Name("asp-for")
                            .TypeName(typeof(string).FullName)
                            .PropertyName("AspFor"))
                    .BoundAttributeDescriptor(attribute =>
                        attribute
                            .Name("asp-extra")
                            .TypeName(typeof(string).FullName)
                            .PropertyName("AspExtra"))
                    .Build()
            };
            var expectedAttributeDescriptors = new[]
            {
                documentDescriptors[0].BoundAttributes.First()
            };
            var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);
            var service = new DefaultTagHelperFactsService();
            var binding = service.GetTagHelperBinding(documentContext, "input", Enumerable.Empty<KeyValuePair<string, string>>(), parentTag: null);

            // Act
            var descriptors = service.GetBoundTagHelperAttributes(documentContext, "asp-for", binding);

            // Assert
            Assert.Equal(expectedAttributeDescriptors, descriptors, BoundAttributeDescriptorComparer.CaseSensitive);
        }

        [Fact]
        public void GetTagHelpersGivenTag_DoesNotAllowOptOutCharacterPrefix()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                    .Build()
            };
            var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);
            var service = new DefaultTagHelperFactsService();

            // Act
            var descriptors = service.GetTagHelpersGivenTag(documentContext, "!strong", parentTag: null);

            // Assert
            Assert.Empty(descriptors);
        }

        [Fact]
        public void GetTagHelpersGivenTag_RequiresTagName()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
                    .Build()
            };
            var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);
            var service = new DefaultTagHelperFactsService();

            // Act
            var descriptors = service.GetTagHelpersGivenTag(documentContext, "strong", "p");

            // Assert
            Assert.Equal(documentDescriptors, descriptors, TagHelperDescriptorComparer.CaseSensitive);
        }

        [Fact]
        public void GetTagHelpersGivenTag_RestrictsTagHelpersBasedOnTagName()
        {
            // Arrange
            var expectedDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                    .TagMatchingRuleDescriptor(
                        rule => rule
                            .RequireTagName("a")
                            .RequireParentTag("div"))
                    .Build()
            };
            var documentDescriptors = new[]
            {
                expectedDescriptors[0],
                TagHelperDescriptorBuilder.Create("TestType2", "TestAssembly")
                    .TagMatchingRuleDescriptor(
                        rule => rule
                            .RequireTagName("strong")
                            .RequireParentTag("div"))
                    .Build()
            };
            var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);
            var service = new DefaultTagHelperFactsService();

            // Act
            var descriptors = service.GetTagHelpersGivenTag(documentContext, "a", "div");

            // Assert
            Assert.Equal(expectedDescriptors, descriptors, TagHelperDescriptorComparer.CaseSensitive);
        }

        [Fact]
        public void GetTagHelpersGivenTag_RestrictsTagHelpersBasedOnTagHelperPrefix()
        {
            // Arrange
            var expectedDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
                    .Build()
            };
            var documentDescriptors = new[]
            {
                expectedDescriptors[0],
                TagHelperDescriptorBuilder.Create("TestType2", "TestAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("thstrong"))
                    .Build()
            };
            var documentContext = TagHelperDocumentContext.Create("th", documentDescriptors);
            var service = new DefaultTagHelperFactsService();

            // Act
            var descriptors = service.GetTagHelpersGivenTag(documentContext, "thstrong", "div");

            // Assert
            Assert.Equal(expectedDescriptors, descriptors, TagHelperDescriptorComparer.CaseSensitive);
        }

        [Fact]
        public void GetTagHelpersGivenTag_RestrictsTagHelpersBasedOnParent()
        {
            // Arrange
            var expectedDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                    .TagMatchingRuleDescriptor(
                        rule => rule
                            .RequireTagName("strong")
                            .RequireParentTag("div"))
                    .Build()
            };
            var documentDescriptors = new[]
            {
                expectedDescriptors[0],
                TagHelperDescriptorBuilder.Create("TestType2", "TestAssembly")
                    .TagMatchingRuleDescriptor(
                        rule => rule
                            .RequireTagName("strong")
                            .RequireParentTag("p"))
                    .Build()
            };
            var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);
            var service = new DefaultTagHelperFactsService();

            // Act
            var descriptors = service.GetTagHelpersGivenTag(documentContext, "strong", "div");

            // Assert
            Assert.Equal(expectedDescriptors, descriptors, TagHelperDescriptorComparer.CaseSensitive);
        }

        [Fact]
        public void GetTagHelpersGivenParent_AllowsRootParentTag()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                    .Build()
            };
            var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);
            var service = new DefaultTagHelperFactsService();

            // Act
            var descriptors = service.GetTagHelpersGivenParent(documentContext, parentTag: null /* root */);

            // Assert
            Assert.Equal(documentDescriptors, descriptors, TagHelperDescriptorComparer.CaseSensitive);
        }

        [Fact]
        public void GetTagHelpersGivenParent_AllowsRootParentTagForParentRestrictedTagHelperDescriptors()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("DivTagHelper", "TestAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                    .Build(),
                TagHelperDescriptorBuilder.Create("PTagHelper", "TestAssembly")
                    .TagMatchingRuleDescriptor(rule => rule
                        .RequireTagName("p")
                        .RequireParentTag("body"))
                    .Build()
            };
            var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);
            var service = new DefaultTagHelperFactsService();

            // Act
            var descriptors = service.GetTagHelpersGivenParent(documentContext, parentTag: null /* root */);

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(documentDescriptors[0], descriptor, TagHelperDescriptorComparer.CaseSensitive);
        }

        [Fact]
        public void GetTagHelpersGivenParent_AllowsUnspecifiedParentTagHelpers()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                    .Build()
            };
            var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);
            var service = new DefaultTagHelperFactsService();

            // Act
            var descriptors = service.GetTagHelpersGivenParent(documentContext, "p");

            // Assert
            Assert.Equal(documentDescriptors, descriptors, TagHelperDescriptorComparer.CaseSensitive);
        }

        [Fact]
        public void GetTagHelpersGivenParent_RestrictsTagHelpersBasedOnParent()
        {
            // Arrange
            var expectedDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("TestType", "TestAssembly")
                    .TagMatchingRuleDescriptor(
                        rule => rule
                            .RequireTagName("p")
                            .RequireParentTag("div"))
                    .Build()
            };
            var documentDescriptors = new[]
            {
                expectedDescriptors[0],
                TagHelperDescriptorBuilder.Create("TestType2", "TestAssembly")
                    .TagMatchingRuleDescriptor(
                        rule => rule
                            .RequireTagName("strong")
                            .RequireParentTag("p"))
                    .Build()
            };
            var documentContext = TagHelperDocumentContext.Create(string.Empty, documentDescriptors);
            var service = new DefaultTagHelperFactsService();

            // Act
            var descriptors = service.GetTagHelpersGivenParent(documentContext, "div");

            // Assert
            Assert.Equal(expectedDescriptors, descriptors, TagHelperDescriptorComparer.CaseSensitive);
        }
    }
}
