// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public class TagHelperParseTreeRewriterTest : TagHelperRewritingTestBase
    {
        public static TheoryData GetAttributeNameValuePairsData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);
                Func<string, string, KeyValuePair<string, string>> kvp =
                    (key, value) => new KeyValuePair<string, string>(key, value);
                var empty = Enumerable.Empty<KeyValuePair<string, string>>();
                var csharp = TagHelperParseTreeRewriter.InvalidAttributeValueMarker;

                // documentContent, expectedPairs
                return new TheoryData<string, IEnumerable<KeyValuePair<string, string>>>
                {
                    { "<a>", empty },
                    { "<a @{ } href='~/home'>", empty },
                    { "<a href=\"@true\">", new[] { kvp("href", csharp) } },
                    { "<a href=\"prefix @true suffix\">", new[] { kvp("href", $"prefix{csharp} suffix") } },
                    { "<a href=~/home>", new[] { kvp("href", "~/home") } },
                    { "<a href=~/home @{ } nothing='something'>", new[] { kvp("href", "~/home"), kvp("", "") } },
                    {
                        "<a href=\"@DateTime.Now::0\" class='btn btn-success' random>",
                        new[] { kvp("href", $"{csharp}::0"), kvp("class", "btn btn-success"), kvp("random", "") }
                    },
                    { "<a href=>", new[] { kvp("href", "") } },
                    { "<a href='\">  ", new[] { kvp("href", "\">") } },
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
            var parseTreeRewriter = new TagHelperParseTreeRewriter(null, Enumerable.Empty<TagHelperDescriptor>(), parseResult.Options.FeatureFlags);

            // Assert - Guard
            var rootBlock = Assert.IsType<Block>(document);
            var child = Assert.Single(rootBlock.Children);
            var tagBlock = Assert.IsType<Block>(child);
            Assert.Equal(BlockKindInternal.Tag, tagBlock.Type);
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
        public void Rewrite_UnderstandsPartialRequiredParentTags1()
        {
            var document = "<p><strong>";
            EvaluateData(PartialRequiredParentTags_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsPartialRequiredParentTags2()
        {
            var document = "<p><strong></strong>";
            EvaluateData(PartialRequiredParentTags_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsPartialRequiredParentTags3()
        {
            var document = "<p><strong></p><strong>";
            EvaluateData(PartialRequiredParentTags_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsPartialRequiredParentTags4()
        {
            var document = "<<p><<strong></</strong</strong></p>";
            EvaluateData(PartialRequiredParentTags_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsPartialRequiredParentTags5()
        {
            var document = "<<p><<strong></</strong></strong></p>";
            EvaluateData(PartialRequiredParentTags_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsPartialRequiredParentTags6()
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
        public void Rewrite_UnderstandsNestedVoidSelfClosingRequiredParent1()
        {
            var document = "<input><strong></strong>";
            EvaluateData(NestedVoidSelfClosingRequiredParent_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsNestedVoidSelfClosingRequiredParent2()
        {
            var document = "<p><input><strong></strong></p>";
            EvaluateData(NestedVoidSelfClosingRequiredParent_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsNestedVoidSelfClosingRequiredParent3()
        {
            var document = "<p><br><strong></strong></p>";
            EvaluateData(NestedVoidSelfClosingRequiredParent_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsNestedVoidSelfClosingRequiredParent4()
        {
            var document = "<p><p><br></p><strong></strong></p>";
            EvaluateData(NestedVoidSelfClosingRequiredParent_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsNestedVoidSelfClosingRequiredParent5()
        {
            var document = "<input><strong></strong>";
            EvaluateData(NestedVoidSelfClosingRequiredParent_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsNestedVoidSelfClosingRequiredParent6()
        {
            var document = "<p><input /><strong /></p>";
            EvaluateData(NestedVoidSelfClosingRequiredParent_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsNestedVoidSelfClosingRequiredParent7()
        {
            var document = "<p><br /><strong /></p>";
            EvaluateData(NestedVoidSelfClosingRequiredParent_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsNestedVoidSelfClosingRequiredParent8()
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
        public void Rewrite_UnderstandsNestedRequiredParent1()
        {
            var document = "<strong></strong>";
            EvaluateData(NestedRequiredParent_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsNestedRequiredParent2()
        {
            var document = "<p><strong></strong></p>";
            EvaluateData(NestedRequiredParent_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsNestedRequiredParent3()
        {
            var document = "<div><strong></strong></div>";
            EvaluateData(NestedRequiredParent_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsNestedRequiredParent4()
        {
            var document = "<strong><strong></strong></strong>";
            EvaluateData(NestedRequiredParent_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsNestedRequiredParent5()
        {
            var document = "<p><strong><strong></strong></strong></p>";
            EvaluateData(NestedRequiredParent_Descriptors, document);
        }

        [Fact]
        public void Rewrite_UnderstandsTagHelperPrefixAndAllowedChildren()
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
        public void Rewrite_UnderstandsTagHelperPrefixAndAllowedChildrenAndRequireParent()
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
        public void Rewrite_InvalidStructure_UnderstandsTagHelperPrefixAndAllowedChildrenAndRequireParent()
        {
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
        public void Rewrite_NonTagHelperChild_UnderstandsTagHelperPrefixAndAllowedChildren()
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
        public void TagHelperParseTreeRewriter_DoesNotUnderstandTagHelpersInInvalidHtmlTypedScriptTags1()
        {
            var document = "<script type><input /></script>";
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_DoesNotUnderstandTagHelpersInInvalidHtmlTypedScriptTags2()
        {
            var document = "<script types='text/html'><input /></script>";
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_DoesNotUnderstandTagHelpersInInvalidHtmlTypedScriptTags3()
        {
            var document = "<script type='text/html invalid'><input /></script>";
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_DoesNotUnderstandTagHelpersInInvalidHtmlTypedScriptTags4()
        {
            var document = "<script type='text/ng-*' type='text/html'><input /></script>";
            RunParseTreeRewriterTest(document, "input");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_UnderstandsTagHelpersInHtmlTypedScriptTags1()
        {
            var document = "<script type='text/html'><input /></script>";
            RunParseTreeRewriterTest(document, "p", "input");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_UnderstandsTagHelpersInHtmlTypedScriptTags2()
        {
            var document = "<script id='scriptTag' type='text/html' class='something'><input /></script>";
            RunParseTreeRewriterTest(document, "p", "input");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_UnderstandsTagHelpersInHtmlTypedScriptTags3()
        {
            var document = "<script type='text/html'><p><script type='text/html'><input /></script></p></script>";
            RunParseTreeRewriterTest(document, "p", "input");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_UnderstandsTagHelpersInHtmlTypedScriptTags4()
        {
            var document = "<script type='text/html'><p><script type='text/ html'><input /></script></p></script>";
            RunParseTreeRewriterTest(document, "p", "input");
        }

        [Fact]
        public void Rewrite_CanHandleInvalidChildrenWithWhitespace()
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
        public void Rewrite_RecoversWhenRequiredAttributeMismatchAndRestrictedChildren()
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
        public void Rewrite_CanHandleMultipleTagHelpersWithAllowedChildren_OneNull()
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
        public void Rewrite_CanHandleMultipleTagHelpersWithAllowedChildren()
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
        public void Rewrite_UnderstandsAllowedChildren1()
        {
            // Arrange
            var documentContent = "<p><br /></p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "br" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void Rewrite_UnderstandsAllowedChildren2()
        {
            // Arrange
            var documentContent = $"<p>{Environment.NewLine}<br />{Environment.NewLine}</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "br" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void Rewrite_UnderstandsAllowedChildren3()
        {
            // Arrange
            var documentContent = "<p><br></p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "strong" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void Rewrite_UnderstandsAllowedChildren4()
        {
            // Arrange
            var documentContent = "<p>Hello</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "strong" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void Rewrite_UnderstandsAllowedChildren5()
        {
            // Arrange
            var documentContent = "<p><hr /></p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "br", "strong" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void Rewrite_UnderstandsAllowedChildren6()
        {
            // Arrange
            var documentContent = "<p><br>Hello</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "strong" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void Rewrite_UnderstandsAllowedChildren7()
        {
            // Arrange
            var documentContent = "<p><strong>Title:</strong><br />Something</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "strong" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void Rewrite_UnderstandsAllowedChildren8()
        {
            // Arrange
            var documentContent = "<p><strong>Title:</strong><br />Something</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "strong", "br" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void Rewrite_UnderstandsAllowedChildren9()
        {
            // Arrange
            var documentContent = "<p>  <strong>Title:</strong>  <br />  Something</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "strong", "br" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void Rewrite_UnderstandsAllowedChildren10()
        {
            // Arrange
            var documentContent = "<p><strong>Title:<br><em>A Very Cool</em></strong><br />Something</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "strong" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void Rewrite_UnderstandsAllowedChildren11()
        {
            // Arrange
            var documentContent = "<p><custom>Title:<br><em>A Very Cool</em></custom><br />Something</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "custom" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void Rewrite_UnderstandsAllowedChildren12()
        {
            // Arrange
            var documentContent = "<p></</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "custom" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void Rewrite_UnderstandsAllowedChildren13()
        {
            // Arrange
            var documentContent = "<p><</p>";
            var descriptors = GetAllowedChildrenTagHelperDescriptors(new[] { "custom" });

            // Act & Assert
            EvaluateData(descriptors, documentContent);
        }

        [Fact]
        public void Rewrite_UnderstandsAllowedChildren14()
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
        public void Rewrite_AllowsSimpleHtmlCommentsAsChildren()
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
        public void Rewrite_DoesntAllowSimpleHtmlCommentsAsChildrenWhenFeatureFlagIsOff()
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
        public void Rewrite_FailsForContentWithCommentsAsChildren()
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
        public void Rewrite_AllowsRazorCommentsAsChildren()
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
        public void Rewrite_AllowsRazorMarkupInHtmlComment()
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
        public void Rewrite_UnderstandsNullTagNameWithAllowedChildrenForCatchAll()
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
        public void Rewrite_UnderstandsNullTagNameWithAllowedChildrenForCatchAllWithPrefix()
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
        public void Rewrite_CanHandleStartTagOnlyTagTagMode()
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
        public void Rewrite_CreatesErrorForWithoutEndTagTagStructureForEndTags()
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
        public void Rewrite_CreatesErrorForInconsistentTagStructures()
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
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly1()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p />");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly2()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p></p>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly3()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div />");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly4()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div></div>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly5()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p class=\"btn\" />");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly6()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p class=\"@DateTime.Now\" />");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly7()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p class=\"btn\">words and spaces</p>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly8()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p class=\"@DateTime.Now\">words and spaces</p>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly9()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p class=\"btn\">words<strong>and</strong>spaces</p>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly10()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<strong catchAll=\"hi\" />");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly11()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<strong catchAll=\"@DateTime.Now\" />");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly12()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<strong catchAll=\"hi\">words and spaces</strong>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly13()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<strong catchAll=\"@DateTime.Now\">words and spaces</strong>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly14()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div class=\"btn\" />");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly15()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div class=\"btn\"></div>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly16()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p notRequired=\"a\" class=\"btn\" />");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly17()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p notRequired=\"@DateTime.Now\" class=\"btn\" />");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly18()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p notRequired=\"a\" class=\"btn\">words and spaces</p>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly19()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"\" class=\"btn\" />");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly20()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"@DateTime.Now\" class=\"btn\" />");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly21()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"\" class=\"btn\">words and spaces</div>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly22()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"@DateTime.Now\" class=\"@DateTime.Now\">words and spaces</div>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly23()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"\" class=\"btn\">words<strong>and</strong>spaces</div>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly24()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p class=\"btn\" catchAll=\"hi\" />");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly25()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<p class=\"btn\" catchAll=\"hi\">words and spaces</p>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly26()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"\" class=\"btn\" catchAll=\"hi\" />");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly27()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"\" class=\"btn\" catchAll=\"hi\" >words and spaces</div>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly28()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"\" class=\"btn\" catchAll=\"@@hi\" >words and spaces</div>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly29()
        {
            EvaluateData(RequiredAttribute_Descriptors, "<div style=\"@DateTime.Now\" class=\"@DateTime.Now\" catchAll=\"@DateTime.Now\" >words and spaces</div>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly30()
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
        public void Rewrite_NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly1()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<p class=\"btn\"><p></p></p>");
        }

        [Fact]
        public void Rewrite_NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly2()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<strong catchAll=\"hi\"><strong></strong></strong>");
        }

        [Fact]
        public void Rewrite_NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly3()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<p class=\"btn\"><strong><p></p></strong></p>");
        }

        [Fact]
        public void Rewrite_NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly4()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<strong catchAll=\"hi\"><p><strong></strong></p></strong>");
        }

        [Fact]
        public void Rewrite_NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly5()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<p class=\"btn\"><strong catchAll=\"hi\"><p></p></strong></p>");
        }

        [Fact]
        public void Rewrite_NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly6()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<strong catchAll=\"hi\"><p class=\"btn\"><strong></strong></p></strong>");
        }

        [Fact]
        public void Rewrite_NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly7()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<p class=\"btn\"><p class=\"btn\"><p></p></p></p>");
        }

        [Fact]
        public void Rewrite_NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly8()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<strong catchAll=\"hi\"><strong catchAll=\"hi\"><strong></strong></strong></strong>");
        }

        [Fact]
        public void Rewrite_NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly9()
        {
            EvaluateData(NestedRequiredAttribute_Descriptors, "<p class=\"btn\"><p><p><p class=\"btn\"><p></p></p></p></p></p>");
        }

        [Fact]
        public void Rewrite_NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly10()
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
        public void Rewrite_RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly1()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly2()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p class=\"btn\"");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly3()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p notRequired=\"hi\" class=\"btn\"");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly4()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p></p");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly5()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p class=\"btn\"></p");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly6()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p notRequired=\"hi\" class=\"btn\"></p");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly7()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p class=\"btn\" <p>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly8()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p notRequired=\"hi\" class=\"btn\" <p>");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly9()
        {
            EvaluateData(MalformedRequiredAttribute_Descriptors, "<p class=\"btn\" </p");
        }

        [Fact]
        public void Rewrite_RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly10()
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
        public void Rewrite_AllowsPrefixedTagHelpers1()
        {
            EvaluateData(PrefixedTagHelperCatchAll_Descriptors, "<th: />", tagHelperPrefix: "th:");
        }

        [Fact]
        public void Rewrite_AllowsPrefixedTagHelpers2()
        {
            EvaluateData(PrefixedTagHelperCatchAll_Descriptors, "<th:>words and spaces</th:>", tagHelperPrefix: "th:");
        }

        [Fact]
        public void Rewrite_AllowsPrefixedTagHelpers3()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<th:myth />", tagHelperPrefix: "th:");
        }

        [Fact]
        public void Rewrite_AllowsPrefixedTagHelpers4()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<th:myth></th:myth>", tagHelperPrefix: "th:");
        }

        [Fact]
        public void Rewrite_AllowsPrefixedTagHelpers5()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<th:myth><th:my2th></th:my2th></th:myth>", tagHelperPrefix: "th:");
        }

        [Fact]
        public void Rewrite_AllowsPrefixedTagHelpers6()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<!th:myth />", tagHelperPrefix: "th:");
        }

        [Fact]
        public void Rewrite_AllowsPrefixedTagHelpers7()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<!th:myth></!th:myth>", tagHelperPrefix: "th:");
        }

        [Fact]
        public void Rewrite_AllowsPrefixedTagHelpers8()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<th:myth class=\"btn\" />", tagHelperPrefix: "th:");
        }

        [Fact]
        public void Rewrite_AllowsPrefixedTagHelpers9()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<th:myth2 class=\"btn\" />", tagHelperPrefix: "th:");
        }

        [Fact]
        public void Rewrite_AllowsPrefixedTagHelpers10()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<th:myth class=\"btn\">words and spaces</th:myth>", tagHelperPrefix: "th:");
        }

        [Fact]
        public void Rewrite_AllowsPrefixedTagHelpers11()
        {
            EvaluateData(PrefixedTagHelperColon_Descriptors, "<th:myth2 bound=\"@DateTime.Now\" />", tagHelperPrefix: "th:");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock_WithAttributeTextTag1()
        {
            RunParseTreeRewriterTest("@{<!text class=\"btn\">}", "p", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock_WithAttributeTextTag2()
        {
            RunParseTreeRewriterTest("@{<!text class=\"btn\"></!text>}", "p", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock_WithAttributeTextTag3()
        {
            RunParseTreeRewriterTest("@{<!text class=\"btn\">words with spaces</!text>}", "p", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock_WithAttributeTextTag4()
        {
            RunParseTreeRewriterTest("@{<!text class='btn1 btn2' class2=btn></!text>}", "p", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock_WithAttributeTextTag5()
        {
            RunParseTreeRewriterTest("@{<!text class='btn1 @DateTime.Now btn2'></!text>}", "p", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag1()
        {
            RunParseTreeRewriterTest("@{<!text>}", "p", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag2()
        {
            RunParseTreeRewriterTest("@{</!text>}", "p", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag3()
        {
            RunParseTreeRewriterTest("@{<!text></!text>}", "p", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag4()
        {
            RunParseTreeRewriterTest("@{<!text>words and spaces</!text>}", "p", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag5()
        {
            RunParseTreeRewriterTest("@{<!text></text>}", "p", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag6()
        {
            RunParseTreeRewriterTest("@{<text></!text>}", "p", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag7()
        {
            RunParseTreeRewriterTest("@{<!text><text></text></!text>}", "p", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag8()
        {
            RunParseTreeRewriterTest("@{<text><!text></!text>}", "p", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock_WithBlockTextTag9()
        {
            RunParseTreeRewriterTest("@{<!text></!text></text>}", "p", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock1()
        {
            RunParseTreeRewriterTest("@{<!text}", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock2()
        {
            RunParseTreeRewriterTest("@{<!text /}", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock3()
        {
            RunParseTreeRewriterTest("@{<!text class=}", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock4()
        {
            RunParseTreeRewriterTest("@{<!text class=\"btn}", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock5()
        {
            RunParseTreeRewriterTest("@{<!text class=\"btn\"}", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock6()
        {
            RunParseTreeRewriterTest("@{<!text class=\"btn\" /}", "text");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock1()
        {
            RunParseTreeRewriterTest("@{<!}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock2()
        {
            RunParseTreeRewriterTest("@{<!p}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock3()
        {
            RunParseTreeRewriterTest("@{<!p /}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock4()
        {
            RunParseTreeRewriterTest("@{<!p class=}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock5()
        {
            RunParseTreeRewriterTest("@{<!p class=\"btn}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock6()
        {
            RunParseTreeRewriterTest("@{<!p class=\"btn@@}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock7()
        {
            RunParseTreeRewriterTest("@{<!p class=\"btn\"}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock8()
        {
            RunParseTreeRewriterTest("@{<!p class=\"btn\" /}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTML1()
        {
            RunParseTreeRewriterTest("<!", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTML2()
        {
            RunParseTreeRewriterTest("<!p", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTML3()
        {
            RunParseTreeRewriterTest("<!p /", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTML4()
        {
            RunParseTreeRewriterTest("<!p class=", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTML5()
        {
            RunParseTreeRewriterTest("<!p class=\"btn", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTML6()
        {
            RunParseTreeRewriterTest("<!p class=\"btn\"", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTML7()
        {
            RunParseTreeRewriterTest("<!p class=\"btn\" /", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithBlockData1()
        {
            RunParseTreeRewriterTest("@{<!p>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithBlockData2()
        {
            RunParseTreeRewriterTest("@{</!p>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithBlockData3()
        {
            RunParseTreeRewriterTest("@{<!p></!p>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithBlockData4()
        {
            RunParseTreeRewriterTest("@{<!p>words and spaces</!p>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithBlockData5()
        {
            RunParseTreeRewriterTest("@{<!p></p>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithBlockData6()
        {
            RunParseTreeRewriterTest("@{<p></!p>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithBlockData7()
        {
            RunParseTreeRewriterTest("@{<p><!p></!p></p>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithBlockData8()
        {
            RunParseTreeRewriterTest("@{<p><!p></!p>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithBlockData9()
        {
            RunParseTreeRewriterTest("@{<!p></!p></p>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithBlockData10()
        {
            RunParseTreeRewriterTest("@{<strong></!p></strong>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithBlockData11()
        {
            RunParseTreeRewriterTest("@{<strong></strong><!p></!p>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithBlockData12()
        {
            RunParseTreeRewriterTest("@{<p><strong></!strong><!p></strong></!p>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithAttributeData1()
        {
            RunParseTreeRewriterTest("@{<!p class=\"btn\">}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithAttributeData2()
        {
            RunParseTreeRewriterTest("@{<!p class=\"btn\"></!p>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithAttributeData3()
        {
            RunParseTreeRewriterTest("@{<!p class=\"btn\">words with spaces</!p>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithAttributeData4()
        {
            RunParseTreeRewriterTest("@{<!p class='btn1 btn2' class2=btn></!p>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp_WithAttributeData5()
        {
            RunParseTreeRewriterTest("@{<!p class='btn1 @DateTime.Now btn2'></!p>}", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithBlockData1()
        {
            RunParseTreeRewriterTest("<!p>", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithBlockData2()
        {
            RunParseTreeRewriterTest("</!p>", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithBlockData3()
        {
            RunParseTreeRewriterTest("<!p></!p>", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithBlockData4()
        {
            RunParseTreeRewriterTest("<!p>words and spaces</!p>", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithBlockData5()
        {
            RunParseTreeRewriterTest("<!p></p>", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithBlockData6()
        {
            RunParseTreeRewriterTest("<p></!p>", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithBlockData7()
        {
            RunParseTreeRewriterTest("<p><!p></!p></p>", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithBlockData8()
        {
            RunParseTreeRewriterTest("<p><!p></!p>", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithBlockData9()
        {
            RunParseTreeRewriterTest("<!p></!p></p>", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithBlockData10()
        {
            RunParseTreeRewriterTest("<strong></!p></strong>", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithBlockData11()
        {
            RunParseTreeRewriterTest("<strong></strong><!p></!p>", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithBlockData12()
        {
            RunParseTreeRewriterTest("<p><strong></!strong><!p></strong></!p>", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithAttributeData1()
        {
            RunParseTreeRewriterTest("<!p class=\"btn\">", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithAttributeData2()
        {
            RunParseTreeRewriterTest("<!p class=\"btn\"></!p>", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithAttributeData3()
        {
            RunParseTreeRewriterTest("<!p class=\"btn\">words and spaces</!p>", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithAttributeData4()
        {
            RunParseTreeRewriterTest("<!p class='btn1 btn2' class2=btn></!p>", "strong", "p");
        }

        [Fact]
        public void Rewrite_AllowsTagHelperElementOptOutHTML_WithAttributeData5()
        {
            RunParseTreeRewriterTest("<!p class='btn1 @DateTime.Now btn2'></!p>", "strong", "p");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_DoesNotRewriteTextTagTransitionTagHelpers1()
        {
            RunParseTreeRewriterTest("<text>Hello World</text>", "p", "text");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_DoesNotRewriteTextTagTransitionTagHelpers2()
        {
            RunParseTreeRewriterTest("@{<text>Hello World</text>}", "p", "text");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_DoesNotRewriteTextTagTransitionTagHelpers3()
        {
            RunParseTreeRewriterTest("@{<text><p>Hello World</p></text>}", "p", "text");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_DoesNotRewriteTextTagTransitionTagHelpers4()
        {
            RunParseTreeRewriterTest("@{<p><text>Hello World</text></p>}", "p", "text");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_DoesNotRewriteSpecialTagTagHelpers1()
        {
            RunParseTreeRewriterTest("<foo><!-- Hello World --></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_DoesNotRewriteSpecialTagTagHelpers2()
        {
            RunParseTreeRewriterTest("<foo><!-- @foo --></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_DoesNotRewriteSpecialTagTagHelpers3()
        {
            RunParseTreeRewriterTest("<foo><?xml Hello World ?></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_DoesNotRewriteSpecialTagTagHelpers4()
        {
            RunParseTreeRewriterTest("<foo><?xml @foo ?></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_DoesNotRewriteSpecialTagTagHelpers5()
        {
            RunParseTreeRewriterTest("<foo><!DOCTYPE @foo ></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_DoesNotRewriteSpecialTagTagHelpers6()
        {
            RunParseTreeRewriterTest("<foo><!DOCTYPE hello=\"world\" ></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_DoesNotRewriteSpecialTagTagHelpers7()
        {
            RunParseTreeRewriterTest("<foo><![CDATA[ Hello World ]]></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_DoesNotRewriteSpecialTagTagHelpers8()
        {
            RunParseTreeRewriterTest("<foo><![CDATA[ @foo ]]></foo>", "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesNestedTagHelperTagBlocks1()
        {
            RunParseTreeRewriterTest("<p><div></div></p>", "p", "div");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesNestedTagHelperTagBlocks2()
        {
            RunParseTreeRewriterTest("<p>Hello World <div></div></p>", "p", "div");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesNestedTagHelperTagBlocks3()
        {
            RunParseTreeRewriterTest("<p>Hel<p>lo</p></p> <p><div>World</div></p>", "p", "div");
        }

        [Fact]
        public void TagHelperParseTreeRewriter_RewritesNestedTagHelperTagBlocks4()
        {
            RunParseTreeRewriterTest("<p>Hel<strong>lo</strong></p> <p><span>World</span></p>", "p", "div");
        }

        [Fact]
        public void Rewrite_HandlesMalformedNestedNonTagHelperTags_Correctly()
        {
            RunParseTreeRewriterTest("<div>@{</div>}");
        }
    }
}