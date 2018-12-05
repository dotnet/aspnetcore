// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class TagHelperParseTreeRewriterTest : TagHelperRewritingTestBase
    {
        public static TheoryData GetAttributeNameValuePairsData
        {
            get
            {
                Func<string, string, KeyValuePair<string, string>> kvp =
                    (key, value) => new KeyValuePair<string, string>(key, value);
                var empty = Enumerable.Empty<KeyValuePair<string, string>>();
                var csharp = TagHelperParseTreeRewriter.Rewriter.InvalidAttributeValueMarker;

                // documentContent, expectedPairs
                return new TheoryData<string, IEnumerable<KeyValuePair<string, string>>>
                {
                    { "<a>", empty },
                    { "<a @{ } href='~/home'>", empty },
                    { "<a href=\"@true\">", new[] { kvp("href", csharp) } },
                    { "<a href=\"prefix @true suffix\">", new[] { kvp("href", $"prefix{csharp} suffix") } },
                    { "<a href=~/home>", new[] { kvp("href", "~/home") } },
                    { "<a href=~/home @{ } nothing='something'>", new[] { kvp("href", "~/home") } },
                    {
                        "<a href=\"@DateTime.Now::0\" class='btn btn-success' random>",
                        new[] { kvp("href", $"{csharp}::0"), kvp("class", "btn btn-success"), kvp("random", "") }
                    },
                    { "<a href=>", new[] { kvp("href", "") } },
                    { "<a href='\">  ", new[] { kvp("href", "\">  ") } },
                    { "<a href'", new[] { kvp("href'", "") } },
                };
            }
        }

        [Theory]
        [MemberData(nameof(GetAttributeNameValuePairsData))]
        public void GetAttributeNameValuePairs_ParsesPairsCorrectly(
            string documentContent,
            IEnumerable<KeyValuePair<string, string>> expectedPairs)
        {
            // Arrange
            var errorSink = new ErrorSink();
            var parseResult = ParseDocument(documentContent);
            var document = parseResult.Root;
            var parseTreeRewriter = new TagHelperParseTreeRewriter.Rewriter(
                parseResult.Source,
                null,
                Enumerable.Empty<TagHelperDescriptor>(),
                parseResult.Options.FeatureFlags,
                errorSink);

            // Assert - Guard
            var rootBlock = Assert.IsType<RazorDocumentSyntax>(document);
            var rootMarkup = Assert.IsType<MarkupBlockSyntax>(rootBlock.Document);
            var childBlock = Assert.Single(rootMarkup.Children);
            var tagBlock = Assert.IsType<MarkupTagBlockSyntax>(childBlock);
            Assert.Empty(errorSink.Errors);

            // Act
            var pairs = parseTreeRewriter.GetAttributeNameValuePairs(tagBlock);

            // Assert
            Assert.Equal(expectedPairs, pairs);
        }

        public static TagHelperDescriptor[] PartialRequiredParentTags_Descriptors = new TagHelperDescriptor[]
        {
            TagHelperDescriptorBuilder.Create("StrongTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("div"))
                .Build(),
            TagHelperDescriptorBuilder.Create("CatchALlTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .Build(),
            TagHelperDescriptorBuilder.Create("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .Build(),
        };

        [Fact]
        public void UnderstandsPartialRequiredParentTags1()
        {
            var document = "<p><strong>";
            EvaluateData(PartialRequiredParentTags_Descriptors, document);
        }

        [Fact]
        public void UnderstandsPartialRequiredParentTags2()
        {
            var document = "<p><strong></strong>";
            EvaluateData(PartialRequiredParentTags_Descriptors, document);
        }

        [Fact]
        public void UnderstandsPartialRequiredParentTags3()
        {
            var document = "<p><strong></p><strong>";
            EvaluateData(PartialRequiredParentTags_Descriptors, document);
        }

        [Fact]
        public void UnderstandsPartialRequiredParentTags4()
        {
            var document = "<<p><<strong></</strong</strong></p>";
            EvaluateData(PartialRequiredParentTags_Descriptors, document);
        }

        [Fact]
        public void UnderstandsPartialRequiredParentTags5()
        {
            var document = "<<p><<strong></</strong></strong></p>";
            EvaluateData(PartialRequiredParentTags_Descriptors, document);
        }

        [Fact]
        public void UnderstandsPartialRequiredParentTags6()
        {
            var document = "<<p><<custom></<</custom></custom></p>";
            EvaluateData(PartialRequiredParentTags_Descriptors, document);
        }

        public static TagHelperDescriptor[] NestedVoidSelfClosingRequiredParent_Descriptors = new TagHelperDescriptor[]
        {
            TagHelperDescriptorBuilder.Create("InputTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("input")
                    .RequireTagStructure(TagStructure.WithoutEndTag))
                .Build(),
            TagHelperDescriptorBuilder.Create("StrongTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("strong")
                    .RequireParentTag("p"))
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("strong")
                    .RequireParentTag("input"))
                .Build(),
            TagHelperDescriptorBuilder.Create("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .Build(),
        };

        [Fact]
        public void UnderstandsNestedVoidSelfClosingRequiredParent1()
        {
            var document = "<input><strong></strong>";
            EvaluateData(NestedVoidSelfClosingRequiredParent_Descriptors, document);
        }

        [Fact]
        public void UnderstandsNestedVoidSelfClosingRequiredParent2()
        {
            var document = "<p><input><strong></strong></p>";
            EvaluateData(NestedVoidSelfClosingRequiredParent_Descriptors, document);
        }

        [Fact]
        public void UnderstandsNestedVoidSelfClosingRequiredParent3()
        {
            var document = "<p><br><strong></strong></p>";
            EvaluateData(NestedVoidSelfClosingRequiredParent_Descriptors, document);
        }

        [Fact]
        public void UnderstandsNestedVoidSelfClosingRequiredParent4()
        {
            var document = "<p><p><br></p><strong></strong></p>";
            EvaluateData(NestedVoidSelfClosingRequiredParent_Descriptors, document);
        }

        [Fact]
        public void UnderstandsNestedVoidSelfClosingRequiredParent5()
        {
            var document = "<input><strong></strong>";
            EvaluateData(NestedVoidSelfClosingRequiredParent_Descriptors, document);
        }

        [Fact]
        public void UnderstandsNestedVoidSelfClosingRequiredParent6()
        {
            var document = "<p><input /><strong /></p>";
            EvaluateData(NestedVoidSelfClosingRequiredParent_Descriptors, document);
        }

        [Fact]
        public void UnderstandsNestedVoidSelfClosingRequiredParent7()
        {
            var document = "<p><br /><strong /></p>";
            EvaluateData(NestedVoidSelfClosingRequiredParent_Descriptors, document);
        }

        [Fact]
        public void UnderstandsNestedVoidSelfClosingRequiredParent8()
        {
            var document = "<p><p><br /></p><strong /></p>";
            EvaluateData(NestedVoidSelfClosingRequiredParent_Descriptors, document);
        }

        public static TagHelperDescriptor[] NestedRequiredParent_Descriptors = new TagHelperDescriptor[]
        {
            TagHelperDescriptorBuilder.Create("StrongTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("strong")
                    .RequireParentTag("p"))
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("strong")
                    .RequireParentTag("div"))
                .Build(),
            TagHelperDescriptorBuilder.Create("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                .Build(),
        };

        [Fact]
        public void UnderstandsNestedRequiredParent1()
        {
            var document = "<strong></strong>";
            EvaluateData(NestedRequiredParent_Descriptors, document);
        }

        [Fact]
        public void UnderstandsNestedRequiredParent2()
        {
            var document = "<p><strong></strong></p>";
            EvaluateData(NestedRequiredParent_Descriptors, document);
        }

        [Fact]
        public void UnderstandsNestedRequiredParent3()
        {
            var document = "<div><strong></strong></div>";
            EvaluateData(NestedRequiredParent_Descriptors, document);
        }

        [Fact]
        public void UnderstandsNestedRequiredParent4()
        {
            var document = "<strong><strong></strong></strong>";
            EvaluateData(NestedRequiredParent_Descriptors, document);
        }

        [Fact]
        public void UnderstandsNestedRequiredParent5()
        {
            var document = "<p><strong><strong></strong></strong></p>";
            EvaluateData(NestedRequiredParent_Descriptors, document);
        }

        [Fact]
        public void UnderstandsTagHelperPrefixAndAllowedChildren()
        {
            // Arrange
            var documentContent = "<th:p><th:strong></th:strong></th:p>";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("PTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .AllowChildTag("strong")
                    .Build(),
                TagHelperDescriptorBuilder.Create("StrongTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
                    .Build(),
            };

            // Act & Assert
            EvaluateData(
                descriptors,
                documentContent,
                tagHelperPrefix: "th:");
        }

        [Fact]
        public void UnderstandsTagHelperPrefixAndAllowedChildrenAndRequireParent()
        {
            // Arrange
            var documentContent = "<th:p><th:strong></th:strong></th:p>";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("PTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .AllowChildTag("strong")
                    .Build(),
                TagHelperDescriptorBuilder.Create("StrongTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong").RequireParentTag("p"))
                    .Build(),
            };

            // Act & Assert
            EvaluateData(
                descriptors,
                documentContent,
                tagHelperPrefix: "th:");
        }

        [Fact]
        public void InvalidStructure_UnderstandsTHPrefixAndAllowedChildrenAndRequireParent()
        {
            // Rewrite_InvalidStructure_UnderstandsTagHelperPrefixAndAllowedChildrenAndRequireParent
            // Arrange
            var documentContent = "<th:p></th:strong></th:p>";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("PTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .AllowChildTag("strong")
                    .Build(),
                TagHelperDescriptorBuilder.Create("StrongTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong").RequireParentTag("p"))
                    .Build(),
            };

            // Act & Assert
            EvaluateData(
                descriptors,
                documentContent,
                tagHelperPrefix: "th:");
        }

        [Fact]
        public void NonTagHelperChild_UnderstandsTagHelperPrefixAndAllowedChildren()
        {
            // Arrange
            var documentContent = "<th:p><strong></strong></th:p>";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("PTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .AllowChildTag("strong")
                    .Build(),
            };

            // Act & Assert
            EvaluateData(
                descriptors,
                documentContent,
                tagHelperPrefix: "th:");
        }

        [Fact]
        public void DoesNotUnderstandTagHelpersInInvalidHtmlTypedScriptTags1()
        {
            var document = "<script type><input /></script>";
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void DoesNotUnderstandTagHelpersInInvalidHtmlTypedScriptTags2()
        {
            var document = "<script types='text/html'><input /></script>";
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void DoesNotUnderstandTagHelpersInInvalidHtmlTypedScriptTags3()
        {
            var document = "<script type='text/html invalid'><input /></script>";
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void DoesNotUnderstandTagHelpersInInvalidHtmlTypedScriptTags4()
        {
            var document = "<script type='text/ng-*' type='text/html'><input /></script>";
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void UnderstandsTagHelpersInHtmlTypedScriptTags1()
        {
            var document = "<script type='text/html'><input /></script>";
            RunParseTreeRewriterTest(document, "p", "input");
        }

        [Fact]
        public void UnderstandsTagHelpersInHtmlTypedScriptTags2()
        {
            var document = "<script id='scriptTag' type='text/html' class='something'><input /></script>";
            RunParseTreeRewriterTest(document, "p", "input");
        }

        [Fact]
        public void UnderstandsTagHelpersInHtmlTypedScriptTags3()
        {
            var document = "<script type='text/html'><p><script type='text/html'><input /></script></p></script>";
            RunParseTreeRewriterTest(document, "p", "input");
        }

        [Fact]
        public void UnderstandsTagHelpersInHtmlTypedScriptTags4()
        {
            var document = "<script type='text/html'><p><script type='text/ html'><input /></script></p></script>";
            RunParseTreeRewriterTest(document, "p", "input");
        }

        [Fact]
        public void CanHandleInvalidChildrenWithWhitespace()
        {
            // Arrange
            var documentContent = $"<p>{Environment.NewLine}    <strong>{Environment.NewLine}        Hello" +
                $"{Environment.NewLine}    </strong>{Environment.NewLine}</p>";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("PTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .AllowChildTag("br")
                    .Build()
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void RecoversWhenRequiredAttributeMismatchAndRestrictedChildren()
        {
            // Arrange
            var documentContent = "<strong required><strong></strong></strong>";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("StrongTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("strong")
                        .RequireAttributeDescriptor(attribute => attribute.Name("required")))
                    .AllowChildTag("br")
                    .Build()
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void CanHandleMultipleTagHelpersWithAllowedChildren_OneNull()
        {
            // Arrange
            var documentContent = "<p><strong>Hello World</strong><br></p>";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("PTagHelper1", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .AllowChildTag("strong")
                    .AllowChildTag("br")
                    .Build(),
                TagHelperDescriptorBuilder.Create("PTagHelper2", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .Build(),
                TagHelperDescriptorBuilder.Create("StrongTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
                    .Build(),
                TagHelperDescriptorBuilder.Create("BRTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("br")
                        .RequireTagStructure(TagStructure.WithoutEndTag))
                    .Build(),
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void CanHandleMultipleTagHelpersWithAllowedChildren()
        {
            // Arrange
            var documentContent = "<p><strong>Hello World</strong><br></p>";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("PTagHelper1", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .AllowChildTag("strong")
                    .Build(),
                TagHelperDescriptorBuilder.Create("PTagHelper2", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .AllowChildTag("br")
                    .Build(),
                TagHelperDescriptorBuilder.Create("StrongTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"))
                    .Build(),
                TagHelperDescriptorBuilder.Create("BRTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("br")
                        .RequireTagStructure(TagStructure.WithoutEndTag))
                    .Build(),
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void UnderstandsAllowedChildren1()
        {
            // Arrange
            var documentContent = "<p><br /></p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "br" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void UnderstandsAllowedChildren2()
        {
            // Arrange
            var documentContent = $"<p>{Environment.NewLine}<br />{Environment.NewLine}</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "br" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void UnderstandsAllowedChildren3()
        {
            // Arrange
            var documentContent = "<p><br></p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "strong" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void UnderstandsAllowedChildren4()
        {
            // Arrange
            var documentContent = "<p>Hello</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "strong" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void UnderstandsAllowedChildren5()
        {
            // Arrange
            var documentContent = "<p><hr /></p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "br", "strong" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void UnderstandsAllowedChildren6()
        {
            // Arrange
            var documentContent = "<p><br>Hello</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "strong" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void UnderstandsAllowedChildren7()
        {
            // Arrange
            var documentContent = "<p><strong>Title:</strong><br />Something</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "strong" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void UnderstandsAllowedChildren8()
        {
            // Arrange
            var documentContent = "<p><strong>Title:</strong><br />Something</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "strong", "br" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void UnderstandsAllowedChildren9()
        {
            // Arrange
            var documentContent = "<p>  <strong>Title:</strong>  <br />  Something</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "strong", "br" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void UnderstandsAllowedChildren10()
        {
            // Arrange
            var documentContent = "<p><strong>Title:<br><em>A Very Cool</em></strong><br />Something</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "strong" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void UnderstandsAllowedChildren11()
        {
            // Arrange
            var documentContent = "<p><custom>Title:<br><em>A Very Cool</em></custom><br />Something</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "custom" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void UnderstandsAllowedChildren12()
        {
            // Arrange
            var documentContent = "<p></</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "custom" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void UnderstandsAllowedChildren13()
        {
            // Arrange
            var documentContent = "<p><</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "custom" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void UnderstandsAllowedChildren14()
        {
            // Arrange
            var documentContent = "<p><custom><br>:<strong><strong>Hello</strong></strong>:<input></custom></p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "custom", "strong" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        private TagHelperDescriptor[] GetAllowedChildrenTagHelperDescriptors(string[] allowedChildren)
        {
            var pTagHelperBuilder = TagHelperDescriptorBuilder.Create("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"));
            var strongTagHelperBuilder = TagHelperDescriptorBuilder.Create("StrongTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("strong"));

            foreach (var childTag in allowedChildren)
            {
                pTagHelperBuilder.AllowChildTag(childTag);
                strongTagHelperBuilder.AllowChildTag(childTag);
            }
            var descriptors = new TagHelperDescriptor[]
            {
                pTagHelperBuilder.Build(),
                strongTagHelperBuilder.Build(),
                TagHelperDescriptorBuilder.Create("BRTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("br")
                        .RequireTagStructure(TagStructure.WithoutEndTag))
                    .Build(),
            };

            return descriptors;
        }

        [Fact]
        public void AllowsSimpleHtmlCommentsAsChildren()
        {
            // Arrange
            var allowedChildren = new List<string> { "b" };
            var literal = "asdf";
            var commentOutput = "Hello World";
            var document = $"<p><b>{literal}</b><!--{commentOutput}--></p>";

            var pTagHelperBuilder = TagHelperDescriptorBuilder
                .Create("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"));
            foreach (var childTag in allowedChildren)
            {
                pTagHelperBuilder.AllowChildTag(childTag);
            }

            var descriptors = new TagHelperDescriptor[]
            {
                pTagHelperBuilder.Build()
            };

            // Act & Assert
            EvaluateData(descriptors, document);
        }

        [Fact]
        public void DoesntAllowSimpleHtmlCommentsAsChildrenWhenFeatureFlagIsOff()
        {
            // Arrange
            var allowedChildren = new List<string> { "b" };
            var comment1 = "Hello";
            var document = $"<p><!--{comment1}--></p>";

            var pTagHelperBuilder = TagHelperDescriptorBuilder
                .Create("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"));
            foreach (var childTag in allowedChildren)
            {
                pTagHelperBuilder.AllowChildTag(childTag);
            }

            var descriptors = new TagHelperDescriptor[]
            {
                pTagHelperBuilder.Build()
            };

            // Act & Assert
            EvaluateData(
                descriptors,
                document,
                featureFlags: RazorParserFeatureFlags.Create(RazorLanguageVersion.Version_2_0));
        }

        [Fact]
        public void FailsForContentWithCommentsAsChildren()
        {
            // Arrange
            var allowedChildren = new List<string> { "b" };
            var comment1 = "Hello";
            var literal = "asdf";
            var comment2 = "World";
            var document = $"<p><!--{comment1}-->{literal}<!--{comment2}--></p>";

            var pTagHelperBuilder = TagHelperDescriptorBuilder
                .Create("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"));
            foreach (var childTag in allowedChildren)
            {
                pTagHelperBuilder.AllowChildTag(childTag);
            }

            var descriptors = new TagHelperDescriptor[]
            {
                pTagHelperBuilder.Build()
            };

            // Act & Assert
            EvaluateData(descriptors, document);
        }

        [Fact]
        public void AllowsRazorCommentsAsChildren()
        {
            // Arrange
            var allowedChildren = new List<string> { "b" };
            var literal = "asdf";
            var commentOutput = $"@*{literal}*@";
            var document = $"<p><b>{literal}</b>{commentOutput}</p>";

            var pTagHelperBuilder = TagHelperDescriptorBuilder
                .Create("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"));
            foreach (var childTag in allowedChildren)
            {
                pTagHelperBuilder.AllowChildTag(childTag);
            }

            var descriptors = new TagHelperDescriptor[]
            {
                pTagHelperBuilder.Build()
            };

            // Act & Assert
            EvaluateData(descriptors, document);
        }

        [Fact]
        public void AllowsRazorMarkupInHtmlComment()
        {
            // Arrange
            var allowedChildren = new List<string> { "b" };
            var literal = "asdf";
            var part1 = "Hello ";
            var part2 = "World";
            var commentStart = "<!--";
            var commentEnd = "-->";
            var document = $"<p><b>{literal}</b>{commentStart}{part1}@{part2}{commentEnd}</p>";

            var pTagHelperBuilder = TagHelperDescriptorBuilder
                .Create("PTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"));
            foreach (var childTag in allowedChildren)
            {
                pTagHelperBuilder.AllowChildTag(childTag);
            }

            var descriptors = new TagHelperDescriptor[]
            {
                pTagHelperBuilder.Build()
            };

            // Act & Assert
            EvaluateData(descriptors, document);
        }

        [Fact]
        public void UnderstandsNullTagNameWithAllowedChildrenForCatchAll()
        {
            // Arrange
            var documentContent = "<p></</p>";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("PTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .AllowChildTag("custom")
                    .Build(),
                TagHelperDescriptorBuilder.Create("CatchAllTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                    .Build(),
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void UnderstandsNullTagNameWithAllowedChildrenForCatchAllWithPrefix()
        {
            // Arrange
            var documentContent = "<th:p></</th:p>";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("PTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .AllowChildTag("custom")
                    .Build(),
                TagHelperDescriptorBuilder.Create("CatchAllTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                    .Build(),
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent, "th:");
        }

        [Fact]
        public void CanHandleStartTagOnlyTagTagMode()
        {
            // Arrange
            var documentContent = "<input>";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("InputTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireTagStructure(TagStructure.WithoutEndTag))
                    .Build()
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void CreatesErrorForWithoutEndTagTagStructureForEndTags()
        {
            // Arrange
            var documentContent = "</input>";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("InputTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireTagStructure(TagStructure.WithoutEndTag))
                    .Build()
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void CreatesErrorForInconsistentTagStructures()
        {
            // Arrange
            var documentContent = "<input>";
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("InputTagHelper1", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireTagStructure(TagStructure.WithoutEndTag))
                    .Build(),
                TagHelperDescriptorBuilder.Create("InputTagHelper2", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("input")
                        .RequireTagStructure(TagStructure.NormalOrSelfClosing))
                    .Build()
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        public static TagHelperDescriptor[] RequiredAttribute_Descriptors = new TagHelperDescriptor[]
        {
            TagHelperDescriptorBuilder.Create("pTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("p")
                    .RequireAttributeDescriptor(attribute => attribute.Name("class")))
                .Build(),
            TagHelperDescriptorBuilder.Create("divTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("div")
                    .RequireAttributeDescriptor(attribute => attribute.Name("class"))
                    .RequireAttributeDescriptor(attribute => attribute.Name("style")))
                .Build(),
            TagHelperDescriptorBuilder.Create("catchAllTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("*")
                    .RequireAttributeDescriptor(attribute => attribute.Name("catchAll")))
                .Build()
        };

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly1()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p />");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly2()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p></p>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly3()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div />");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly4()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div></div>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly5()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p class=\"btn\" />");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly6()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p class=\"@DateTime.Now\" />");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly7()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p class=\"btn\">words and spaces</p>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly8()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p class=\"@DateTime.Now\">words and spaces</p>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly9()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p class=\"btn\">words<strong>and</strong>spaces</p>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly10()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<strong catchAll=\"hi\" />");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly11()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<strong catchAll=\"@DateTime.Now\" />");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly12()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<strong catchAll=\"hi\">words and spaces</strong>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly13()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<strong catchAll=\"@DateTime.Now\">words and spaces</strong>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly14()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div class=\"btn\" />");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly15()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div class=\"btn\"></div>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly16()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p notRequired=\"a\" class=\"btn\" />");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly17()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p notRequired=\"@DateTime.Now\" class=\"btn\" />");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly18()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p notRequired=\"a\" class=\"btn\">words and spaces</p>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly19()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"\" class=\"btn\" />");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly20()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"@DateTime.Now\" class=\"btn\" />");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly21()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"\" class=\"btn\">words and spaces</div>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly22()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"@DateTime.Now\" class=\"@DateTime.Now\">words and spaces</div>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly23()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"\" class=\"btn\">words<strong>and</strong>spaces</div>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly24()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p class=\"btn\" catchAll=\"hi\" />");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly25()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p class=\"btn\" catchAll=\"hi\">words and spaces</p>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly26()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"\" class=\"btn\" catchAll=\"hi\" />");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly27()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"\" class=\"btn\" catchAll=\"hi\" >words and spaces</div>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly28()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"\" class=\"btn\" catchAll=\"@@hi\" >words and spaces</div>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly29()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"@DateTime.Now\" class=\"@DateTime.Now\" catchAll=\"@DateTime.Now\" >words and spaces</div>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly30()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"\" class=\"btn\" catchAll=\"hi\" >words<strong>and</strong>spaces</div>");
        }

        public static TagHelperDescriptor[] NestedRequiredAttribute_Descriptors = new TagHelperDescriptor[]
        {
            TagHelperDescriptorBuilder.Create("pTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("p")
                    .RequireAttributeDescriptor(attribute => attribute.Name("class")))
                .Build(),
            TagHelperDescriptorBuilder.Create("catchAllTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("*")
                    .RequireAttributeDescriptor(attribute => attribute.Name("catchAll")))
                .Build(),
        };

        [Fact]
        public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly1()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<p class=\"btn\"><p></p></p>");
        }

        [Fact]
        public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly2()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<strong catchAll=\"hi\"><strong></strong></strong>");
        }

        [Fact]
        public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly3()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<p class=\"btn\"><strong><p></p></strong></p>");
        }

        [Fact]
        public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly4()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<strong catchAll=\"hi\"><p><strong></strong></p></strong>");
        }

        [Fact]
        public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly5()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<p class=\"btn\"><strong catchAll=\"hi\"><p></p></strong></p>");
        }

        [Fact]
        public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly6()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<strong catchAll=\"hi\"><p class=\"btn\"><strong></strong></p></strong>");
        }

        [Fact]
        public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly7()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<p class=\"btn\"><p class=\"btn\"><p></p></p></p>");
        }

        [Fact]
        public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly8()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<strong catchAll=\"hi\"><strong catchAll=\"hi\"><strong></strong></strong></strong>");
        }

        [Fact]
        public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly9()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<p class=\"btn\"><p><p><p class=\"btn\"><p></p></p></p></p></p>");
        }

        [Fact]
        public void NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly10()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<strong catchAll=\"hi\"><strong><strong><strong catchAll=\"hi\"><strong></strong></strong></strong></strong></strong>");
        }

        public static TagHelperDescriptor[] MalformedRequiredAttribute_Descriptors = new TagHelperDescriptor[]
        {
            TagHelperDescriptorBuilder.Create("pTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule =>
                    rule
                    .RequireTagName("p")
                    .RequireAttributeDescriptor(attribute => attribute.Name("class")))
                .Build(),
        };

        [Fact]
        public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly1()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly2()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p class=\"btn\"");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly3()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p notRequired=\"hi\" class=\"btn\"");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly4()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p></p");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly5()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p class=\"btn\"></p");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly6()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p notRequired=\"hi\" class=\"btn\"></p");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly7()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p class=\"btn\" <p>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly8()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p notRequired=\"hi\" class=\"btn\" <p>");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly9()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p class=\"btn\" </p");
        }

        [Fact]
        public void RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly10()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p notRequired=\"hi\" class=\"btn\" </p");
        }

        public static TagHelperDescriptor[] PrefixedTagHelperColon_Descriptors = new TagHelperDescriptor[]
        {
            TagHelperDescriptorBuilder.Create("mythTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("myth"))
                .Build(),
            TagHelperDescriptorBuilder.Create("mythTagHelper2", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("myth2"))
                .BoundAttributeDescriptor(attribute =>
                    attribute
                    .Name("bound")
                    .PropertyName("Bound")
                    .TypeName(typeof(bool).FullName))
                .Build()
        };

        public static TagHelperDescriptor[] PrefixedTagHelperCatchAll_Descriptors = new TagHelperDescriptor[]
        {
            TagHelperDescriptorBuilder.Create("mythTagHelper", "SomeAssembly")
                .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                .Build(),
        };

        [Fact]
        public void AllowsPrefixedTagHelpers1()
        {
            EvaluateData(PrefixedTagHelperCatchAll_Descriptors, "<th: />", tagHelperPrefix: "th:");
        }

        [Fact]
        public void AllowsPrefixedTagHelpers2()
        {
            EvaluateData(PrefixedTagHelperCatchAll_Descriptors, "<th:>words and spaces</th:>", tagHelperPrefix: "th:");
        }

        [Fact]
        public void AllowsPrefixedTagHelpers3()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<th:myth />", tagHelperPrefix: "th:");
        }

        [Fact]
        public void AllowsPrefixedTagHelpers4()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<th:myth></th:myth>", tagHelperPrefix: "th:");
        }

        [Fact]
        public void AllowsPrefixedTagHelpers5()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<th:myth><th:my2th></th:my2th></th:myth>", tagHelperPrefix: "th:");
        }

        [Fact]
        public void AllowsPrefixedTagHelpers6()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<!th:myth />", tagHelperPrefix: "th:");
        }

        [Fact]
        public void AllowsPrefixedTagHelpers7()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<!th:myth></!th:myth>", tagHelperPrefix: "th:");
        }

        [Fact]
        public void AllowsPrefixedTagHelpers8()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<th:myth class=\"btn\" />", tagHelperPrefix: "th:");
        }

        [Fact]
        public void AllowsPrefixedTagHelpers9()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<th:myth2 class=\"btn\" />", tagHelperPrefix: "th:");
        }

        [Fact]
        public void AllowsPrefixedTagHelpers10()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<th:myth class=\"btn\">words and spaces</th:myth>", tagHelperPrefix: "th:");
        }

        [Fact]
        public void AllowsPrefixedTagHelpers11()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<th:myth2 bound=\"@DateTime.Now\" />", tagHelperPrefix: "th:");
        }

        [Fact]
        public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithAttrTextTag1()
        {
            RunParseTreeRewriterTest("@{<!text class=\"btn\">}", "p", "text");
        }

        [Fact]
        public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithAttrTextTag2()
        {
            RunParseTreeRewriterTest("@{<!text class=\"btn\"></!text>}", "p", "text");
        }

        [Fact]
        public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithAttrTextTag3()
        {
            RunParseTreeRewriterTest("@{<!text class=\"btn\">words with spaces</!text>}", "p", "text");
        }

        [Fact]
        public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithAttrTextTag4()
        {
            RunParseTreeRewriterTest("@{<!text class='btn1 btn2' class2=btn></!text>}", "p", "text");
        }

        [Fact]
        public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithAttrTextTag5()
        {
            RunParseTreeRewriterTest("@{<!text class='btn1 @DateTime.Now btn2'></!text>}", "p", "text");
        }

        [Fact]
        public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag1()
        {
            RunParseTreeRewriterTest("@{<!text>}", "p", "text");
        }

        [Fact]
        public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag2()
        {
            RunParseTreeRewriterTest("@{</!text>}", "p", "text");
        }

        [Fact]
        public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag3()
        {
            RunParseTreeRewriterTest("@{<!text></!text>}", "p", "text");
        }

        [Fact]
        public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag4()
        {
            RunParseTreeRewriterTest("@{<!text>words and spaces</!text>}", "p", "text");
        }

        [Fact]
        public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag5()
        {
            RunParseTreeRewriterTest("@{<!text></text>}", "p", "text");
        }

        [Fact]
        public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag6()
        {
            RunParseTreeRewriterTest("@{<text></!text>}", "p", "text");
        }

        [Fact]
        public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag7()
        {
            RunParseTreeRewriterTest("@{<!text><text></text></!text>}", "p", "text");
        }

        [Fact]
        public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag8()
        {
            RunParseTreeRewriterTest("@{<text><!text></!text>}", "p", "text");
        }

        [Fact]
        public void AllowsTHElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag9()
        {
            RunParseTreeRewriterTest("@{<!text></!text></text>}", "p", "text");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock1()
        {
            RunParseTreeRewriterTest("@{<!text}", "text");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock2()
        {
            RunParseTreeRewriterTest("@{<!text /}", "text");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock3()
        {
            RunParseTreeRewriterTest("@{<!text class=}", "text");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock4()
        {
            RunParseTreeRewriterTest("@{<!text class=\"btn}", "text");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock5()
        {
            RunParseTreeRewriterTest("@{<!text class=\"btn\"}", "text");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock6()
        {
            RunParseTreeRewriterTest("@{<!text class=\"btn\" /}", "text");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock1()
        {
            RunParseTreeRewriterTest("@{<!}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock2()
        {
            RunParseTreeRewriterTest("@{<!p}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock3()
        {
            RunParseTreeRewriterTest("@{<!p /}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock4()
        {
            RunParseTreeRewriterTest("@{<!p class=}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock5()
        {
            RunParseTreeRewriterTest("@{<!p class=\"btn}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock6()
        {
            RunParseTreeRewriterTest("@{<!p class=\"btn@@}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock7()
        {
            RunParseTreeRewriterTest("@{<!p class=\"btn\"}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock8()
        {
            RunParseTreeRewriterTest("@{<!p class=\"btn\" /}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteHTML1()
        {
            RunParseTreeRewriterTest("<!", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteHTML2()
        {
            RunParseTreeRewriterTest("<!p", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteHTML3()
        {
            RunParseTreeRewriterTest("<!p /", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteHTML4()
        {
            RunParseTreeRewriterTest("<!p class=", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteHTML5()
        {
            RunParseTreeRewriterTest("<!p class=\"btn", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteHTML6()
        {
            RunParseTreeRewriterTest("<!p class=\"btn\"", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptForIncompleteHTML7()
        {
            RunParseTreeRewriterTest("<!p class=\"btn\" /", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithBlockData1()
        {
            RunParseTreeRewriterTest("@{<!p>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithBlockData2()
        {
            RunParseTreeRewriterTest("@{</!p>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithBlockData3()
        {
            RunParseTreeRewriterTest("@{<!p></!p>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithBlockData4()
        {
            RunParseTreeRewriterTest("@{<!p>words and spaces</!p>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithBlockData5()
        {
            RunParseTreeRewriterTest("@{<!p></p>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithBlockData6()
        {
            RunParseTreeRewriterTest("@{<p></!p>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithBlockData7()
        {
            RunParseTreeRewriterTest("@{<p><!p></!p></p>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithBlockData8()
        {
            RunParseTreeRewriterTest("@{<p><!p></!p>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithBlockData9()
        {
            RunParseTreeRewriterTest("@{<!p></!p></p>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithBlockData10()
        {
            RunParseTreeRewriterTest("@{<strong></!p></strong>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithBlockData11()
        {
            RunParseTreeRewriterTest("@{<strong></strong><!p></!p>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithBlockData12()
        {
            RunParseTreeRewriterTest("@{<p><strong></!strong><!p></strong></!p>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithAttributeData1()
        {
            RunParseTreeRewriterTest("@{<!p class=\"btn\">}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithAttributeData2()
        {
            RunParseTreeRewriterTest("@{<!p class=\"btn\"></!p>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithAttributeData3()
        {
            RunParseTreeRewriterTest("@{<!p class=\"btn\">words with spaces</!p>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithAttributeData4()
        {
            RunParseTreeRewriterTest("@{<!p class='btn1 btn2' class2=btn></!p>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutCSharp_WithAttributeData5()
        {
            RunParseTreeRewriterTest("@{<!p class='btn1 @DateTime.Now btn2'></!p>}", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithBlockData1()
        {
            RunParseTreeRewriterTest("<!p>", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithBlockData2()
        {
            RunParseTreeRewriterTest("</!p>", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithBlockData3()
        {
            RunParseTreeRewriterTest("<!p></!p>", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithBlockData4()
        {
            RunParseTreeRewriterTest("<!p>words and spaces</!p>", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithBlockData5()
        {
            RunParseTreeRewriterTest("<!p></p>", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithBlockData6()
        {
            RunParseTreeRewriterTest("<p></!p>", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithBlockData7()
        {
            RunParseTreeRewriterTest("<p><!p></!p></p>", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithBlockData8()
        {
            RunParseTreeRewriterTest("<p><!p></!p>", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithBlockData9()
        {
            RunParseTreeRewriterTest("<!p></!p></p>", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithBlockData10()
        {
            RunParseTreeRewriterTest("<strong></!p></strong>", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithBlockData11()
        {
            RunParseTreeRewriterTest("<strong></strong><!p></!p>", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithBlockData12()
        {
            RunParseTreeRewriterTest("<p><strong></!strong><!p></strong></!p>", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithAttributeData1()
        {
            RunParseTreeRewriterTest("<!p class=\"btn\">", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithAttributeData2()
        {
            RunParseTreeRewriterTest("<!p class=\"btn\"></!p>", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithAttributeData3()
        {
            RunParseTreeRewriterTest("<!p class=\"btn\">words and spaces</!p>", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithAttributeData4()
        {
            RunParseTreeRewriterTest("<!p class='btn1 btn2' class2=btn></!p>", "strong", "p");
        }

        [Fact]
        public void AllowsTagHelperElementOptOutHTML_WithAttributeData5()
        {
            RunParseTreeRewriterTest("<!p class='btn1 @DateTime.Now btn2'></!p>", "strong", "p");
        }

        [Fact]
        public void DoesNotRewriteTextTagTransitionTagHelpers1()
        {
            RunParseTreeRewriterTest("<text>Hello World</text>", "p", "text");
        }

        [Fact]
        public void DoesNotRewriteTextTagTransitionTagHelpers2()
        {
            RunParseTreeRewriterTest("@{<text>Hello World</text>}", "p", "text");
        }

        [Fact]
        public void DoesNotRewriteTextTagTransitionTagHelpers3()
        {
            RunParseTreeRewriterTest("@{<text><p>Hello World</p></text>}", "p", "text");
        }

        [Fact]
        public void DoesNotRewriteTextTagTransitionTagHelpers4()
        {
            RunParseTreeRewriterTest("@{<p><text>Hello World</text></p>}", "p", "text");
        }

        [Fact]
        public void DoesNotRewriteSpecialTagTagHelpers1()
        {
            RunParseTreeRewriterTest("<foo><!-- Hello World --></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void DoesNotRewriteSpecialTagTagHelpers2()
        {
            RunParseTreeRewriterTest("<foo><!-- @foo --></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void DoesNotRewriteSpecialTagTagHelpers3()
        {
            RunParseTreeRewriterTest("<foo><?xml Hello World ?></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void DoesNotRewriteSpecialTagTagHelpers4()
        {
            RunParseTreeRewriterTest("<foo><?xml @foo ?></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void DoesNotRewriteSpecialTagTagHelpers5()
        {
            RunParseTreeRewriterTest("<foo><!DOCTYPE @foo ></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void DoesNotRewriteSpecialTagTagHelpers6()
        {
            RunParseTreeRewriterTest("<foo><!DOCTYPE hello=\"world\" ></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void DoesNotRewriteSpecialTagTagHelpers7()
        {
            RunParseTreeRewriterTest("<foo><![CDATA[ Hello World ]]></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void DoesNotRewriteSpecialTagTagHelpers8()
        {
            RunParseTreeRewriterTest("<foo><![CDATA[ @foo ]]></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void RewritesNestedTagHelperTagBlocks1()
        {
            RunParseTreeRewriterTest("<p><div></div></p>", "p", "div");
        }

        [Fact]
        public void RewritesNestedTagHelperTagBlocks2()
        {
            RunParseTreeRewriterTest("<p>Hello World <div></div></p>", "p", "div");
        }

        [Fact]
        public void RewritesNestedTagHelperTagBlocks3()
        {
            RunParseTreeRewriterTest("<p>Hel<p>lo</p></p> <p><div>World</div></p>", "p", "div");
        }

        [Fact]
        public void RewritesNestedTagHelperTagBlocks4()
        {
            RunParseTreeRewriterTest("<p>Hel<strong>lo</strong></p> <p><span>World</span></p>", "p", "div");
        }

        [Fact]
        public void HandlesMalformedNestedNonTagHelperTags_Correctly()
        {
            RunParseTreeRewriterTest("<div>@{</div>}");
        }
    }
}