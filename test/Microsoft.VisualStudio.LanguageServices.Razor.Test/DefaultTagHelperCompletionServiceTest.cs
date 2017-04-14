// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public class DefaultTagHelperCompletionServiceTest
    {
        [Fact]
        public void GetElementCompletions_TagOutputHintDoesNotFallThroughToSchemaCheck()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("MyTableTagHelper", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("my-table"))
                    .TagOutputHint("table")
                    .Build(),
                TagHelperDescriptorBuilder.Create("MyTrTagHelper", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("my-tr"))
                    .TagOutputHint("tr")
                    .Build(),
            };
            var expectedCompletions = ElementCompletionResult.Create(new Dictionary<string, HashSet<TagHelperDescriptor>>()
            {
                ["my-table"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0] },
                ["table"] = new HashSet<TagHelperDescriptor>(),
            });

            var existingCompletions = new[] { "table" };
            var completionContext = BuildCompletionContext(
                documentDescriptors,
                existingCompletions,
                containingTagName: "body",
                containingParentTagName: null);
            var service = CreateTagHelperCompletionFactsService();

            // Act
            var completions = service.GetElementCompletions(completionContext);

            // Assert
            AssertCompletionsAreEquivalent(expectedCompletions, completions);
        }

        [Fact]
        public void GetElementCompletions_CatchAllsOnlyApplyToCompletionsStartingWithPrefix()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("CatchAllTagHelper", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("*"))
                    .Build(),
                TagHelperDescriptorBuilder.Create("LiTagHelper", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("li"))
                    .Build(),
            };
            var expectedCompletions = ElementCompletionResult.Create(new Dictionary<string, HashSet<TagHelperDescriptor>>()
            {
                ["th:li"] = new HashSet<TagHelperDescriptor> { documentDescriptors[1], documentDescriptors[0] },
                ["li"] = new HashSet<TagHelperDescriptor>(),
            });

            var existingCompletions = new[] { "li" };
            var completionContext = BuildCompletionContext(
                documentDescriptors,
                existingCompletions,
                containingTagName: "ul",
                tagHelperPrefix: "th:");
            var service = CreateTagHelperCompletionFactsService();

            // Act
            var completions = service.GetElementCompletions(completionContext);

            // Assert
            AssertCompletionsAreEquivalent(expectedCompletions, completions);
        }

        [Fact]
        public void GetElementCompletions_TagHelperPrefixIsPrependedToTagHelperCompletions()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("SuperLiTagHelper", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("superli"))
                    .Build(),
                TagHelperDescriptorBuilder.Create("LiTagHelper", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("li"))
                    .Build(),
            };
            var expectedCompletions = ElementCompletionResult.Create(new Dictionary<string, HashSet<TagHelperDescriptor>>()
            {
                ["th:superli"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0] },
                ["th:li"] = new HashSet<TagHelperDescriptor> { documentDescriptors[1] },
                ["li"] = new HashSet<TagHelperDescriptor>(),
            });

            var existingCompletions = new[] { "li" };
            var completionContext = BuildCompletionContext(
                documentDescriptors,
                existingCompletions,
                containingTagName: "ul",
                tagHelperPrefix: "th:");
            var service = CreateTagHelperCompletionFactsService();

            // Act
            var completions = service.GetElementCompletions(completionContext);

            // Assert
            AssertCompletionsAreEquivalent(expectedCompletions, completions);
        }

        [Fact]
        public void GetElementCompletions_CatchAllsApplyToAllCompletions()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("SuperLiTagHelper", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("superli"))
                    .Build(),
                TagHelperDescriptorBuilder.Create("CatchAll", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("*"))
                    .Build(),
            };
            var expectedCompletions = ElementCompletionResult.Create(new Dictionary<string, HashSet<TagHelperDescriptor>>()
            {
                ["superli"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0], documentDescriptors[1] },
                ["li"] = new HashSet<TagHelperDescriptor> { documentDescriptors[1] },
            });

            var existingCompletions = new[] { "li" };
            var completionContext = BuildCompletionContext(
                documentDescriptors,
                existingCompletions,
                containingTagName: "ul");
            var service = CreateTagHelperCompletionFactsService();

            // Act
            var completions = service.GetElementCompletions(completionContext);

            // Assert
            AssertCompletionsAreEquivalent(expectedCompletions, completions);
        }

        [Fact]
        public void GetElementCompletions_AllowsMultiTargetingTagHelpers()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("BoldTagHelper1", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("strong"))
                    .TagMatchingRule(rule => rule.RequireTagName("b"))
                    .TagMatchingRule(rule => rule.RequireTagName("bold"))
                    .Build(),
                TagHelperDescriptorBuilder.Create("BoldTagHelper2", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("strong"))
                    .Build(),
            };
            var expectedCompletions = ElementCompletionResult.Create(new Dictionary<string, HashSet<TagHelperDescriptor>>()
            {
                ["strong"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0], documentDescriptors[1] },
                ["b"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0] },
                ["bold"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0] },
            });

            var existingCompletions = new[] { "strong", "b", "bold" };
            var completionContext = BuildCompletionContext(
                documentDescriptors,
                existingCompletions,
                containingTagName: "ul");
            var service = CreateTagHelperCompletionFactsService();

            // Act
            var completions = service.GetElementCompletions(completionContext);

            // Assert
            AssertCompletionsAreEquivalent(expectedCompletions, completions);
        }

        [Fact]
        public void GetElementCompletions_CombinesDescriptorsOnExistingCompletions()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("LiTagHelper1", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("li"))
                    .Build(),
                TagHelperDescriptorBuilder.Create("LiTagHelper2", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("li"))
                    .Build(),
            };
            var expectedCompletions = ElementCompletionResult.Create(new Dictionary<string, HashSet<TagHelperDescriptor>>()
            {
                ["li"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0], documentDescriptors[1] },
            });

            var existingCompletions = new[] { "li" };
            var completionContext = BuildCompletionContext(documentDescriptors, existingCompletions, containingTagName: "ul");
            var service = CreateTagHelperCompletionFactsService();

            // Act
            var completions = service.GetElementCompletions(completionContext);

            // Assert
            AssertCompletionsAreEquivalent(expectedCompletions, completions);
        }

        [Fact]
        public void GetElementCompletions_NewCompletionsForSchemaTagsNotInExistingCompletionsAreIgnored()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("SuperLiTagHelper", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("superli"))
                    .Build(),
                TagHelperDescriptorBuilder.Create("LiTagHelper", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("li"))
                    .TagOutputHint("strong")
                    .Build(),
                TagHelperDescriptorBuilder.Create("DivTagHelper", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("div"))
                    .Build(),
            };
            var expectedCompletions = ElementCompletionResult.Create(new Dictionary<string, HashSet<TagHelperDescriptor>>()
            {
                ["li"] = new HashSet<TagHelperDescriptor> { documentDescriptors[1] },
                ["superli"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0] },
            });

            var existingCompletions = new[] { "li" };
            var completionContext = BuildCompletionContext(documentDescriptors, existingCompletions, containingTagName: "ul");
            var service = CreateTagHelperCompletionFactsService();

            // Act
            var completions = service.GetElementCompletions(completionContext);

            // Assert
            AssertCompletionsAreEquivalent(expectedCompletions, completions);
        }

        [Fact]
        public void GetElementCompletions_OutputHintIsCrossReferencedWithExistingCompletions()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("DivTagHelper", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("div"))
                    .TagOutputHint("li")
                    .Build(),
                TagHelperDescriptorBuilder.Create("LiTagHelper", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("li"))
                    .TagOutputHint("strong")
                    .Build(),
            };
            var expectedCompletions = ElementCompletionResult.Create(new Dictionary<string, HashSet<TagHelperDescriptor>>()
            {
                ["div"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0] },
                ["li"] = new HashSet<TagHelperDescriptor> { documentDescriptors[1] },
            });

            var existingCompletions = new[] { "li" };
            var completionContext = BuildCompletionContext(documentDescriptors, existingCompletions, containingTagName: "ul");
            var service = CreateTagHelperCompletionFactsService();

            // Act
            var completions = service.GetElementCompletions(completionContext);

            // Assert
            AssertCompletionsAreEquivalent(expectedCompletions, completions);
        }

        [Fact]
        public void GetElementCompletions_EnsuresDescriptorsHaveSatisfiedParent()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("LiTagHelper1", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("li"))
                    .Build(),
                TagHelperDescriptorBuilder.Create("LiTagHelper2", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("li").RequireParentTag("ol"))
                    .Build(),
            };
            var expectedCompletions = ElementCompletionResult.Create(new Dictionary<string, HashSet<TagHelperDescriptor>>()
            {
                ["li"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0] },
            });

            var existingCompletions = new[] { "li" };
            var completionContext = BuildCompletionContext(documentDescriptors, existingCompletions, containingTagName: "ul");
            var service = CreateTagHelperCompletionFactsService();

            // Act
            var completions = service.GetElementCompletions(completionContext);

            // Assert
            AssertCompletionsAreEquivalent(expectedCompletions, completions);
        }

        [Fact]
        public void GetElementCompletions_AllowedChildrenAreIgnoredWhenAtRoot()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("CatchAll", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("*"))
                    .AllowChildTag("b")
                    .AllowChildTag("bold")
                    .AllowChildTag("div")
                    .Build(),
            };
            var expectedCompletions = ElementCompletionResult.Create(new Dictionary<string, HashSet<TagHelperDescriptor>>());

            var existingCompletions = Enumerable.Empty<string>();
            var completionContext = BuildCompletionContext(
                documentDescriptors,
                existingCompletions,
                containingTagName: null,
                containingParentTagName: null);
            var service = CreateTagHelperCompletionFactsService();

            // Act
            var completions = service.GetElementCompletions(completionContext);

            // Assert
            AssertCompletionsAreEquivalent(expectedCompletions, completions);
        }

        [Fact]
        public void GetElementCompletions_DoesNotReturnExistingCompletionsWhenAllowedChildren()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("BoldParent", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("div"))
                    .AllowChildTag("b")
                    .AllowChildTag("bold")
                    .AllowChildTag("div")
                    .Build(),
            };
            var expectedCompletions = ElementCompletionResult.Create(new Dictionary<string, HashSet<TagHelperDescriptor>>()
            {
                ["b"] = new HashSet<TagHelperDescriptor>(),
                ["bold"] = new HashSet<TagHelperDescriptor>(),
                ["div"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0] }
            });

            var existingCompletions = new[] { "p", "em" };
            var completionContext = BuildCompletionContext(documentDescriptors, existingCompletions, containingTagName: "div");
            var service = CreateTagHelperCompletionFactsService();

            // Act
            var completions = service.GetElementCompletions(completionContext);

            // Assert
            AssertCompletionsAreEquivalent(expectedCompletions, completions);
        }

        [Fact]
        public void GetElementCompletions_CapturesAllAllowedChildTagsFromParentTagHelpers_NoneTagHelpers()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("BoldParent", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("div"))
                    .AllowChildTag("b")
                    .AllowChildTag("bold")
                    .Build(),
            };
            var expectedCompletions = ElementCompletionResult.Create(new Dictionary<string, HashSet<TagHelperDescriptor>>()
            {
                ["b"] = new HashSet<TagHelperDescriptor>(),
                ["bold"] = new HashSet<TagHelperDescriptor>(),
            });

            var completionContext = BuildCompletionContext(documentDescriptors, Enumerable.Empty<string>(), containingTagName: "div");
            var service = CreateTagHelperCompletionFactsService();

            // Act
            var completions = service.GetElementCompletions(completionContext);

            // Assert
            AssertCompletionsAreEquivalent(expectedCompletions, completions);
        }

        [Fact]
        public void GetElementCompletions_CapturesAllAllowedChildTagsFromParentTagHelpers_SomeTagHelpers()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("BoldParent", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("div"))
                    .AllowChildTag("b")
                    .AllowChildTag("bold")
                    .AllowChildTag("div")
                    .Build(),
            };
            var expectedCompletions = ElementCompletionResult.Create(new Dictionary<string, HashSet<TagHelperDescriptor>>()
            {
                ["b"] = new HashSet<TagHelperDescriptor>(),
                ["bold"] = new HashSet<TagHelperDescriptor>(),
                ["div"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0] }
            });

            var completionContext = BuildCompletionContext(documentDescriptors, Enumerable.Empty<string>(), containingTagName: "div");
            var service = CreateTagHelperCompletionFactsService();

            // Act
            var completions = service.GetElementCompletions(completionContext);

            // Assert
            AssertCompletionsAreEquivalent(expectedCompletions, completions);
        }

        [Fact]
        public void GetElementCompletions_CapturesAllAllowedChildTagsFromParentTagHelpers_AllTagHelpers()
        {
            // Arrange
            var documentDescriptors = new[]
            {
                TagHelperDescriptorBuilder.Create("BoldParentCatchAll", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("*"))
                    .AllowChildTag("strong")
                    .AllowChildTag("div")
                    .AllowChildTag("b")
                    .Build(),
                TagHelperDescriptorBuilder.Create("BoldParent", "TestAssembly")
                    .TagMatchingRule(rule => rule.RequireTagName("div"))
                    .AllowChildTag("b")
                    .AllowChildTag("bold")
                    .Build(),
            };
            var expectedCompletions = ElementCompletionResult.Create(new Dictionary<string, HashSet<TagHelperDescriptor>>()
            {
                ["strong"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0] },
                ["b"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0] },
                ["bold"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0] },
                ["div"] = new HashSet<TagHelperDescriptor> { documentDescriptors[0], documentDescriptors[1] },
            });

            var completionContext = BuildCompletionContext(documentDescriptors, Enumerable.Empty<string>(), containingTagName: "div");
            var service = CreateTagHelperCompletionFactsService();

            // Act
            var completions = service.GetElementCompletions(completionContext);

            // Assert
            AssertCompletionsAreEquivalent(expectedCompletions, completions);
        }

        private static DefaultTagHelperCompletionService CreateTagHelperCompletionFactsService()
        {
            var tagHelperFactService = new DefaultTagHelperFactsService();
            var completionFactService = new DefaultTagHelperCompletionService(tagHelperFactService);

            return completionFactService;
        }

        private static void AssertCompletionsAreEquivalent(ElementCompletionResult expected, ElementCompletionResult actual)
        {
            Assert.Equal(expected.Completions.Count, actual.Completions.Count);

            foreach (var expectedCompletion in expected.Completions)
            {
                var actualValue = actual.Completions[expectedCompletion.Key];
                Assert.NotNull(actualValue);
                Assert.Equal(expectedCompletion.Value, actualValue, TagHelperDescriptorComparer.CaseSensitive);
            }
        }

        private static ElementCompletionContext BuildCompletionContext(
            IEnumerable<TagHelperDescriptor> descriptors,
            IEnumerable<string> existingCompletions,
            string containingTagName,
            string containingParentTagName = "body",
            string tagHelperPrefix = "")
        {
            var documentContext = TagHelperDocumentContext.Create(tagHelperPrefix, descriptors);
            var completionContext = new ElementCompletionContext(
                documentContext,
                existingCompletions,
                containingTagName,
                attributes: Enumerable.Empty<KeyValuePair<string, string>>(),
                containingParentTagName: containingParentTagName,
                inHTMLSchema: (tag) => tag == "strong" || tag == "b" || tag == "bold" || tag == "li" || tag == "div");

            return completionContext;
        }
    }
}
