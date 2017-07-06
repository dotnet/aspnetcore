// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class TagHelperBinderTest
    {
        [Fact]
        public void GetBinding_ReturnsBindingWithInformation()
        {
            // Arrange
            var divTagHelper = TagHelperDescriptorBuilder.Create("DivTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .Build();
            var expectedDescriptors = new[] { divTagHelper };
            var expectedAttributes = new[]
            {
                new KeyValuePair<string, string>("class", "something")
            };
            var tagHelperBinder = new TagHelperBinder("th:", expectedDescriptors);

            // Act
            var bindingResult = tagHelperBinder.GetBinding(
                tagName: "th:div",
                attributes: expectedAttributes,
                parentTagName: "body");

            // Assert
            Assert.Equal(expectedDescriptors, bindingResult.Descriptors, TagHelperDescriptorComparer.CaseSensitive);
            Assert.Equal("th:div", bindingResult.TagName);
            Assert.Equal("body", bindingResult.ParentTagName);
            Assert.Equal(expectedAttributes, bindingResult.Attributes);
            Assert.Equal("th:", bindingResult.TagHelperPrefix);
            Assert.Equal(divTagHelper.TagMatchingRules, bindingResult.GetBoundRules(divTagHelper), TagMatchingRuleDescriptorComparer.CaseSensitive);
        }

        public static TheoryData RequiredParentData
        {
            get
            {
                var strongPDivParent = TagHelperDescriptorBuilder.Create("StrongTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("strong")
                        .RequireParentTag("p"))
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("strong")
                        .RequireParentTag("div"))
                    .Build();
                var catchAllPParent = TagHelperDescriptorBuilder.Create("CatchAllTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
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
        public void GetBinding_ReturnsBindingResultWithDescriptorsParentTags(
            string tagName,
            string parentTagName,
            object availableDescriptors,
            object expectedDescriptors)
        {
            // Arrange
            var tagHelperBinder = new TagHelperBinder(null, (IEnumerable<TagHelperDescriptor>)availableDescriptors);

            // Act
            var bindingResult = tagHelperBinder.GetBinding(
                tagName,
                attributes: Array.Empty<KeyValuePair<string, string>>(),
                parentTagName: parentTagName);

            // Assert
            Assert.Equal((IEnumerable<TagHelperDescriptor>)expectedDescriptors, bindingResult.Descriptors, TagHelperDescriptorComparer.CaseSensitive);
        }

        public static TheoryData RequiredAttributeData
        {
            get
            {
                var divDescriptor = TagHelperDescriptorBuilder.Create("DivTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("div")
                        .RequireAttributeDescriptor(attribute => attribute.Name("style")))
                    .Build();
                var inputDescriptor = TagHelperDescriptorBuilder.Create("InputTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireAttributeDescriptor(attribute => attribute.Name("class"))
                        .RequireAttributeDescriptor(attribute => attribute.Name("style")))
                    .Build();
                var inputWildcardPrefixDescriptor = TagHelperDescriptorBuilder.Create("InputWildCardAttribute", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireAttributeDescriptor(attribute =>
                            attribute
                            .Name("nodashprefix")
                            .NameComparisonMode(RequiredAttributeDescriptor.NameComparisonMode.PrefixMatch)))
                    .Build();
                var catchAllDescriptor = TagHelperDescriptorBuilder.Create("CatchAllTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                        .RequireAttributeDescriptor(attribute => attribute.Name("class")))
                    .Build();
                var catchAllDescriptor2 = TagHelperDescriptorBuilder.Create("CatchAllTagHelper2", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                        .RequireAttributeDescriptor(attribute => attribute.Name("custom"))
                        .RequireAttributeDescriptor(attribute => attribute.Name("class")))
                    .Build();
                var catchAllWildcardPrefixDescriptor = TagHelperDescriptorBuilder.Create("CatchAllWildCardAttribute", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                        .RequireAttributeDescriptor(attribute =>
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
                    IReadOnlyList<KeyValuePair<string, string>>, // providedAttributes
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
        public void GetBinding_ReturnsBindingResultDescriptorsWithRequiredAttributes(
            string tagName,
            IReadOnlyList<KeyValuePair<string, string>> providedAttributes,
            object availableDescriptors,
            object expectedDescriptors)
        {
            // Arrange
            var tagHelperBinder = new TagHelperBinder(null, (IReadOnlyList<TagHelperDescriptor>)availableDescriptors);

            // Act
            var bindingResult = tagHelperBinder.GetBinding(tagName, providedAttributes, parentTagName: "p");

            // Assert
            Assert.Equal((IEnumerable<TagHelperDescriptor>)expectedDescriptors, bindingResult?.Descriptors, TagHelperDescriptorComparer.CaseSensitive);
        }

        [Fact]
        public void GetBinding_ReturnsNullBindingResultPrefixAsTagName()
        {
            // Arrange
            var catchAllDescriptor = TagHelperDescriptorBuilder.Create("foo1", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName(TagHelperMatchingConventions.ElementCatchAllName))
                .Build();
            var descriptors = new[] { catchAllDescriptor };
            var tagHelperBinder = new TagHelperBinder("th", descriptors);

            // Act
            var bindingResult = tagHelperBinder.GetBinding(
                tagName: "th",
                attributes: Array.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

            // Assert
            Assert.Null(bindingResult);
        }

        [Fact]
        public void GetBinding_ReturnsBindingResultCatchAllDescriptorsForPrefixedTags()
        {
            // Arrange
            var catchAllDescriptor = TagHelperDescriptorBuilder.Create("foo1", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName(TagHelperMatchingConventions.ElementCatchAllName))
                .Build();
            var descriptors = new[] { catchAllDescriptor };
            var tagHelperBinder = new TagHelperBinder("th:", descriptors);

            // Act
            var bindingResultDiv = tagHelperBinder.GetBinding(
                tagName: "th:div",
                attributes: Array.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");
            var bindingResultSpan = tagHelperBinder.GetBinding(
                tagName: "th:span",
                attributes: Array.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

            // Assert
            var descriptor = Assert.Single(bindingResultDiv.Descriptors);
            Assert.Same(catchAllDescriptor, descriptor);
            descriptor = Assert.Single(bindingResultSpan.Descriptors);
            Assert.Same(catchAllDescriptor, descriptor);
        }

        [Fact]
        public void GetBinding_ReturnsBindingResultDescriptorsForPrefixedTags()
        {
            // Arrange
            var divDescriptor = TagHelperDescriptorBuilder.Create("foo1", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .Build();
            var descriptors = new[] { divDescriptor };
            var tagHelperBinder = new TagHelperBinder("th:", descriptors);

            // Act
            var bindingResult = tagHelperBinder.GetBinding(
                tagName: "th:div",
                attributes: Array.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

            // Assert
            var descriptor = Assert.Single(bindingResult.Descriptors);
            Assert.Same(divDescriptor, descriptor);
        }

        [Theory]
        [InlineData("*")]
        [InlineData("div")]
        public void GetBinding_ReturnsNullForUnprefixedTags(string tagName)
        {
            // Arrange
            var divDescriptor = TagHelperDescriptorBuilder.Create("foo1", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName(tagName))
                .Build();
            var descriptors = new[] { divDescriptor };
            var tagHelperBinder = new TagHelperBinder("th:", descriptors);

            // Act
            var bindingResult = tagHelperBinder.GetBinding(
                tagName: "div",
                attributes: Array.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

            // Assert
            Assert.Null(bindingResult);
        }

        [Fact]
        public void GetDescriptors_ReturnsNothingForUnregisteredTags()
        {
            // Arrange
            var divDescriptor = TagHelperDescriptorBuilder.Create("foo1", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .Build();
            var spanDescriptor = TagHelperDescriptorBuilder.Create("foo2", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("span"))
                .Build();
            var descriptors = new TagHelperDescriptor[] { divDescriptor, spanDescriptor };
            var tagHelperBinder = new TagHelperBinder(null, descriptors);

            // Act
            var tagHelperBinding = tagHelperBinder.GetBinding(
                tagName: "foo",
                attributes: Array.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

            // Assert
            Assert.Null(tagHelperBinding);
        }

        [Fact]
        public void GetDescriptors_ReturnsCatchAllsWithEveryTagName()
        {
            // Arrange
            var divDescriptor = TagHelperDescriptorBuilder.Create("foo1", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .Build();
            var spanDescriptor = TagHelperDescriptorBuilder.Create("foo2", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("span"))
                .Build();
            var catchAllDescriptor = TagHelperDescriptorBuilder.Create("foo3", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName(TagHelperMatchingConventions.ElementCatchAllName))
                .Build();
            var descriptors = new TagHelperDescriptor[] { divDescriptor, spanDescriptor, catchAllDescriptor };
            var tagHelperBinder = new TagHelperBinder(null, descriptors);

            // Act
            var divBinding = tagHelperBinder.GetBinding(
                tagName: "div",
                attributes: Array.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");
            var spanBinding = tagHelperBinder.GetBinding(
                tagName: "span",
                attributes: Array.Empty<KeyValuePair<string, string>>(),
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
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .Build();
            var descriptors = new TagHelperDescriptor[] { divDescriptor, divDescriptor };
            var tagHelperBinder = new TagHelperBinder(null, descriptors);

            // Act
            var bindingResult = tagHelperBinder.GetBinding(
                tagName: "div",
                attributes: Array.Empty<KeyValuePair<string, string>>(),
                parentTagName: "p");

            // Assert
            var descriptor = Assert.Single(bindingResult.Descriptors);
            Assert.Same(divDescriptor, descriptor);
        }

        [Fact]
        public void GetBinding_DescriptorWithMultipleRules_CorrectlySelectsMatchingRules()
        {
            // Arrange
            var multiRuleDescriptor = TagHelperDescriptorBuilder.Create("foo", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName(TagHelperMatchingConventions.ElementCatchAllName)
                    .RequireParentTag("body"))
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("div"))
                .TagMatchingRuleDescriptor(rule => rule
                    .RequireTagName("span"))
                .Build();
            var descriptors = new TagHelperDescriptor[] { multiRuleDescriptor };
            var tagHelperBinder = new TagHelperBinder(null, descriptors);

            // Act
            var binding = tagHelperBinder.GetBinding(
                tagName: "div",
                attributes: Array.Empty<KeyValuePair<string, string>>(),
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