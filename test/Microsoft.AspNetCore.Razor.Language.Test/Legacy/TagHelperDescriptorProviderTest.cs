// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class TagHelperDescriptorProviderTest
    {
        public static TheoryData RequiredParentData
        {
            get
            {
                var strongPDivParent = TagHelperDescriptorBuilder.Create("StrongTagHelper", "SomeAssembly")
                    .TagMatchingRule(rule =>
                        rule
                        .RequireTagName("strong")
                        .RequireParentTag("p"))
                    .TagMatchingRule(rule =>
                        rule
                        .RequireTagName("strong")
                        .RequireParentTag("div"))
                    .Build();
                var catchAllPParent = TagHelperDescriptorBuilder.Create("CatchAllTagHelper", "SomeAssembly")
                    .TagMatchingRule(rule =>
                        rule
                        .RequireTagName("*")
                        .RequireParentTag("p"))
                    .Build();

                return new TheoryData<
                    string, // tagName
                    string, // parentTagName
                    IEnumerable<TagHelperDescriptor>, // availableDescriptors
                    IEnumerable<TagHelperDescriptor>> // expectedDescriptors
                {
                    {
                        "strong",
                        "p",
                        new[] { strongPDivParent },
                        new[] { strongPDivParent }
                    },
                    {
                        "strong",
                        "div",
                        new[] { strongPDivParent, catchAllPParent },
                        new[] { strongPDivParent }
                    },
                    {
                        "strong",
                        "p",
                        new[] { strongPDivParent, catchAllPParent },
                        new[] { strongPDivParent, catchAllPParent }
                    },
                    {
                        "custom",
                        "p",
                        new[] { strongPDivParent, catchAllPParent },
                        new[] { catchAllPParent }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(RequiredParentData))]
        public void GetTagHelperBinding_ReturnsBindingResultWithDescriptorsParentTags(
            string tagName,
            string parentTagName,
            object availableDescriptors,
            object expectedDescriptors)
        {
            // Arrange
            var provider = new TagHelperDescriptorProvider(null, (IEnumerable<TagHelperDescriptor>)availableDescriptors);

            // Act
            var bindingResult = provider.GetTagHelperBinding(
                tagName,
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: parentTagName);

            // Assert
            Assert.Equal((IEnumerable<TagHelperDescriptor>)expectedDescriptors, bindingResult.Descriptors, TagHelperDescriptorComparer.CaseSensitive);
        }

        public static TheoryData RequiredAttributeData
        {
            get
            {
                var divDescriptor = TagHelperDescriptorBuilder.Create("DivTagHelper", "SomeAssembly")
                    .TagMatchingRule(rule =>
                        rule
                        .RequireTagName("div")
                        .RequireAttribute(attribute => attribute.Name("style")))
                    .Build();
                var inputDescriptor = TagHelperDescriptorBuilder.Create("InputTagHelper", "SomeAssembly")
                    .TagMatchingRule(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireAttribute(attribute => attribute.Name("class"))
                        .RequireAttribute(attribute => attribute.Name("style")))
                    .Build();
                var inputWildcardPrefixDescriptor = TagHelperDescriptorBuilder.Create("InputWildCardAttribute", "SomeAssembly")
                    .TagMatchingRule(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireAttribute(attribute => 
                            attribute
                            .Name("nodashprefix")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch)))
                    .Build();
                var catchAllDescriptor = TagHelperDescriptorBuilder.Create("CatchAllTagHelper", "SomeAssembly")
                    .TagMatchingRule(rule =>
                        rule
                        .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                        .RequireAttribute(attribute => attribute.Name("class")))
                    .Build();
                var catchAllDescriptor2 = TagHelperDescriptorBuilder.Create("CatchAllTagHelper2", "SomeAssembly")
                    .TagMatchingRule(rule =>
                        rule
                        .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                        .RequireAttribute(attribute => attribute.Name("custom"))
                        .RequireAttribute(attribute => attribute.Name("class")))
                    .Build();
                var catchAllWildcardPrefixDescriptor = TagHelperDescriptorBuilder.Create("CatchAllWildCardAttribute", "SomeAssembly")
                    .TagMatchingRule(rule =>
                        rule
                        .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                        .RequireAttribute(attribute => 
                            attribute
                            .Name("prefix-")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch)))
                    .Build();
                var defaultAvailableDescriptors =
                    new[] { divDescriptor, inputDescriptor, catchAllDescriptor, catchAllDescriptor2 };
                var defaultWildcardDescriptors =
                    new[] { inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor };
                Func<string, KeyValuePair<string, string>> kvp =
                    (name) => new KeyValuePair<string, string>(name, "test value");

                return new TheoryData<
                    string, // tagName
                    IEnumerable<KeyValuePair<string, string>>, // providedAttributes
                    IEnumerable<TagHelperDescriptor>, // availableDescriptors
                    IEnumerable<TagHelperDescriptor>> // expectedDescriptors
                {
                    {
                        "div",
                        new[] { kvp("custom") },
                        defaultAvailableDescriptors,
                        null
                    },
                    { "div", new[] { kvp("style") }, defaultAvailableDescriptors, new[] { divDescriptor } },
                    { "div", new[] { kvp("class") }, defaultAvailableDescriptors, new[] { catchAllDescriptor } },
                    {
                        "div",
                        new[] { kvp("class"), kvp("style") },
                        defaultAvailableDescriptors,
                        new[] { divDescriptor, catchAllDescriptor }
                    },
                    {
                        "div",
                        new[] { kvp("class"), kvp("style"), kvp("custom") },
                        defaultAvailableDescriptors,
                        new[] { divDescriptor, catchAllDescriptor, catchAllDescriptor2 }
                    },
                    {
                        "input",
                        new[] { kvp("class"), kvp("style") },
                        defaultAvailableDescriptors,
                        new[] { inputDescriptor, catchAllDescriptor }
                    },
                    {
                        "input",
                        new[] { kvp("nodashprefixA") },
                        defaultWildcardDescriptors,
                        new[] { inputWildcardPrefixDescriptor }
                    },
                    {
                        "input",
                        new[] { kvp("nodashprefix-ABC-DEF"), kvp("random") },
                        defaultWildcardDescriptors,
                        new[] { inputWildcardPrefixDescriptor }
                    },
                    {
                        "input",
                        new[] { kvp("prefixABCnodashprefix") },
                        defaultWildcardDescriptors,
                        null
                    },
                    {
                        "input",
                        new[] { kvp("prefix-") },
                        defaultWildcardDescriptors,
                        null
                    },
                    {
                        "input",
                        new[] { kvp("nodashprefix") },
                        defaultWildcardDescriptors,
                        null
                    },
                    {
                        "input",
                        new[] { kvp("prefix-A") },
                        defaultWildcardDescriptors,
                        new[] { catchAllWildcardPrefixDescriptor }
                    },
                    {
                        "input",
                        new[] { kvp("prefix-ABC-DEF"), kvp("random") },
                        defaultWildcardDescriptors,
                        new[] { catchAllWildcardPrefixDescriptor }
                    },
                    {
                        "input",
                        new[] { kvp("prefix-abc"), kvp("nodashprefix-def") },
                        defaultWildcardDescriptors,
                        new[] { inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor }
                    },
                    {
                        "input",
                        new[] { kvp("class"), kvp("prefix-abc"), kvp("onclick"), kvp("nodashprefix-def"), kvp("style") },
                        defaultWildcardDescriptors,
                        new[] { inputWildcardPrefixDescriptor, catchAllWildcardPrefixDescriptor }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(RequiredAttributeData))]
        public void GetTagHelperBinding_ReturnsBindingResultDescriptorsWithRequiredAttributes(
            string tagName,
            IEnumerable<KeyValuePair<string, string>> providedAttributes,
            object availableDescriptors,
            object expectedDescriptors)
        {
            // Arrange
            var provider = new TagHelperDescriptorProvider(null, (IEnumerable<TagHelperDescriptor>)availableDescriptors);

            // Act
            var bindingResult = provider.GetTagHelperBinding(tagName, providedAttributes, parentTagName: "p");

            // Assert
            Assert.Equal((IEnumerable<TagHelperDescriptor>)expectedDescriptors, bindingResult?.Descriptors, TagHelperDescriptorComparer.CaseSensitive);
        }

        [Fact]
        public void GetTagHelperBinding_ReturnsNullBindingResultPrefixAsTagName()
        {
            // Arrange
            var catchAllDescriptor = TagHelperDescriptorBuilder.Create("foo1", "SomeAssembly")
                .TagMatchingRule(rule => rule.RequireTagName(TagHelperMatchingConventions.ElementCatchAllName))
                .Build();
            var descriptors = new[] { catchAllDescriptor };
            var provider = new TagHelperDescriptorProvider("th", descriptors);

            // Act
            var bindingResult = provider.GetTagHelperBinding(
                tagName: "th",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

            // Assert
            Assert.Null(bindingResult);
        }

        [Fact]
        public void GetTagHelperBinding_ReturnsBindingResultCatchAllDescriptorsForPrefixedTags()
        {
            // Arrange
            var catchAllDescriptor = TagHelperDescriptorBuilder.Create("foo1", "SomeAssembly")
                .TagMatchingRule(rule => rule.RequireTagName(TagHelperMatchingConventions.ElementCatchAllName))
                .Build();
            var descriptors = new[] { catchAllDescriptor };
            var provider = new TagHelperDescriptorProvider("th:", descriptors);

            // Act
            var bindingResultDiv = provider.GetTagHelperBinding(
                tagName: "th:div",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");
            var bindingResultSpan = provider.GetTagHelperBinding(
                tagName: "th:span",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

            // Assert
            var descriptor = Assert.Single(bindingResultDiv.Descriptors);
            Assert.Same(catchAllDescriptor, descriptor);
            descriptor = Assert.Single(bindingResultSpan.Descriptors);
            Assert.Same(catchAllDescriptor, descriptor);
        }

        [Fact]
        public void GetTagHelperBinding_ReturnsBindingResultDescriptorsForPrefixedTags()
        {
            // Arrange
            var divDescriptor = TagHelperDescriptorBuilder.Create("foo1", "SomeAssembly")
                .TagMatchingRule(rule => rule.RequireTagName("div"))
                .Build();
            var descriptors = new[] { divDescriptor };
            var provider = new TagHelperDescriptorProvider("th:", descriptors);

            // Act
            var bindingResult = provider.GetTagHelperBinding(
                tagName: "th:div",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

            // Assert
            var descriptor = Assert.Single(bindingResult.Descriptors);
            Assert.Same(divDescriptor, descriptor);
        }

        [Theory]
        [InlineData("*")]
        [InlineData("div")]
        public void GetTagHelperBinding_ReturnsNullForUnprefixedTags(string tagName)
        {
            // Arrange
            var divDescriptor = TagHelperDescriptorBuilder.Create("foo1", "SomeAssembly")
                .TagMatchingRule(rule => rule.RequireTagName(tagName))
                .Build();
            var descriptors = new[] { divDescriptor };
            var provider = new TagHelperDescriptorProvider("th:", descriptors);

            // Act
            var bindingResult = provider.GetTagHelperBinding(
                tagName: "div",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

            // Assert
            Assert.Null(bindingResult);
        }

        [Fact]
        public void GetDescriptors_ReturnsNothingForUnregisteredTags()
        {
            // Arrange
            var divDescriptor = TagHelperDescriptorBuilder.Create("foo1", "SomeAssembly")
                .TagMatchingRule(rule => rule.RequireTagName("div"))
                .Build();
            var spanDescriptor = TagHelperDescriptorBuilder.Create("foo2", "SomeAssembly")
                .TagMatchingRule(rule => rule.RequireTagName("span"))
                .Build();
            var descriptors = new TagHelperDescriptor[] { divDescriptor, spanDescriptor };
            var provider = new TagHelperDescriptorProvider(null, descriptors);

            // Act
            var tagHelperBinding = provider.GetTagHelperBinding(
                tagName: "foo",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

            // Assert
            Assert.Null(tagHelperBinding);
        }

        [Fact]
        public void GetDescriptors_ReturnsCatchAllsWithEveryTagName()
        {
            // Arrange
            var divDescriptor = TagHelperDescriptorBuilder.Create("foo1", "SomeAssembly")
                .TagMatchingRule(rule => rule.RequireTagName("div"))
                .Build();
            var spanDescriptor = TagHelperDescriptorBuilder.Create("foo2", "SomeAssembly")
                .TagMatchingRule(rule => rule.RequireTagName("span"))
                .Build();
            var catchAllDescriptor = TagHelperDescriptorBuilder.Create("foo3", "SomeAssembly")
                .TagMatchingRule(rule => rule.RequireTagName(TagHelperMatchingConventions.ElementCatchAllName))
                .Build();
            var descriptors = new TagHelperDescriptor[] { divDescriptor, spanDescriptor, catchAllDescriptor };
            var provider = new TagHelperDescriptorProvider(null, descriptors);

            // Act
            var divBinding = provider.GetTagHelperBinding(
                tagName: "div",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");
            var spanBinding = provider.GetTagHelperBinding(
                tagName: "span",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

            // Assert
            // For divs
            Assert.Equal(2, divBinding.Descriptors.Count());
            Assert.Contains(divDescriptor, divBinding.Descriptors);
            Assert.Contains(catchAllDescriptor, divBinding.Descriptors);

            // For spans
            Assert.Equal(2, spanBinding.Descriptors.Count());
            Assert.Contains(spanDescriptor, spanBinding.Descriptors);
            Assert.Contains(catchAllDescriptor, spanBinding.Descriptors);
        }

        [Fact]
        public void GetDescriptors_DuplicateDescriptorsAreNotPartOfTagHelperDescriptorPool()
        {
            // Arrange
            var divDescriptor = TagHelperDescriptorBuilder.Create("foo1", "SomeAssembly")
                .TagMatchingRule(rule => rule.RequireTagName("div"))
                .Build(); 
            var descriptors = new TagHelperDescriptor[] { divDescriptor, divDescriptor };
            var provider = new TagHelperDescriptorProvider(null, descriptors);

            // Act
            var bindingResult = provider.GetTagHelperBinding(
                tagName: "div",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

            // Assert
            var descriptor = Assert.Single(bindingResult.Descriptors);
            Assert.Same(divDescriptor, descriptor);
        }

        [Fact]
        public void GetTagHelperBinding_DescriptorWithMultipleRules_CorrectlySelectsMatchingRules()
        {
            // Arrange
            var multiRuleDescriptor = TagHelperDescriptorBuilder.Create("foo", "SomeAssembly")
                .TagMatchingRule(rule => rule
                    .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                    .RequireParentTag("body"))
                .TagMatchingRule(rule => rule
                    .RequireTagName("div"))
                .TagMatchingRule(rule => rule
                    .RequireTagName("span"))
                .Build();
            var descriptors = new TagHelperDescriptor[] { multiRuleDescriptor };
            var provider = new TagHelperDescriptorProvider(null, descriptors);

            // Act
            var binding = provider.GetTagHelperBinding(
                tagName: "div",
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

            // Assert
            var boundDescriptor = Assert.Single(binding.Descriptors);
            Assert.Same(multiRuleDescriptor, boundDescriptor);
            var boundRules = binding.GetBoundRules(boundDescriptor);
            var boundRule = Assert.Single(boundRules);
            Assert.Equal("div", boundRule.TagName);
        }
    }
}