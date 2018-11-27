// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
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

        public static TheoryData PartialRequiredParentData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);
                Func<int, string, RazorDiagnostic> errorFormatUnclosed = (location, tagName) =>
                    RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                        new SourceSpan(new SourceLocation(location, 0, location), tagName.Length), tagName);

                Func<int, string, RazorDiagnostic> errorFormatNoCloseAngle = (location, tagName) =>
                   RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                       new SourceSpan(new SourceLocation(location, 0, location), tagName.Length), tagName);

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorDiagnostic[]>
                {
                    {
                        "<p><strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("strong"))),
                        new[] { errorFormatUnclosed(1, "p"), errorFormatUnclosed(4, "strong") }
                    },
                    {
                        "<p><strong></strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("strong"))),
                        new[] { errorFormatUnclosed(1, "p") }
                    },
                    {
                        "<p><strong></p><strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("strong")),
                            new MarkupTagHelperBlock("strong")),
                        new[] { errorFormatUnclosed(4, "strong"), errorFormatUnclosed(16, "strong") }
                    },
                    {
                        "<<p><<strong></</strong</strong></p>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<"),
                            new MarkupTagHelperBlock("p",
                                blockFactory.MarkupTagBlock("<"),
                                new MarkupTagHelperBlock("strong",
                                    blockFactory.MarkupTagBlock("</")),
                                blockFactory.MarkupTagBlock("</strong>"))),
                        new[] { errorFormatNoCloseAngle(17, "strong"), errorFormatUnclosed(25, "strong") }
                    },
                    {
                        "<<p><<strong></</strong></strong></p>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<"),
                            new MarkupTagHelperBlock("p",
                                blockFactory.MarkupTagBlock("<"),
                                new MarkupTagHelperBlock("strong",
                                    blockFactory.MarkupTagBlock("</")),
                                blockFactory.MarkupTagBlock("</strong>"))),
                        new[] { errorFormatUnclosed(26, "strong") }
                    },

                    {
                        "<<p><<custom></<</custom></custom></p>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<"),
                            new MarkupTagHelperBlock("p",
                                blockFactory.MarkupTagBlock("<"),
                                new MarkupTagHelperBlock("custom",
                                    blockFactory.MarkupTagBlock("</"),
                                    blockFactory.MarkupTagBlock("<")),
                                blockFactory.MarkupTagBlock("</custom>"))),
                        new[] { errorFormatUnclosed(27, "custom") }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(PartialRequiredParentData))]
        public void Rewrite_UnderstandsPartialRequiredParentTags(
            string documentContent,
            object expectedOutput,
            object expectedErrors)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
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

            // Act & Assert
            EvaluateData(descriptors, documentContent, (MarkupBlock)expectedOutput, (RazorDiagnostic[])expectedErrors);
        }

        public static TheoryData NestedVoidSelfClosingRequiredParentData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                // documentContent, expectedOutput
                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<input><strong></strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("input", TagMode.StartTagOnly),
                            blockFactory.MarkupTagBlock("<strong>"),
                            blockFactory.MarkupTagBlock("</strong>"))
                    },
                    {
                        "<p><input><strong></strong></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("input", TagMode.StartTagOnly),
                                new MarkupTagHelperBlock("strong")))
                    },
                    {
                        "<p><br><strong></strong></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                blockFactory.MarkupTagBlock("<br>"),
                                new MarkupTagHelperBlock("strong")))
                    },
                    {
                        "<p><p><br></p><strong></strong></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("p",
                                    blockFactory.MarkupTagBlock("<br>")),
                                new MarkupTagHelperBlock("strong")))
                    },
                    {
                        "<input><strong></strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("input", TagMode.StartTagOnly),
                            blockFactory.MarkupTagBlock("<strong>"),
                            blockFactory.MarkupTagBlock("</strong>"))
                    },
                    {
                        "<p><input /><strong /></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("input", TagMode.SelfClosing),
                                new MarkupTagHelperBlock("strong", TagMode.SelfClosing)))
                    },
                    {
                        "<p><br /><strong /></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                blockFactory.MarkupTagBlock("<br />"),
                                new MarkupTagHelperBlock("strong", TagMode.SelfClosing)))
                    },
                    {
                        "<p><p><br /></p><strong /></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("p",
                                    blockFactory.MarkupTagBlock("<br />")),
                                new MarkupTagHelperBlock("strong", TagMode.SelfClosing)))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(NestedVoidSelfClosingRequiredParentData))]
        public void Rewrite_UnderstandsNestedVoidSelfClosingRequiredParent(
            string documentContent,
            object expectedOutput)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
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

            // Act & Assert
            EvaluateData(descriptors, documentContent, (MarkupBlock)expectedOutput, expectedErrors: new RazorDiagnostic[0]);
        }

        public static TheoryData NestedRequiredParentData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                // documentContent, expectedOutput
                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<strong></strong>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<strong>"),
                            blockFactory.MarkupTagBlock("</strong>"))
                    },
                    {
                        "<p><strong></strong></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("strong")))
                    },
                    {
                        "<div><strong></strong></div>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<div>"),
                            new MarkupTagHelperBlock("strong"),
                            blockFactory.MarkupTagBlock("</div>"))
                    },
                    {
                        "<strong><strong></strong></strong>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<strong>"),
                            blockFactory.MarkupTagBlock("<strong>"),
                            blockFactory.MarkupTagBlock("</strong>"),
                            blockFactory.MarkupTagBlock("</strong>"))
                    },
                    {
                        "<p><strong><strong></strong></strong></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("strong",
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    blockFactory.MarkupTagBlock("</strong>"))))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(NestedRequiredParentData))]
        public void Rewrite_UnderstandsNestedRequiredParent(string documentContent, object expectedOutput)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
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

            // Act & Assert
            EvaluateData(descriptors, documentContent, (MarkupBlock)expectedOutput, expectedErrors: new RazorDiagnostic[0]);
        }

        [Fact]
        public void Rewrite_UnderstandsTagHelperPrefixAndAllowedChildren()
        {
            // Arrange
            var documentContent = "<th:p><th:strong></th:strong></th:p>";
            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock("th:p",
                    new MarkupTagHelperBlock("th:strong")));
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
                expectedOutput,
                expectedErrors: Enumerable.Empty<RazorDiagnostic>(),
                tagHelperPrefix: "th:");
        }

        [Fact]
        public void Rewrite_UnderstandsTagHelperPrefixAndAllowedChildrenAndRequireParent()
        {
            // Arrange
            var documentContent = "<th:p><th:strong></th:strong></th:p>";
            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock("th:p",
                    new MarkupTagHelperBlock("th:strong")));
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
                expectedOutput,
                expectedErrors: Enumerable.Empty<RazorDiagnostic>(),
                tagHelperPrefix: "th:");
        }

        [Fact]
        public void Rewrite_InvalidStructure_UnderstandsTagHelperPrefixAndAllowedChildrenAndRequireParent()
        {
            // Arrange
            var documentContent = "<th:p></th:strong></th:p>";
            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock("th:p",
                    new MarkupTagBlock(
                        Factory.Markup("</th:strong>"))));
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
            var expectedErrors = new[] {
                RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                    new SourceSpan(filePath: null, absoluteIndex: 8, lineIndex: 0, characterIndex: 8, length: 9),
                    "th:strong"),
            };

            // Act & Assert
            EvaluateData(
                descriptors,
                documentContent,
                expectedOutput,
                expectedErrors: expectedErrors,
                tagHelperPrefix: "th:");
        }

        [Fact]
        public void Rewrite_NonTagHelperChild_UnderstandsTagHelperPrefixAndAllowedChildren()
        {
            // Arrange
            var documentContent = "<th:p><strong></strong></th:p>";
            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock("th:p",
                    new MarkupTagBlock(
                        Factory.Markup("<strong>")),
                    new MarkupTagBlock(
                        Factory.Markup("</strong>"))));
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
                expectedOutput,
                expectedErrors: Enumerable.Empty<RazorDiagnostic>(),
                tagHelperPrefix: "th:");
        }

        public static TheoryData InvalidHtmlScriptBlockData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<script type><input /></script>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<script"),
                                new MarkupBlock(factory.Markup(" type")),
                                factory.Markup(">")),
                            factory.Markup("<input />"),
                            blockFactory.MarkupTagBlock("</script>"))
                    },
                    {
                        "<script types='text/html'><input /></script>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<script"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "types",
                                        prefix: new LocationTagged<string>(" types='", 7, 0, 7),
                                        suffix: new LocationTagged<string>("'", 24, 0, 24)),
                                    factory.Markup(" types='").With(SpanChunkGenerator.Null),
                                    factory.Markup("text/html").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 15, 0, 15),
                                            value: new LocationTagged<string>("text/html", 15, 0, 15))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(">")),
                            factory.Markup("<input />"),
                            blockFactory.MarkupTagBlock("</script>"))
                    },
                    {
                        "<script type='text/html invalid'><input /></script>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<script"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "type",
                                        prefix: new LocationTagged<string>(" type='", 7, 0, 7),
                                        suffix: new LocationTagged<string>("'", 31, 0, 31)),
                                    factory.Markup(" type='").With(SpanChunkGenerator.Null),
                                    factory.Markup("text/html").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 14, 0, 14),
                                            value: new LocationTagged<string>("text/html", 14, 0, 14))),
                                    factory.Markup(" invalid").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(" ", 23, 0, 23),
                                            value: new LocationTagged<string>("invalid", 24, 0, 24))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(">")),
                            factory.Markup("<input />"),
                            blockFactory.MarkupTagBlock("</script>"))
                    },
                    {
                        "<script type='text/ng-*' type='text/html'><input /></script>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<script"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "type",
                                        prefix: new LocationTagged<string>(" type='", 7, 0, 7),
                                        suffix: new LocationTagged<string>("'", 23, 0, 23)),
                                    factory.Markup(" type='").With(SpanChunkGenerator.Null),
                                    factory.Markup("text/ng-*").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 14, 0, 14),
                                            value: new LocationTagged<string>("text/ng-*", 14, 0, 14))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "type",
                                        prefix: new LocationTagged<string>(" type='", 24, 0, 24),
                                        suffix: new LocationTagged<string>("'", 40, 0, 40)),
                                    factory.Markup(" type='").With(SpanChunkGenerator.Null),
                                    factory.Markup("text/html").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 31, 0, 31),
                                            value: new LocationTagged<string>("text/html", 31, 0, 31))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(">")),
                            factory.Markup("<input />"),
                            blockFactory.MarkupTagBlock("</script>"))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidHtmlScriptBlockData))]
        public void TagHelperParseTreeRewriter_DoesNotUnderstandTagHelpersInInvalidHtmlTypedScriptTags(
            string documentContent,
            object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, "input");
        }

        public static TheoryData HtmlScriptBlockData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<script type='text/html'><input /></script>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<script"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "type",
                                        prefix: new LocationTagged<string>(" type='", 7, 0, 7),
                                        suffix: new LocationTagged<string>("'", 23, 0, 23)),
                                    factory.Markup(" type='").With(SpanChunkGenerator.Null),
                                    factory.Markup("text/html").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 14, 0, 14),
                                            value: new LocationTagged<string>("text/html", 14, 0, 14))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(">")),
                            new MarkupTagHelperBlock("input", TagMode.SelfClosing),
                            blockFactory.MarkupTagBlock("</script>"))
                    },
                    {
                        "<script id='scriptTag' type='text/html' class='something'><input /></script>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<script"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "id",
                                        prefix: new LocationTagged<string>(" id='", 7, 0, 7),
                                        suffix: new LocationTagged<string>("'", 21, 0, 21)),
                                    factory.Markup(" id='").With(SpanChunkGenerator.Null),
                                    factory.Markup("scriptTag").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 12, 0, 12),
                                            value: new LocationTagged<string>("scriptTag", 12, 0, 12))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "type",
                                        prefix: new LocationTagged<string>(" type='", 22, 0, 22),
                                        suffix: new LocationTagged<string>("'", 38, 0, 38)),
                                    factory.Markup(" type='").With(SpanChunkGenerator.Null),
                                    factory.Markup("text/html").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 29, 0, 29),
                                            value: new LocationTagged<string>("text/html", 29, 0, 29))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class='", 39, 0, 39),
                                        suffix: new LocationTagged<string>("'", 56, 0, 56)),
                                    factory.Markup(" class='").With(SpanChunkGenerator.Null),
                                    factory.Markup("something").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 47, 0, 47),
                                            value: new LocationTagged<string>("something", 47, 0, 47))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(">")),
                            new MarkupTagHelperBlock("input", TagMode.SelfClosing),
                            blockFactory.MarkupTagBlock("</script>"))
                    },
                    {
                        "<script type='text/html'><p><script type='text/html'><input /></script></p></script>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<script"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "type",
                                        prefix: new LocationTagged<string>(" type='", 7, 0, 7),
                                        suffix: new LocationTagged<string>("'", 23, 0, 23)),
                                    factory.Markup(" type='").With(SpanChunkGenerator.Null),
                                    factory.Markup("text/html").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 14, 0, 14),
                                            value: new LocationTagged<string>("text/html", 14, 0, 14))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(">")),
                            new MarkupTagHelperBlock("p",
                                new MarkupTagBlock(
                                    factory.Markup("<script"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "type",
                                            prefix: new LocationTagged<string>(" type='", 35, 0, 35),
                                            suffix: new LocationTagged<string>("'", 51, 0, 51)),
                                        factory.Markup(" type='").With(SpanChunkGenerator.Null),
                                        factory.Markup("text/html").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 42, 0, 42),
                                                value: new LocationTagged<string>("text/html", 42, 0, 42))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                    factory.Markup(">")),
                                new MarkupTagHelperBlock("input", TagMode.SelfClosing),
                                blockFactory.MarkupTagBlock("</script>")),
                            blockFactory.MarkupTagBlock("</script>"))
                    },
                    {
                        "<script type='text/html'><p><script type='text/ html'><input /></script></p></script>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<script"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "type",
                                        prefix: new LocationTagged<string>(" type='", 7, 0, 7),
                                        suffix: new LocationTagged<string>("'", 23, 0, 23)),
                                    factory.Markup(" type='").With(SpanChunkGenerator.Null),
                                    factory.Markup("text/html").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 14, 0, 14),
                                            value: new LocationTagged<string>("text/html", 14, 0, 14))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(">")),
                            new MarkupTagHelperBlock("p",
                                new MarkupTagBlock(
                                    factory.Markup("<script"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "type",
                                            prefix: new LocationTagged<string>(" type='", 35, 0, 35),
                                            suffix: new LocationTagged<string>("'", 52, 0, 52)),
                                        factory.Markup(" type='").With(SpanChunkGenerator.Null),
                                        factory.Markup("text/").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 42, 0, 42),
                                                value: new LocationTagged<string>("text/", 42, 0, 42))),
                                        factory.Markup(" html").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(" ", 47, 0, 47),
                                                value: new LocationTagged<string>("html", 48, 0, 48))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                    factory.Markup(">")),
                                factory.Markup("<input />"),
                                blockFactory.MarkupTagBlock("</script>")),
                            blockFactory.MarkupTagBlock("</script>"))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(HtmlScriptBlockData))]
        public void TagHelperParseTreeRewriter_UnderstandsTagHelpersInHtmlTypedScriptTags(
            string documentContent,
            object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, "p", "input");
        }

        [Fact]
        public void Rewrite_CanHandleInvalidChildrenWithWhitespace()
        {
            // Arrange
            var factory = new SpanFactory();
            var blockFactory = new BlockFactory(factory);
            var documentContent = $"<p>{Environment.NewLine}    <strong>{Environment.NewLine}        Hello" +
                $"{Environment.NewLine}    </strong>{Environment.NewLine}</p>";
            var newLineLength = Environment.NewLine.Length;
            var expectedErrors = new[] {
                RazorDiagnosticFactory.CreateTagHelper_InvalidNestedTag(
                    new SourceSpan(absoluteIndex: 8 + newLineLength, lineIndex: 1, characterIndex: 5, length: 6), "strong", "p", "br"),
            };
            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock("p",
                    factory.Markup(Environment.NewLine + "    "),
                    blockFactory.MarkupTagBlock("<strong>"),
                    factory.Markup(Environment.NewLine + "        Hello" + Environment.NewLine + "    "),
                    blockFactory.MarkupTagBlock("</strong>"),
                    factory.Markup(Environment.NewLine)));
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("PTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule => rule.RequireTagName("p"))
                    .AllowChildTag("br")
                    .Build()
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent, expectedOutput, expectedErrors);
        }

        [Fact]
        public void Rewrite_RecoversWhenRequiredAttributeMismatchAndRestrictedChildren()
        {
            // Arrange
            var factory = new SpanFactory();
            var blockFactory = new BlockFactory(factory);
            var documentContent = "<strong required><strong></strong></strong>";

            var expectedErrors = new[] {
                RazorDiagnosticFactory.CreateTagHelper_InvalidNestedTag(
                    new SourceSpan(absoluteIndex: 18, lineIndex: 0, characterIndex: 18, length: 6), "strong", "strong", "br")
            };
            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock("strong",
                    new List<TagHelperAttributeNode>
                    {
                        new TagHelperAttributeNode("required", null, AttributeStructure.Minimized)
                    },
                    blockFactory.MarkupTagBlock("<strong>"),
                    blockFactory.MarkupTagBlock("</strong>")));
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
            EvaluateData(descriptors, documentContent, expectedOutput, expectedErrors);
        }

        [Fact]
        public void Rewrite_CanHandleMultipleTagHelpersWithAllowedChildren_OneNull()
        {
            // Arrange
            var factory = new SpanFactory();
            var documentContent = "<p><strong>Hello World</strong><br></p>";
            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock("p",
                    new MarkupTagHelperBlock("strong",
                        factory.Markup("Hello World")),
                    new MarkupTagHelperBlock("br", TagMode.StartTagOnly)));
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
            EvaluateData(descriptors, documentContent, expectedOutput, expectedErrors: new RazorDiagnostic[0]);
        }

        [Fact]
        public void Rewrite_CanHandleMultipleTagHelpersWithAllowedChildren()
        {
            // Arrange
            var factory = new SpanFactory();
            var documentContent = "<p><strong>Hello World</strong><br></p>";
            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock("p",
                    new MarkupTagHelperBlock("strong",
                        factory.Markup("Hello World")),
                    new MarkupTagHelperBlock("br", TagMode.StartTagOnly)));
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
            EvaluateData(descriptors, documentContent, expectedOutput, expectedErrors: new RazorDiagnostic[0]);
        }

        public static TheoryData AllowedChildrenData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);
                Func<string, string, string, int, int, RazorDiagnostic> nestedTagError =
                    (childName, parentName, allowed, location, length) =>
                    RazorDiagnosticFactory.CreateTagHelper_InvalidNestedTag(
                        new SourceSpan(absoluteIndex: location, lineIndex: 0, characterIndex: location, length: length), childName, parentName, allowed);
                Func<string, string, int, int, RazorDiagnostic> nestedContentError =
                    (parentName, allowed, location, length) =>
                    RazorDiagnosticFactory.CreateTagHelper_CannotHaveNonTagContent(
                        new SourceSpan(absoluteIndex: location, lineIndex: 0, characterIndex: location, length: length), parentName, allowed);

                return new TheoryData<string, IEnumerable<string>, MarkupBlock, RazorDiagnostic[]>
                {
                    {
                        "<p><br /></p>",
                        new[] { "br" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("br", TagMode.SelfClosing))),
                        new RazorDiagnostic[0]
                    },
                    {
                        $"<p>{Environment.NewLine}<br />{Environment.NewLine}</p>",
                        new[] { "br" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                factory.Markup(Environment.NewLine),
                                new MarkupTagHelperBlock("br", TagMode.SelfClosing),
                                factory.Markup(Environment.NewLine))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<p><br></p>",
                        new[] { "strong" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("br", TagMode.StartTagOnly))),
                        new[] { nestedTagError("br", "p", "strong", 4, 2) }
                    },
                    {
                        "<p>Hello</p>",
                        new[] { "strong" },
                        new MarkupBlock(new MarkupTagHelperBlock("p", factory.Markup("Hello"))),
                        new[] { nestedContentError("p", "strong", 3, 5) }
                    },
                    {
                        "<p><hr /></p>",
                        new[] { "br", "strong" },
                        new MarkupBlock(new MarkupTagHelperBlock("p", blockFactory.MarkupTagBlock("<hr />"))),
                        new[] { nestedTagError("hr", "p", "br, strong", 4, 2) }
                    },
                    {
                        "<p><br>Hello</p>",
                        new[] { "strong" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("br", TagMode.StartTagOnly),
                                factory.Markup("Hello"))),
                        new[] { nestedTagError("br", "p", "strong", 4, 2), nestedContentError("p", "strong", 7, 5) }
                    },
                    {
                        "<p><strong>Title:</strong><br />Something</p>",
                        new[] { "strong" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("strong", factory.Markup("Title:")),
                                new MarkupTagHelperBlock("br", TagMode.SelfClosing),
                                factory.Markup("Something"))),
                        new[]
                        {
                            nestedContentError("strong", "strong", 11, 6),
                            nestedTagError("br", "p", "strong", 27, 2),
                            nestedContentError("p", "strong", 32, 9),
                        }
                    },
                    {
                        "<p><strong>Title:</strong><br />Something</p>",
                        new[] { "strong", "br" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("strong", factory.Markup("Title:")),
                                new MarkupTagHelperBlock("br", TagMode.SelfClosing),
                                factory.Markup("Something"))),
                        new[]
                        {
                            nestedContentError("strong", "strong, br", 11, 6),
                            nestedContentError("p", "strong, br", 32, 9),
                        }
                    },
                    {
                        "<p>  <strong>Title:</strong>  <br />  Something</p>",
                        new[] { "strong", "br" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                factory.Markup("  "),
                                new MarkupTagHelperBlock("strong", factory.Markup("Title:")),
                                factory.Markup("  "),
                                new MarkupTagHelperBlock("br", TagMode.SelfClosing),
                                factory.Markup("  Something"))),
                        new[]
                        {
                            nestedContentError("strong", "strong, br", 13, 6),
                            nestedContentError("p", "strong, br", 38, 9),
                        }
                    },
                    {
                        "<p><strong>Title:<br><em>A Very Cool</em></strong><br />Something</p>",
                        new[] { "strong" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("strong",
                                    factory.Markup("Title:"),
                                    new MarkupTagHelperBlock("br", TagMode.StartTagOnly),
                                    blockFactory.MarkupTagBlock("<em>"),
                                    factory.Markup("A Very Cool"),
                                    blockFactory.MarkupTagBlock("</em>")),
                                new MarkupTagHelperBlock("br", TagMode.SelfClosing),
                                factory.Markup("Something"))),
                        new[]
                        {
                            nestedContentError("strong", "strong", 11, 6),
                            nestedTagError("br", "strong", "strong", 18, 2),
                            nestedTagError("em", "strong", "strong", 22, 2),
                            nestedTagError("br", "p", "strong", 51, 2),
                            nestedContentError("p", "strong", 56, 9)
                        }
                    },
                    {
                        "<p><custom>Title:<br><em>A Very Cool</em></custom><br />Something</p>",
                        new[] { "custom" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                blockFactory.MarkupTagBlock("<custom>"),
                                factory.Markup("Title:"),
                                new MarkupTagHelperBlock("br", TagMode.StartTagOnly),
                                blockFactory.MarkupTagBlock("<em>"),
                                factory.Markup("A Very Cool"),
                                blockFactory.MarkupTagBlock("</em>"),
                                blockFactory.MarkupTagBlock("</custom>"),
                                new MarkupTagHelperBlock("br", TagMode.SelfClosing),
                                factory.Markup("Something"))),
                        new[]
                        {
                            nestedTagError("br", "p", "custom", 51, 2),
                            nestedContentError("p", "custom", 56, 9)
                        }
                    },
                    {
                        "<p></</p>",
                        new[] { "custom" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                blockFactory.MarkupTagBlock("</"))),
                        new[]
                        {
                            nestedContentError("p", "custom", 3, 2),
                        }
                    },
                    {
                        "<p><</p>",
                        new[] { "custom" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                blockFactory.MarkupTagBlock("<"))),
                        new[]
                        {
                            nestedContentError("p", "custom", 3, 1),
                        }
                    },
                    {
                        "<p><custom><br>:<strong><strong>Hello</strong></strong>:<input></custom></p>",
                        new[] { "custom", "strong" },
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                blockFactory.MarkupTagBlock("<custom>"),
                                new MarkupTagHelperBlock("br", TagMode.StartTagOnly),
                                factory.Markup(":"),
                                new MarkupTagHelperBlock("strong",
                                    new MarkupTagHelperBlock("strong",
                                        factory.Markup("Hello"))),
                                factory.Markup(":"),
                                blockFactory.MarkupTagBlock("<input>"),
                                blockFactory.MarkupTagBlock("</custom>"))),
                        new[]
                        {
                            nestedContentError("strong", "custom, strong", 32, 5),
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(AllowedChildrenData))]
        public void Rewrite_UnderstandsAllowedChildren(
            string documentContent,
            IEnumerable<string> allowedChildren,
            object expectedOutput,
            object expectedErrors)
        {
            // Arrange
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

            // Act & Assert
            EvaluateData(descriptors, documentContent, (MarkupBlock)expectedOutput, (RazorDiagnostic[])expectedErrors);
        }

        [Fact]
        public void Rewrite_AllowsSimpleHtmlCommentsAsChildren()
        {
            // Arrange
            IEnumerable<string> allowedChildren = new List<string> { "b" };
            string literal = "asdf";
            string commentOutput = "Hello World";
            string expectedOutput = $"<p><b>{literal}</b><!--{commentOutput}--></p>";

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

            var factory = new SpanFactory();
            var blockFactory = new BlockFactory(factory);

            var expectedMarkup = new MarkupBlock(
                new MarkupTagHelperBlock("p",
                    blockFactory.MarkupTagBlock("<b>"),
                    factory.Markup(literal),
                    blockFactory.MarkupTagBlock("</b>"),
                    blockFactory.HtmlCommentBlock(commentOutput)));

            // Act & Assert
            EvaluateData(
                descriptors,
                expectedOutput,
                expectedMarkup,
                Array.Empty<RazorDiagnostic>());
        }

        [Fact]
        public void Rewrite_DoesntAllowSimpleHtmlCommentsAsChildrenWhenFeatureFlagIsOff()
        {
            // Arrange
            Func<string, string, string, int, int, RazorDiagnostic> nestedTagError =
                    (childName, parentName, allowed, location, length) =>
                    RazorDiagnosticFactory.CreateTagHelper_InvalidNestedTag(
                        new SourceSpan(absoluteIndex: location, lineIndex: 0, characterIndex: location, length: length), childName, parentName, allowed);
            Func<string, string, int, int, RazorDiagnostic> nestedContentError =
                (parentName, allowed, location, length) =>
                RazorDiagnosticFactory.CreateTagHelper_CannotHaveNonTagContent(
                    new SourceSpan(absoluteIndex: location, lineIndex: 0, characterIndex: location, length: length), parentName, allowed);

            IEnumerable<string> allowedChildren = new List<string> { "b" };
            string comment1 = "Hello";
            string expectedOutput = $"<p><!--{comment1}--></p>";

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

            var factory = new SpanFactory();
            var blockFactory = new BlockFactory(factory);

            var expectedMarkup = new MarkupBlock(
                new MarkupTagHelperBlock("p",
                    blockFactory.HtmlCommentBlock(comment1)));

            // Act & Assert
            EvaluateData(
                descriptors,
                expectedOutput,
                expectedMarkup,
                new[]
                {
                    nestedContentError("p", "b", 3, 4),
                    nestedContentError("p", "b", 7, 5),
                    nestedContentError("p", "b", 12, 3),
                },
                featureFlags: RazorParserFeatureFlags.Create(RazorLanguageVersion.Version_2_0));
        }

        [Fact]
        public void Rewrite_FailsForContentWithCommentsAsChildren()
        {
            // Arrange
            Func<string, string, string, int, int, RazorDiagnostic> nestedTagError =
                    (childName, parentName, allowed, location, length) =>
                    RazorDiagnosticFactory.CreateTagHelper_InvalidNestedTag(
                        new SourceSpan(absoluteIndex: location, lineIndex: 0, characterIndex: location, length: length), childName, parentName, allowed);
            Func<string, string, int, int, RazorDiagnostic> nestedContentError =
                (parentName, allowed, location, length) =>
                RazorDiagnosticFactory.CreateTagHelper_CannotHaveNonTagContent(
                    new SourceSpan(absoluteIndex: location, lineIndex: 0, characterIndex: location, length: length), parentName, allowed);

            IEnumerable<string> allowedChildren = new List<string> { "b" };
            string comment1 = "Hello";
            string literal = "asdf";
            string comment2 = "World";
            string expectedOutput = $"<p><!--{comment1}-->{literal}<!--{comment2}--></p>";

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

            var factory = new SpanFactory();
            var blockFactory = new BlockFactory(factory);

            var expectedMarkup = new MarkupBlock(
                new MarkupTagHelperBlock("p",
                    blockFactory.HtmlCommentBlock(comment1),
                    factory.Markup(literal),
                    blockFactory.HtmlCommentBlock(comment2)));

            // Act & Assert
            EvaluateData(
                descriptors,
                expectedOutput,
                expectedMarkup,
                new[]
                {
                    nestedContentError("p", "b", 15, 4),
                });
        }

        [Fact]
        public void Rewrite_AllowsRazorCommentsAsChildren()
        {
            // Arrange
            IEnumerable<string> allowedChildren = new List<string> { "b" };
            string literal = "asdf";
            string commentOutput = $"@*{literal}*@";
            string expectedOutput = $"<p><b>{literal}</b>{commentOutput}</p>";

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

            var factory = new SpanFactory();
            var blockFactory = new BlockFactory(factory);

            var expectedMarkup = new MarkupBlock(
                new MarkupTagHelperBlock("p",
                    blockFactory.MarkupTagBlock("<b>"),
                    factory.Markup(literal),
                    blockFactory.MarkupTagBlock("</b>"),
                    new CommentBlock(
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition).Accepts(AcceptedCharactersInternal.None),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharactersInternal.None),
                        Factory.Span(SpanKindInternal.Comment, new HtmlSymbol(literal, HtmlSymbolType.RazorComment)).Accepts(AcceptedCharactersInternal.Any),
                        Factory.MetaMarkup("*", HtmlSymbolType.RazorCommentStar).Accepts(AcceptedCharactersInternal.None),
                        Factory.MarkupTransition(HtmlSymbolType.RazorCommentTransition).Accepts(AcceptedCharactersInternal.None))));

            // Act & Assert
            EvaluateData(
                descriptors,
                expectedOutput,
                expectedMarkup,
                Array.Empty<RazorDiagnostic>());
        }

        [Fact]
        public void Rewrite_AllowsRazorMarkupInHtmlComment()
        {
            // Arrange
            IEnumerable<string> allowedChildren = new List<string> { "b" };
            string literal = "asdf";
            string part1 = "Hello ";
            string part2 = "World";
            string commentStart = "<!--";
            string commentEnd = "-->";
            string expectedOutput = $"<p><b>{literal}</b>{commentStart}{part1}@{part2}{commentEnd}</p>";

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

            var factory = new SpanFactory();
            var blockFactory = new BlockFactory(factory);

            var expectedMarkup = new MarkupBlock(
                new MarkupTagHelperBlock("p",
                    blockFactory.MarkupTagBlock("<b>"),
                    factory.Markup(literal),
                    blockFactory.MarkupTagBlock("</b>"),
                    BlockFactory.HtmlCommentBlock(factory, f => new SyntaxTreeNode[] {
                        f.Markup(part1).Accepts(AcceptedCharactersInternal.WhiteSpace),
                         new ExpressionBlock(
                            f.CodeTransition(),
                            f.Code(part2)
                                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                .Accepts(AcceptedCharactersInternal.NonWhiteSpace)) })));

            // Act & Assert
            EvaluateData(
                descriptors,
                expectedOutput,
                expectedMarkup,
                Array.Empty<RazorDiagnostic>());
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
            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock("p",
                    BlockFactory.MarkupTagBlock("</")));
            var expectedErrors = new[]
            {
                RazorDiagnosticFactory.CreateTagHelper_CannotHaveNonTagContent(
                    new SourceSpan(absoluteIndex: 3, lineIndex: 0, characterIndex: 3, length: 2), "p", "custom")
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent, expectedOutput, expectedErrors);
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
            var expectedOutput = new MarkupBlock(
                new MarkupTagHelperBlock("th:p",
                    BlockFactory.MarkupTagBlock("</")));
            var expectedErrors = new[]
            {
                RazorDiagnosticFactory.CreateTagHelper_CannotHaveNonTagContent(
                    new SourceSpan(absoluteIndex: 6, lineIndex: 0, characterIndex: 6, length: 2), "th:p", "custom")
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent, expectedOutput, expectedErrors, "th:");
        }

        [Fact]
        public void Rewrite_CanHandleStartTagOnlyTagTagMode()
        {
            // Arrange
            var documentContent = "<input>";
            var expectedOutput = new MarkupBlock(new MarkupTagHelperBlock("input", TagMode.StartTagOnly));
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
            EvaluateData(descriptors, documentContent, expectedOutput, expectedErrors: new RazorDiagnostic[0]);
        }

        [Fact]
        public void Rewrite_CreatesErrorForWithoutEndTagTagStructureForEndTags()
        {
            // Arrange
            var factory = new SpanFactory();
            var blockFactory = new BlockFactory(factory);
            var expectedError = RazorDiagnosticFactory.CreateParsing_TagHelperMustNotHaveAnEndTag(
                new SourceSpan(filePath: null, absoluteIndex: 2, lineIndex: 0, characterIndex: 2, length: 5),
                "input",
                "InputTagHelper",
                TagStructure.WithoutEndTag);
            var documentContent = "</input>";
            var expectedOutput = new MarkupBlock(blockFactory.MarkupTagBlock("</input>"));
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
            EvaluateData(descriptors, documentContent, expectedOutput, expectedErrors: new[] { expectedError });
        }

        [Fact]
        public void Rewrite_CreatesErrorForInconsistentTagStructures()
        {
            // Arrange
            var factory = new SpanFactory();
            var blockFactory = new BlockFactory(factory);
            var expectedError = RazorDiagnosticFactory.CreateTagHelper_InconsistentTagStructure(
                new SourceSpan(absoluteIndex: 0, lineIndex: 0, characterIndex: 0, length: 7), "InputTagHelper1", "InputTagHelper2", "input");
            var documentContent = "<input>";
            var expectedOutput = new MarkupBlock(new MarkupTagHelperBlock("input", TagMode.StartTagOnly));
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
            EvaluateData(descriptors, documentContent, expectedOutput, expectedErrors: new[] { expectedError });
        }

        public static TheoryData RequiredAttributeData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);
                var dateTimeNow = new Func<int, SyntaxTreeNode>(index =>
                     new MarkupBlock(
                        new MarkupBlock(
                            new DynamicAttributeBlockChunkGenerator(
                                new LocationTagged<string>(
                                    string.Empty,
                                    new SourceLocation(index, 0, index)),
                                new SourceLocation(index, 0, index)),
                            new ExpressionBlock(
                                factory.CodeTransition(),
                                factory.Code("DateTime.Now")
                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                    .Accepts(AcceptedCharactersInternal.NonWhiteSpace)))));

                // documentContent, expectedOutput
                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<p />",
                        new MarkupBlock(blockFactory.MarkupTagBlock("<p />"))
                    },
                    {
                        "<p></p>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<p>"),
                            blockFactory.MarkupTagBlock("</p>"))
                    },
                    {
                        "<div />",
                        new MarkupBlock(blockFactory.MarkupTagBlock("<div />"))
                    },
                    {
                        "<div></div>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<div>"),
                            blockFactory.MarkupTagBlock("</div>"))
                    },
                    {
                        "<p class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                }))
                    },
                    {
                        "<p class=\"@DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", dateTimeNow(10))
                                }))
                    },
                    {
                        "<p class=\"btn\">words and spaces</p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<p class=\"@DateTime.Now\">words and spaces</p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", dateTimeNow(10))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<p class=\"btn\">words<strong>and</strong>spaces</p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                },
                                children: new SyntaxTreeNode[]
                                {
                                    factory.Markup("words"),
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    factory.Markup("and"),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                    factory.Markup("spaces")
                                }))
                    },
                    {
                        "<strong catchAll=\"hi\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("catchAll", factory.Markup("hi"))
                                }))
                    },
                    {
                        "<strong catchAll=\"@DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("catchAll", dateTimeNow(18))
                                }))
                    },
                    {
                        "<strong catchAll=\"hi\">words and spaces</strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("catchAll", factory.Markup("hi"))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<strong catchAll=\"@DateTime.Now\">words and spaces</strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("catchAll", dateTimeNow(18))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<div"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 4, 0, 4),
                                        suffix: new LocationTagged<string>("\"", 15, 0, 15)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 12, 0, 12),
                                            value: new LocationTagged<string>("btn", 12, 0, 12))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                factory.Markup(" />")))
                    },
                    {
                        "<div class=\"btn\"></div>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<div"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 4, 0, 4),
                                        suffix: new LocationTagged<string>("\"", 15, 0, 15)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 12, 0, 12),
                                            value: new LocationTagged<string>("btn", 12, 0, 12))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                factory.Markup(">")),
                            blockFactory.MarkupTagBlock("</div>"))
                    },
                    {
                        "<p notRequired=\"a\" class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("notRequired", factory.Markup("a")),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                }))
                    },
                    {
                        "<p notRequired=\"@DateTime.Now\" class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("notRequired", dateTimeNow(16)),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                }))
                    },
                    {
                        "<p notRequired=\"a\" class=\"btn\">words and spaces</p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("notRequired", factory.Markup("a")),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div style=\"\" class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("style", new MarkupBlock()),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                }))
                    },
                    {
                        "<div style=\"@DateTime.Now\" class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("style", dateTimeNow(12)),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                }))
                    },
                    {
                        "<div style=\"\" class=\"btn\">words and spaces</div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("style", new MarkupBlock()),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div style=\"@DateTime.Now\" class=\"@DateTime.Now\">words and spaces</div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("style", dateTimeNow(12)),
                                    new TagHelperAttributeNode("class", dateTimeNow(34))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div style=\"\" class=\"btn\">words<strong>and</strong>spaces</div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("style", new MarkupBlock()),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                },
                                children: new SyntaxTreeNode[]
                                {
                                    factory.Markup("words"),
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    factory.Markup("and"),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                    factory.Markup("spaces")
                                }))
                    },
                    {
                        "<p class=\"btn\" catchAll=\"hi\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn")),
                                    new TagHelperAttributeNode("catchAll", factory.Markup("hi"))
                                }))
                    },
                    {
                        "<p class=\"btn\" catchAll=\"hi\">words and spaces</p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn")),
                                    new TagHelperAttributeNode("catchAll", factory.Markup("hi"))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div style=\"\" class=\"btn\" catchAll=\"hi\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("style", new MarkupBlock()),
                                    new TagHelperAttributeNode("class", factory.Markup("btn")),
                                    new TagHelperAttributeNode("catchAll", factory.Markup("hi"))
                                }))
                    },
                    {
                        "<div style=\"\" class=\"btn\" catchAll=\"hi\" >words and spaces</div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("style", new MarkupBlock()),
                                    new TagHelperAttributeNode("class", factory.Markup("btn")),
                                    new TagHelperAttributeNode("catchAll", factory.Markup("hi"))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div style=\"\" class=\"btn\" catchAll=\"@@hi\" >words and spaces</div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("style", new MarkupBlock()),
                                    new TagHelperAttributeNode("class", factory.Markup("btn")),
                                    new TagHelperAttributeNode("catchAll",
                                        new MarkupBlock(
                                            new MarkupBlock(
                                                factory.Markup("@").Accepts(AcceptedCharactersInternal.None),
                                                factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                            factory.Markup("hi"))),
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div style=\"@DateTime.Now\" class=\"@DateTime.Now\" catchAll=\"@DateTime.Now\" >words and " +
                        "spaces</div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("style", dateTimeNow(12)),
                                    new TagHelperAttributeNode("class", dateTimeNow(34)),
                                    new TagHelperAttributeNode("catchAll", dateTimeNow(59))
                                },
                                children: factory.Markup("words and spaces")))
                    },
                    {
                        "<div style=\"\" class=\"btn\" catchAll=\"hi\" >words<strong>and</strong>spaces</div>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "div",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("style", new MarkupBlock()),
                                    new TagHelperAttributeNode("class", factory.Markup("btn")),
                                    new TagHelperAttributeNode("catchAll", factory.Markup("hi"))
                                },
                                children: new SyntaxTreeNode[]
                                {
                                    factory.Markup("words"),
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    factory.Markup("and"),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                    factory.Markup("spaces")
                                }))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(RequiredAttributeData))]
        public void Rewrite_RequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly(
            string documentContent,
            object expectedOutput)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
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

            // Act & Assert
            EvaluateData(descriptors, documentContent, (MarkupBlock)expectedOutput, expectedErrors: new RazorDiagnostic[0]);
        }

        public static TheoryData NestedRequiredAttributeData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);
                var dateTimeNow = new MarkupBlock(
                    new MarkupBlock(
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("DateTime.Now")
                                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                .Accepts(AcceptedCharactersInternal.NonWhiteSpace))));

                // documentContent, expectedOutput
                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<p class=\"btn\"><p></p></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                },
                                children: new[]
                                {
                                    blockFactory.MarkupTagBlock("<p>"),
                                    blockFactory.MarkupTagBlock("</p>")
                                }))
                    },
                    {
                        "<strong catchAll=\"hi\"><strong></strong></strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("catchAll", factory.Markup("hi"))
                                },
                                children: new SyntaxTreeNode[]
                                {
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                }))
                    },
                    {
                        "<p class=\"btn\"><strong><p></p></strong></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                },
                                children: new[]
                                {
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    blockFactory.MarkupTagBlock("<p>"),
                                    blockFactory.MarkupTagBlock("</p>"),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                }))
                    },
                    {
                        "<strong catchAll=\"hi\"><p><strong></strong></p></strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("catchAll", factory.Markup("hi"))
                                },
                                children: new SyntaxTreeNode[]
                                {
                                    blockFactory.MarkupTagBlock("<p>"),
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                    blockFactory.MarkupTagBlock("</p>"),
                                }))
                    },
                    {
                        "<p class=\"btn\"><strong catchAll=\"hi\"><p></p></strong></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                },
                                children: new MarkupTagHelperBlock(
                                    "strong",
                                    attributes: new List<TagHelperAttributeNode>
                                    {
                                        new TagHelperAttributeNode("catchAll", factory.Markup("hi"))
                                    },
                                    children: new[]
                                    {
                                        blockFactory.MarkupTagBlock("<p>"),
                                        blockFactory.MarkupTagBlock("</p>")
                                    })))
                    },
                    {
                        "<strong catchAll=\"hi\"><p class=\"btn\"><strong></strong></p></strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("catchAll", factory.Markup("hi"))
                                },
                                children: new MarkupTagHelperBlock(
                                    "p",
                                    attributes: new List<TagHelperAttributeNode>
                                    {
                                        new TagHelperAttributeNode("class", factory.Markup("btn"))
                                    },
                                    children: new[]
                                    {
                                        blockFactory.MarkupTagBlock("<strong>"),
                                        blockFactory.MarkupTagBlock("</strong>"),
                                    })))
                    },
                    {
                        "<p class=\"btn\"><p class=\"btn\"><p></p></p></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                },
                                children: new MarkupTagHelperBlock(
                                    "p",
                                    attributes: new List<TagHelperAttributeNode>
                                    {
                                        new TagHelperAttributeNode("class", factory.Markup("btn"))
                                    },
                                    children: new[]
                                    {
                                        blockFactory.MarkupTagBlock("<p>"),
                                        blockFactory.MarkupTagBlock("</p>")
                                    })))
                    },
                    {
                        "<strong catchAll=\"hi\"><strong catchAll=\"hi\"><strong></strong></strong></strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("catchAll", factory.Markup("hi"))
                                },
                                children: new MarkupTagHelperBlock(
                                    "strong",
                                    attributes: new List<TagHelperAttributeNode>
                                    {
                                        new TagHelperAttributeNode("catchAll", factory.Markup("hi"))
                                    },
                                    children: new[]
                                    {
                                        blockFactory.MarkupTagBlock("<strong>"),
                                        blockFactory.MarkupTagBlock("</strong>"),
                                    })))
                    },
                    {
                        "<p class=\"btn\"><p><p><p class=\"btn\"><p></p></p></p></p></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                },
                                children: new[]
                                {
                                    blockFactory.MarkupTagBlock("<p>"),
                                    blockFactory.MarkupTagBlock("<p>"),
                                    new MarkupTagHelperBlock(
                                        "p",
                                        attributes: new List<TagHelperAttributeNode>
                                        {
                                            new TagHelperAttributeNode("class", factory.Markup("btn"))
                                        },
                                        children: new[]
                                        {
                                            blockFactory.MarkupTagBlock("<p>"),
                                            blockFactory.MarkupTagBlock("</p>")
                                        }),
                                    blockFactory.MarkupTagBlock("</p>"),
                                    blockFactory.MarkupTagBlock("</p>"),
                                }))
                    },
                    {
                        "<strong catchAll=\"hi\"><strong><strong><strong catchAll=\"hi\"><strong></strong></strong>" +
                        "</strong></strong></strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "strong",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("catchAll", factory.Markup("hi"))
                                },
                                children: new[]
                                {
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    blockFactory.MarkupTagBlock("<strong>"),
                                    new MarkupTagHelperBlock(
                                    "strong",
                                    attributes: new List<TagHelperAttributeNode>
                                    {
                                        new TagHelperAttributeNode("catchAll", factory.Markup("hi"))
                                    },
                                    children: new[]
                                    {
                                        blockFactory.MarkupTagBlock("<strong>"),
                                        blockFactory.MarkupTagBlock("</strong>"),
                                    }),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                    blockFactory.MarkupTagBlock("</strong>"),
                                }))
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(NestedRequiredAttributeData))]
        public void Rewrite_NestedRequiredAttributeDescriptorsCreateTagHelperBlocksCorrectly(
            string documentContent,
            object expectedOutput)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
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

            // Act & Assert
            EvaluateData(descriptors, documentContent, (MarkupBlock)expectedOutput, expectedErrors: new RazorDiagnostic[0]);
        }

        public static TheoryData MalformedRequiredAttributeData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorDiagnostic[]>
                {
                    {
                        "<p",
                        new MarkupBlock(blockFactory.MarkupTagBlock("<p")),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<p class=\"btn\"",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
                        }
                    },
                    {
                        "<p notRequired=\"hi\" class=\"btn\"",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("notRequired", factory.Markup("hi")),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
                        }
                    },
                    {
                        "<p></p",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<p>"),
                            blockFactory.MarkupTagBlock("</p")),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<p class=\"btn\"></p",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(17, 0, 17), contentLength: 1), "p")
                        }
                    },
                    {
                        "<p notRequired=\"hi\" class=\"btn\"></p",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("notRequired", factory.Markup("hi")),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(34, 0, 34), contentLength: 1), "p")
                        }
                    },
                    {
                        "<p class=\"btn\" <p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                },
                                children: blockFactory.MarkupTagBlock("<p>"))),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                        }
                    },
                    {
                        "<p notRequired=\"hi\" class=\"btn\" <p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("notRequired", factory.Markup("hi")),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                },
                                children: blockFactory.MarkupTagBlock("<p>"))),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                        }
                    },
                    {
                        "<p class=\"btn\" </p",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(17, 0, 17), contentLength: 1), "p")
                        }
                    },
                    {
                        "<p notRequired=\"hi\" class=\"btn\" </p",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "p",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("notRequired", factory.Markup("hi")),
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                })),
                        new[]
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperMissingCloseAngle(
                                new SourceSpan(new SourceLocation(34, 0, 34), contentLength: 1), "p")
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(MalformedRequiredAttributeData))]
        public void Rewrite_RequiredAttributeDescriptorsCreateMalformedTagHelperBlocksCorrectly(
            string documentContent,
            object expectedOutput,
            object expectedErrors)
        {
            // Arrange
            var descriptors = new TagHelperDescriptor[]
            {
                TagHelperDescriptorBuilder.Create("pTagHelper", "SomeAssembly")
                    .TagMatchingRuleDescriptor(rule =>
                        rule
                        .RequireTagName("p")
                        .RequireAttributeDescriptor(attribute => attribute.Name("class")))
                    .Build(),
            };

            // Act & Assert
            EvaluateData(descriptors, documentContent, (MarkupBlock)expectedOutput, (RazorDiagnostic[])expectedErrors);
        }

        public static TheoryData PrefixedTagHelperBoundData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);
                var availableDescriptorsColon = new TagHelperDescriptor[]
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
                var availableDescriptorsCatchAll = new TagHelperDescriptor[]
                {
                    TagHelperDescriptorBuilder.Create("mythTagHelper", "SomeAssembly")
                        .TagMatchingRuleDescriptor(rule => rule.RequireTagName("*"))
                        .Build(),
                };

                // documentContent, expectedOutput, availableDescriptors
                return new TheoryData<string, MarkupBlock, IEnumerable<TagHelperDescriptor>>
                {
                    {
                        "<th: />",
                        new MarkupBlock(blockFactory.MarkupTagBlock("<th: />")),
                        availableDescriptorsCatchAll
                    },
                    {
                        "<th:>words and spaces</th:>",
                        new MarkupBlock(
                            blockFactory.MarkupTagBlock("<th:>"),
                            factory.Markup("words and spaces"),
                            blockFactory.MarkupTagBlock("</th:>")),
                        availableDescriptorsCatchAll
                    },
                    {
                        "<th:myth />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("th:myth", tagMode: TagMode.SelfClosing)),
                        availableDescriptorsColon
                    },
                    {
                        "<th:myth></th:myth>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("th:myth")),
                        availableDescriptorsColon
                    },
                    {
                        "<th:myth><th:my2th></th:my2th></th:myth>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "th:myth",
                                blockFactory.MarkupTagBlock("<th:my2th>"),
                                blockFactory.MarkupTagBlock("</th:my2th>"))),
                        availableDescriptorsColon
                    },
                    {
                        "<!th:myth />",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "th:myth />")),
                        availableDescriptorsColon
                    },
                    {
                        "<!th:myth></!th:myth>",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "th:myth>"),
                            blockFactory.EscapedMarkupTagBlock("</", "th:myth>")),
                        availableDescriptorsColon
                    },
                    {
                        "<th:myth class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "th:myth",
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                })),
                        availableDescriptorsColon
                    },
                    {
                        "<th:myth2 class=\"btn\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "th:myth2",
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                })),
                        availableDescriptorsColon
                    },
                    {
                        "<th:myth class=\"btn\">words and spaces</th:myth>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "th:myth",
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    new TagHelperAttributeNode("class", factory.Markup("btn"))
                                },
                                children: factory.Markup("words and spaces"))),
                        availableDescriptorsColon
                    },
                    {
                        "<th:myth2 bound=\"@DateTime.Now\" />",
                        new MarkupBlock(
                            new MarkupTagHelperBlock(
                                "th:myth2",
                                tagMode: TagMode.SelfClosing,
                                attributes: new List<TagHelperAttributeNode>
                                {
                                    {
                                        new TagHelperAttributeNode(
                                            "bound",
                                            new MarkupBlock(
                                                new MarkupBlock(
                                                    new ExpressionBlock(
                                                        factory.CodeTransition(),
                                                        factory.Code("DateTime.Now")
                                                            .AsImplicitExpression(CSharpCodeParser.DefaultKeywords, acceptTrailingDot: true)
                                                            .Accepts(AcceptedCharactersInternal.AnyExceptNewline)))))
                                    }
                                })),
                        availableDescriptorsColon
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(PrefixedTagHelperBoundData))]
        public void Rewrite_AllowsPrefixedTagHelpers(
            string documentContent,
            object expectedOutput,
            object availableDescriptors)
        {
            // Act & Assert
            EvaluateData(
                (IEnumerable<TagHelperDescriptor>)availableDescriptors,
                documentContent,
                (MarkupBlock)expectedOutput,
                expectedErrors: Enumerable.Empty<RazorDiagnostic>(),
                tagHelperPrefix: "th:");
        }

        public static TheoryData OptOut_WithAttributeTextTagData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                Func<Func<MarkupBlock>, MarkupBlock> buildStatementBlock = (insideBuilder) =>
                {
                    return new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            insideBuilder(),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                        factory.EmptyHtml());
                };

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorDiagnostic[]>
                {
                    {
                        "@{<!text class=\"btn\">}",
                        new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                                new MarkupBlock(
                                    new MarkupTagBlock(
                                        factory.Markup("<"),
                                        factory.BangEscape(),
                                        factory.Markup("text"),
                                        new MarkupBlock(
                                            new AttributeBlockChunkGenerator(
                                                name: "class",
                                                prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                                suffix: new LocationTagged<string>("\"", 19, 0, 19)),
                                            factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                            factory.Markup("btn").With(
                                                new LiteralAttributeChunkGenerator(
                                                    prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                    value: new LocationTagged<string>("btn", 16, 0, 16))),
                                            factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                        factory.Markup(">").Accepts(AcceptedCharactersInternal.None)),
                                    factory.Markup("}")))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                            RazorDiagnosticFactory.CreateParsing_MissingEndTag(
                                new SourceSpan(filePath: null, absoluteIndex: 3, lineIndex: 0, characterIndex: 3, length: 5), "!text"),
                        }
                    },
                    {
                        "@{<!text class=\"btn\"></!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                            suffix: new LocationTagged<string>("\"", 19, 0, 19)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn", 16, 0, 16))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                    factory.Markup(">").Accepts(AcceptedCharactersInternal.None)),
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharactersInternal.None))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "@{<!text class=\"btn\">words with spaces</!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                            suffix: new LocationTagged<string>("\"", 19, 0, 19)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn", 16, 0, 16))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                    factory.Markup(">").Accepts(AcceptedCharactersInternal.None)),
                                factory.Markup("words with spaces"),
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharactersInternal.None))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "@{<!text class='btn1 btn2' class2=btn></!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class='", 8, 0, 8),
                                            suffix: new LocationTagged<string>("'", 25, 0, 25)),
                                        factory.Markup(" class='").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn1").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn1", 16, 0, 16))),
                                        factory.Markup(" btn2").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(" ", 20, 0, 20),
                                                value: new LocationTagged<string>("btn2", 21, 0, 21))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                        new MarkupBlock(
                                            new AttributeBlockChunkGenerator(
                                                name: "class2",
                                                prefix: new LocationTagged<string>(" class2=", 26, 0, 26),
                                                suffix: new LocationTagged<string>(string.Empty, 37, 0, 37)),
                                            factory.Markup(" class2=").With(SpanChunkGenerator.Null),
                                            factory.Markup("btn").With(
                                                new LiteralAttributeChunkGenerator(
                                                    prefix: new LocationTagged<string>(string.Empty, 34, 0, 34),
                                                    value: new LocationTagged<string>("btn", 34, 0, 34)))),
                                    factory.Markup(">").Accepts(AcceptedCharactersInternal.None)),
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharactersInternal.None))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "@{<!text class='btn1 @DateTime.Now btn2'></!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class='", 8, 0, 8),
                                            suffix: new LocationTagged<string>("'", 39, 0, 39)),
                                        factory.Markup(" class='").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn1").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn1", 16, 0, 16))),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(
                                                new LocationTagged<string>(" ", 20, 0, 20), 21, 0, 21),
                                            factory.Markup(" ").With(SpanChunkGenerator.Null),
                                            new ExpressionBlock(
                                                factory.CodeTransition(),
                                                factory.Code("DateTime.Now")
                                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                    .Accepts(AcceptedCharactersInternal.NonWhiteSpace))),
                                    factory.Markup(" btn2").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(" ", 34, 0, 34),
                                                value: new LocationTagged<string>("btn2", 35, 0, 35))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                    factory.Markup(">").Accepts(AcceptedCharactersInternal.None)),
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharactersInternal.None))),
                        new RazorDiagnostic[0]
                    },
                };
            }
        }

        public static TheoryData OptOut_WithBlockTextTagData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                RazorDiagnostic MissingEndTagError(string tagName)
                {
                    return RazorDiagnosticFactory.CreateParsing_MissingEndTag(
                        new SourceSpan(filePath: null, absoluteIndex: 3, lineIndex: 0, characterIndex: 3, length: tagName.Length), tagName);
                }

                Func<Func<MarkupBlock>, MarkupBlock> buildStatementBlock = (insideBuilder) =>
                {
                    return new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            insideBuilder(),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                        factory.EmptyHtml());
                };

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorDiagnostic[]>
                {
                    {
                        "@{<!text>}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                                new MarkupBlock(
                                    blockFactory.EscapedMarkupTagBlock("<", "text>", AcceptedCharactersInternal.None),
                                    factory.Markup("}")))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                            MissingEndTagError("!text"),
                        }
                    },
                    {
                        "@{</!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharactersInternal.None))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_UnexpectedEndTag(
                                new SourceSpan(filePath: null, absoluteIndex: 4, lineIndex: 0, characterIndex: 4, length: 5), "!text"),
                        }
                    },
                    {
                        "@{<!text></!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "text>", AcceptedCharactersInternal.None),
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharactersInternal.None))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "@{<!text>words and spaces</!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "text>", AcceptedCharactersInternal.None),
                                factory.Markup("words and spaces"),
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharactersInternal.None))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "@{<!text></text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "text>", AcceptedCharactersInternal.None),
                                blockFactory.MarkupTagBlock("</text>", AcceptedCharactersInternal.None))),
                        new []
                        {
                            MissingEndTagError("!text"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(absoluteIndex: 11, lineIndex: 0, characterIndex: 11, length: 4), "text")
                        }
                    },
                    {
                        "@{<text></!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(factory.MarkupTransition("<text>")),
                                new MarkupTagBlock(
                                    factory.Markup("</").Accepts(AcceptedCharactersInternal.None),
                                    factory.BangEscape(),
                                    factory.Markup("text>").Accepts(AcceptedCharactersInternal.None)))),
                        new []
                        {
                            MissingEndTagError("text"),
                        }
                    },
                    {
                        "@{<!text><text></text></!text>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "text>", AcceptedCharactersInternal.None),
                                new MarkupTagHelperBlock("text"),
                                blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharactersInternal.None))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "@{<text><!text></!text>}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                                new MarkupBlock(
                                    new MarkupTagBlock(factory.MarkupTransition("<text>")),
                                    new MarkupTagBlock(
                                        factory.Markup("<").Accepts(AcceptedCharactersInternal.None),
                                        factory.BangEscape(),
                                        factory.Markup("text>").Accepts(AcceptedCharactersInternal.None)),
                                    blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharactersInternal.None),
                                    factory.Markup("}")))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                            MissingEndTagError("text"),
                        }
                    },
                    {
                        "@{<!text></!text></text>}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                                new MarkupBlock(
                                    blockFactory.EscapedMarkupTagBlock("<", "text>", AcceptedCharactersInternal.None),
                                    blockFactory.EscapedMarkupTagBlock("</", "text>", AcceptedCharactersInternal.None)),
                                new MarkupBlock(
                                    blockFactory.MarkupTagBlock("</text>", AcceptedCharactersInternal.None)),
                                factory.EmptyCSharp().AsStatement(),
                                factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                            factory.EmptyHtml()),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_UnexpectedEndTag(
                                new SourceSpan(filePath: null, absoluteIndex: 19, lineIndex: 0, characterIndex: 19, length: 4), "text"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                        new SourceSpan(absoluteIndex: 19, lineIndex: 0, characterIndex: 19, length: 4), "text")
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(OptOut_WithAttributeTextTagData))]
        [MemberData(nameof(OptOut_WithBlockTextTagData))]
        public void Rewrite_AllowsTagHelperElementOptForCompleteTextTagInCSharpBlock(
            string documentContent,
            object expectedOutput,
            object expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, (RazorDiagnostic[])expectedErrors, "p", "text");
        }

        public static TheoryData OptOut_WithPartialTextTagData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                RazorDiagnostic UnfinishedTagError(string tagName, int length)
                {
                    return RazorDiagnosticFactory.CreateParsing_UnfinishedTag(
                        new SourceSpan(filePath: null, absoluteIndex: 3, lineIndex: 0, characterIndex: 3, length: length),
                        tagName);
                }

                Func<Func<MarkupBlock>, MarkupBlock> buildPartialStatementBlock = (insideBuilder) =>
                {
                    return new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            insideBuilder()));
                };

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorDiagnostic[]>
                {
                    {
                        "@{<!text}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(blockFactory.EscapedMarkupTagBlock("<", "text}"))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                            UnfinishedTagError("!text}", 6),
                        }
                    },
                    {
                        "@{<!text /}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock(
                                    "<",
                                    "text /",
                                    new MarkupBlock(factory.Markup("}"))))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                            UnfinishedTagError("!text", 5),
                        }
                    },
                    {
                        "@{<!text class=}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=", 8, 0, 8),
                                            suffix: new LocationTagged<string>(string.Empty, 16, 0, 16)),
                                        factory.Markup(" class=").With(SpanChunkGenerator.Null),
                                        factory.Markup("}").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 15, 0, 15),
                                                value: new LocationTagged<string>("}", 15, 0, 15))))))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                            UnfinishedTagError("!text", 5),
                        }
                    },
                    {
                        "@{<!text class=\"btn}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                            suffix: new LocationTagged<string>(string.Empty, 20, 0, 20)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn}").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn}", 16, 0, 16))))))),
                            new []
                            {
                                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                    new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                                UnfinishedTagError("!text", 5),
                            }
                    },
                    {
                        "@{<!text class=\"btn\"}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),

                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                            suffix: new LocationTagged<string>("\"", 19, 0, 19)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn", 16, 0, 16))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                        new MarkupBlock(factory.Markup("}"))))),
                                new []
                                {
                                    RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                        new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                                    UnfinishedTagError("!text", 5),
                                }
                    },
                    {
                        "@{<!text class=\"btn\" /}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("text"),

                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 8, 0, 8),
                                            suffix: new LocationTagged<string>("\"", 19, 0, 19)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 16, 0, 16),
                                                value: new LocationTagged<string>("btn", 16, 0, 16))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                        factory.Markup(" /"),
                                        new MarkupBlock(factory.Markup("}"))))),
                                new []
                                {
                                    RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                        new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                                    UnfinishedTagError("!text", 5),
                                }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(OptOut_WithPartialTextTagData))]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteTextTagInCSharpBlock(
            string documentContent,
            object expectedOutput,
            object expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, (RazorDiagnostic[])expectedErrors, "text");
        }

        public static TheoryData OptOut_WithPartialData_CSharp
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                RazorDiagnostic UnfinishedTagError(string tagName)
                {
                    return RazorDiagnosticFactory.CreateParsing_UnfinishedTag(
                        new SourceSpan(filePath: null, absoluteIndex: 3, lineIndex: 0, characterIndex: 3, length: tagName.Length),
                        tagName);
                }

                Func<Func<MarkupBlock>, MarkupBlock> buildPartialStatementBlock = (insideBuilder) =>
                {
                    return new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            insideBuilder()));
                };

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorDiagnostic[]>
                {
                    {
                        "@{<!}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(blockFactory.EscapedMarkupTagBlock("<", "}"))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                            UnfinishedTagError("!}"),
                        }
                    },
                    {
                        "@{<!p}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(blockFactory.EscapedMarkupTagBlock("<", "p}"))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                            UnfinishedTagError("!p}"),
                        }
                    },
                    {
                        "@{<!p /}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "p /", new MarkupBlock(factory.Markup("}"))))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                            UnfinishedTagError("!p"),
                        }
                    },
                    {
                        "@{<!p class=}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=", 5, 0, 5),
                                            suffix: new LocationTagged<string>(string.Empty, 13, 0, 13)),
                                        factory.Markup(" class=").With(SpanChunkGenerator.Null),
                                        factory.Markup("}").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 12, 0, 12),
                                                value: new LocationTagged<string>("}", 12, 0, 12))))))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                            UnfinishedTagError("!p"),
                        }
                    },
                    {
                        "@{<!p class=\"btn}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>(string.Empty, 17, 0, 17)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn}").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn}", 13, 0, 13))))))),
                            new []
                            {
                                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                    new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                                UnfinishedTagError("!p"),
                            }
                    },
                    {
                        "@{<!p class=\"btn@@}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>(string.Empty, 19, 0, 19)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn", 13, 0, 13))),
                                        new MarkupBlock(
                                            factory.Markup("@").With(new LiteralAttributeChunkGenerator(new LocationTagged<string>(string.Empty, 16, 0, 16), new LocationTagged<string>("@", 16, 0, 16))).Accepts(AcceptedCharactersInternal.None),
                                            factory.Markup("@").With(SpanChunkGenerator.Null).Accepts(AcceptedCharactersInternal.None)),
                                        factory.Markup("}").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 18, 0, 18),
                                                value: new LocationTagged<string>("}", 18, 0, 18))))))),
                            new []
                            {
                                RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                    new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                                UnfinishedTagError("!p"),
                            }
                    },
                    {
                        "@{<!p class=\"btn\"}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),

                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>("\"", 16, 0, 16)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn", 13, 0, 13))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                        new MarkupBlock(factory.Markup("}"))))),
                                new []
                                {
                                    RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                        new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                                    UnfinishedTagError("!p"),
                                }
                    },
                    {
                        "@{<!p class=\"btn\" /}",
                        buildPartialStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),

                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>("\"", 16, 0, 16)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn", 13, 0, 13))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                        factory.Markup(" /"),
                                        new MarkupBlock(
                                            factory.Markup("}"))))),
                                new []
                                {
                                    RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                       new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                                    UnfinishedTagError("!p"),
                                }
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(OptOut_WithPartialData_CSharp))]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTMLInCSharpBlock(
            string documentContent,
            object expectedOutput,
            object expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, (RazorDiagnostic[])expectedErrors, "strong", "p");
        }

        public static TheoryData OptOut_WithPartialData_HTML
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                // documentContent, expectedOutput
                return new TheoryData<string, MarkupBlock>
                {
                    {
                        "<!",
                        new MarkupBlock(factory.Markup("<!"))
                    },
                    {
                        "<!p",
                        new MarkupBlock(blockFactory.EscapedMarkupTagBlock("<", "p"))
                    },
                    {
                        "<!p /",
                        new MarkupBlock(blockFactory.EscapedMarkupTagBlock("<", "p /"))
                    },
                    {
                        "<!p class=",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=", 3, 0, 3),
                                        suffix: new LocationTagged<string>(string.Empty, 10, 0, 10)),
                                    factory.Markup(" class=").With(SpanChunkGenerator.Null))))
                    },
                    {
                        "<!p class=\"btn",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>(string.Empty, 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))))))
                    },
                    {
                        "<!p class=\"btn\"",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>("\"", 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null))))
                    },
                    {
                        "<!p class=\"btn\" /",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),

                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>("\"", 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                factory.Markup(" /")))
                    }
                };
            }
        }

        [Theory]
        [MemberData(nameof(OptOut_WithPartialData_HTML))]
        public void Rewrite_AllowsTagHelperElementOptForIncompleteHTML(
            string documentContent,
            object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, new RazorDiagnostic[0], "strong", "p");
        }

        public static TheoryData OptOut_WithBlockData_CSharp
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                RazorDiagnostic MissingEndTagError(string tagName, int index = 3)
                {
                    return RazorDiagnosticFactory.CreateParsing_MissingEndTag(
                        new SourceSpan(filePath: null, absoluteIndex: index, lineIndex: 0, characterIndex: index, length: tagName.Length), tagName);
                }

                Func<Func<MarkupBlock>, MarkupBlock> buildStatementBlock = (insideBuilder) =>
                {
                    return new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            insideBuilder(),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                        factory.EmptyHtml());
                };

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorDiagnostic[]>
                {
                    {
                        "@{<!p>}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                                new MarkupBlock(
                                    blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharactersInternal.None),
                                    factory.Markup("}")))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                            MissingEndTagError("!p"),
                        }
                    },
                    {
                        "@{</!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharactersInternal.None))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_UnexpectedEndTag(
                                new SourceSpan(filePath: null, absoluteIndex: 4, lineIndex: 0, characterIndex: 4, length: 2), "!p"),
                        }
                    },
                    {
                        "@{<!p></!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharactersInternal.None),
                                blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharactersInternal.None))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "@{<!p>words and spaces</!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharactersInternal.None),
                                factory.Markup("words and spaces"),
                                blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharactersInternal.None))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "@{<!p></p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharactersInternal.None),
                                blockFactory.MarkupTagBlock("</p>", AcceptedCharactersInternal.None))),
                        new []
                        {
                            MissingEndTagError("!p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(absoluteIndex: 8, lineIndex: 0, characterIndex: 8, length: 1), "p")
                        }
                    },
                    {
                        "@{<p></!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagHelperBlock("p",
                                    blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharactersInternal.None)))),
                        new []
                        {
                            MissingEndTagError("p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(absoluteIndex: 3, lineIndex: 0, characterIndex: 3, length: 1), "p")
                        }
                    },
                    {
                        "@{<p><!p></!p></p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagHelperBlock("p",
                                    blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharactersInternal.None),
                                    blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharactersInternal.None)))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "@{<p><!p></!p>}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                                new MarkupBlock(
                                    new MarkupTagHelperBlock("p",
                                        blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharactersInternal.None),
                                        blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharactersInternal.None),
                                        factory.Markup("}"))))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                            MissingEndTagError("p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(absoluteIndex: 3, lineIndex: 0, characterIndex: 3, length: 1), "p")
                        }
                    },
                    {
                        "@{<!p></!p></p>}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                                new MarkupBlock(
                                    blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharactersInternal.None),
                                    blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharactersInternal.None)),
                                new MarkupBlock(
                                    blockFactory.MarkupTagBlock("</p>", AcceptedCharactersInternal.None)),
                                factory.EmptyCSharp().AsStatement(),
                                factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                            factory.EmptyHtml()),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_UnexpectedEndTag(
                                new SourceSpan(filePath: null, absoluteIndex: 13, lineIndex: 0, characterIndex: 13, length: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(absoluteIndex: 13, lineIndex: 0, characterIndex: 13, length: 1), "p")
                        }
                    },
                    {
                        "@{<strong></!p></strong>}",
                        new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            new MarkupBlock(
                                new MarkupTagHelperBlock("strong",
                                    blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharactersInternal.None))),
                            new MarkupBlock(
                                blockFactory.MarkupTagBlock("</strong>", AcceptedCharactersInternal.None)),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                        factory.EmptyHtml()),
                        new []
                        {
                            MissingEndTagError("strong"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(absoluteIndex: 3, lineIndex: 0, characterIndex: 3, length: 6), "strong"),
                            RazorDiagnosticFactory.CreateParsing_UnexpectedEndTag(
                                new SourceSpan(filePath: null, absoluteIndex: 17, lineIndex: 0, characterIndex: 17, length: 6), "strong"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(absoluteIndex: 17, lineIndex: 0, characterIndex: 17, length: 6), "strong")
                        }
                    },
                    {
                        "@{<strong></strong><!p></!p>}",
                        new MarkupBlock(
                            factory.EmptyHtml(),
                            new StatementBlock(
                                factory.CodeTransition(),
                                factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                                new MarkupBlock(
                                    new MarkupTagHelperBlock("strong")),
                                new MarkupBlock(
                                    blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharactersInternal.None),
                                    blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharactersInternal.None)),
                                factory.EmptyCSharp().AsStatement(),
                                factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                            factory.EmptyHtml()),
                        new RazorDiagnostic[0]
                    },
                    {
                        "@{<p><strong></!strong><!p></strong></!p>}",
                            new MarkupBlock(
                                factory.EmptyHtml(),
                                new StatementBlock(
                                    factory.CodeTransition(),
                                    factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                                    new MarkupBlock(
                                        new MarkupTagHelperBlock("p",
                                            new MarkupTagHelperBlock("strong",
                                                blockFactory.EscapedMarkupTagBlock("</", "strong>", AcceptedCharactersInternal.None)))),
                                    new MarkupBlock(
                                        blockFactory.EscapedMarkupTagBlock("<", "p>", AcceptedCharactersInternal.None),
                                        blockFactory.MarkupTagBlock("</strong>", AcceptedCharactersInternal.None)),
                                    new MarkupBlock(
                                        blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharactersInternal.None)),
                                    factory.EmptyCSharp().AsStatement(),
                                    factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                                factory.EmptyHtml()),
                        new []
                        {
                            MissingEndTagError("p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(absoluteIndex: 3, lineIndex: 0, characterIndex: 3, length: 1), "p"),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(absoluteIndex: 6, lineIndex: 0, characterIndex: 6, length: 6), "strong"),
                            MissingEndTagError("!p", index: 24),
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(absoluteIndex: 29, lineIndex: 0, characterIndex: 29, length: 6), "strong"),
                            RazorDiagnosticFactory.CreateParsing_UnexpectedEndTag(
                                new SourceSpan(filePath: null, absoluteIndex: 38, lineIndex: 0, characterIndex: 38, length: 2), "!p"),
                        }
                    },
                };
            }
        }

        public static TheoryData OptOut_WithAttributeData_CSharp
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                Func<Func<MarkupBlock>, MarkupBlock> buildStatementBlock = (insideBuilder) =>
                {
                    return new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            insideBuilder(),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                        factory.EmptyHtml());
                };

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorDiagnostic[]>
                {
                    {
                        "@{<!p class=\"btn\">}",
                        new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                                new MarkupBlock(
                                    new MarkupTagBlock(
                                        factory.Markup("<"),
                                        factory.BangEscape(),
                                        factory.Markup("p"),
                                        new MarkupBlock(
                                            new AttributeBlockChunkGenerator(
                                                name: "class",
                                                prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                                suffix: new LocationTagged<string>("\"", 16, 0, 16)),
                                            factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                            factory.Markup("btn").With(
                                                new LiteralAttributeChunkGenerator(
                                                    prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                    value: new LocationTagged<string>("btn", 13, 0, 13))),
                                            factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                        factory.Markup(">").Accepts(AcceptedCharactersInternal.None)),
                                    factory.Markup("}")))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_ExpectedEndOfBlockBeforeEOF(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), Resources.BlockName_Code, "}", "{"),
                            RazorDiagnosticFactory.CreateParsing_MissingEndTag(
                                new SourceSpan(filePath: null, absoluteIndex: 3, lineIndex: 0, characterIndex: 3, length: 2), "!p"),
                        }
                    },
                    {
                        "@{<!p class=\"btn\"></!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>("\"", 16, 0, 16)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn", 13, 0, 13))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                    factory.Markup(">").Accepts(AcceptedCharactersInternal.None)),
                                blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharactersInternal.None))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "@{<!p class=\"btn\">words with spaces</!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class=\"", 5, 0, 5),
                                            suffix: new LocationTagged<string>("\"", 16, 0, 16)),
                                        factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn", 13, 0, 13))),
                                        factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                    factory.Markup(">").Accepts(AcceptedCharactersInternal.None)),
                                factory.Markup("words with spaces"),
                                blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharactersInternal.None))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "@{<!p class='btn1 btn2' class2=btn></!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class='", 5, 0, 5),
                                            suffix: new LocationTagged<string>("'", 22, 0, 22)),
                                        factory.Markup(" class='").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn1").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn1", 13, 0, 13))),
                                        factory.Markup(" btn2").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(" ", 17, 0, 17),
                                                value: new LocationTagged<string>("btn2", 18, 0, 18))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                        new MarkupBlock(
                                            new AttributeBlockChunkGenerator(
                                                name: "class2",
                                                prefix: new LocationTagged<string>(" class2=", 23, 0, 23),
                                                suffix: new LocationTagged<string>(string.Empty, 34, 0, 34)),
                                            factory.Markup(" class2=").With(SpanChunkGenerator.Null),
                                            factory.Markup("btn").With(
                                                new LiteralAttributeChunkGenerator(
                                                    prefix: new LocationTagged<string>(string.Empty, 31, 0, 31),
                                                    value: new LocationTagged<string>("btn", 31, 0, 31)))),
                                    factory.Markup(">").Accepts(AcceptedCharactersInternal.None)),
                                blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharactersInternal.None))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "@{<!p class='btn1 @DateTime.Now btn2'></!p>}",
                        buildStatementBlock(
                            () => new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.Markup("<"),
                                    factory.BangEscape(),
                                    factory.Markup("p"),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class",
                                            prefix: new LocationTagged<string>(" class='", 5, 0, 5),
                                            suffix: new LocationTagged<string>("'", 36, 0, 36)),
                                        factory.Markup(" class='").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn1").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 13, 0, 13),
                                                value: new LocationTagged<string>("btn1", 13, 0, 13))),
                                        new MarkupBlock(
                                            new DynamicAttributeBlockChunkGenerator(
                                                new LocationTagged<string>(" ", 17, 0, 17), 18, 0, 18),
                                            factory.Markup(" ").With(SpanChunkGenerator.Null),
                                            new ExpressionBlock(
                                                factory.CodeTransition(),
                                                factory.Code("DateTime.Now")
                                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                    .Accepts(AcceptedCharactersInternal.NonWhiteSpace))),
                                    factory.Markup(" btn2").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(" ", 31, 0, 31),
                                                value: new LocationTagged<string>("btn2", 32, 0, 32))),
                                        factory.Markup("'").With(SpanChunkGenerator.Null)),
                                    factory.Markup(">").Accepts(AcceptedCharactersInternal.None)),
                                blockFactory.EscapedMarkupTagBlock("</", "p>", AcceptedCharactersInternal.None))),
                        new RazorDiagnostic[0]
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(OptOut_WithBlockData_CSharp))]
        [MemberData(nameof(OptOut_WithAttributeData_CSharp))]
        public void Rewrite_AllowsTagHelperElementOptOutCSharp(
            string documentContent,
            object expectedOutput,
            object expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, (RazorDiagnostic[])expectedErrors, "strong", "p");
        }

        public static TheoryData OptOut_WithBlockData_HTML
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorDiagnostic[]>
                {
                    {
                        "<!p>",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "p>")),
                        new RazorDiagnostic[0]
                    },
                    {
                        "</!p>",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<!p></!p>",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "p>"),
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<!p>words and spaces</!p>",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "p>"),
                            factory.Markup("words and spaces"),
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<!p></p>",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "p>"),
                            blockFactory.MarkupTagBlock("</p>")),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(absoluteIndex: 6, lineIndex: 0, characterIndex: 6, length: 1), "p")
                        }
                    },
                    {
                        "<p></!p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p", blockFactory.EscapedMarkupTagBlock("</", "p>"))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
                        }
                    },
                    {
                        "<p><!p></!p></p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                blockFactory.EscapedMarkupTagBlock("<", "p>"),
                                blockFactory.EscapedMarkupTagBlock("</", "p>"))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<p><!p></!p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                blockFactory.EscapedMarkupTagBlock("<", "p>"),
                                blockFactory.EscapedMarkupTagBlock("</", "p>"))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
                        }
                    },
                    {
                        "<!p></!p></p>",
                        new MarkupBlock(
                            blockFactory.EscapedMarkupTagBlock("<", "p>"),
                            blockFactory.EscapedMarkupTagBlock("</", "p>"),
                            blockFactory.MarkupTagBlock("</p>")),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(11, 0, 11), contentLength: 1), "p")
                        }
                    },
                    {
                        "<strong></!p></strong>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("strong",
                                blockFactory.EscapedMarkupTagBlock("</", "p>"))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<strong></strong><!p></!p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("strong"),
                            blockFactory.EscapedMarkupTagBlock("<", "p>"),
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<p><strong></!strong><!p></strong></!p>",
                        new MarkupBlock(
                            new MarkupTagHelperBlock("p",
                                new MarkupTagHelperBlock("strong",
                                    blockFactory.EscapedMarkupTagBlock("</", "strong>"),
                                    blockFactory.EscapedMarkupTagBlock("<", "p>")),
                                blockFactory.EscapedMarkupTagBlock("</", "p>"))),
                        new []
                        {
                            RazorDiagnosticFactory.CreateParsing_TagHelperFoundMalformedTagHelper(
                                new SourceSpan(new SourceLocation(1, 0, 1), contentLength: 1), "p")
                        }
                    },
                };
            }
        }

        public static TheoryData OptOut_WithAttributeData_HTML
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                // documentContent, expectedOutput, expectedErrors
                return new TheoryData<string, MarkupBlock, RazorDiagnostic[]>
                {
                    {
                        "<!p class=\"btn\">",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>("\"", 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                factory.Markup(">"))),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<!p class=\"btn\"></!p>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>("\"", 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                factory.Markup(">")),
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<!p class=\"btn\">words and spaces</!p>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class=\"", 3, 0, 3),
                                        suffix: new LocationTagged<string>("\"", 14, 0, 14)),
                                    factory.Markup(" class=\"").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn", 11, 0, 11))),
                                    factory.Markup("\"").With(SpanChunkGenerator.Null)),
                                factory.Markup(">")),
                            factory.Markup("words and spaces"),
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<!p class='btn1 btn2' class2=btn></!p>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class='", 3, 0, 3),
                                        suffix: new LocationTagged<string>("'", 20, 0, 20)),
                                    factory.Markup(" class='").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn1").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn1", 11, 0, 11))),
                                    factory.Markup(" btn2").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(" ", 15, 0, 15),
                                            value: new LocationTagged<string>("btn2", 16, 0, 16))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                    new MarkupBlock(
                                        new AttributeBlockChunkGenerator(
                                            name: "class2",
                                            prefix: new LocationTagged<string>(" class2=", 21, 0, 21),
                                            suffix: new LocationTagged<string>(string.Empty, 32, 0, 32)),
                                        factory.Markup(" class2=").With(SpanChunkGenerator.Null),
                                        factory.Markup("btn").With(
                                            new LiteralAttributeChunkGenerator(
                                                prefix: new LocationTagged<string>(string.Empty, 29, 0, 29),
                                                value: new LocationTagged<string>("btn", 29, 0, 29)))),
                                factory.Markup(">")),
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorDiagnostic[0]
                    },
                    {
                        "<!p class='btn1 @DateTime.Now btn2'></!p>",
                        new MarkupBlock(
                            new MarkupTagBlock(
                                factory.Markup("<"),
                                factory.BangEscape(),
                                factory.Markup("p"),
                                new MarkupBlock(
                                    new AttributeBlockChunkGenerator(
                                        name: "class",
                                        prefix: new LocationTagged<string>(" class='", 3, 0, 3),
                                        suffix: new LocationTagged<string>("'", 34, 0, 34)),
                                    factory.Markup(" class='").With(SpanChunkGenerator.Null),
                                    factory.Markup("btn1").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(string.Empty, 11, 0, 11),
                                            value: new LocationTagged<string>("btn1", 11, 0, 11))),
                                    new MarkupBlock(
                                        new DynamicAttributeBlockChunkGenerator(
                                            new LocationTagged<string>(" ", 15, 0, 15), 16, 0, 16),
                                        factory.Markup(" ").With(SpanChunkGenerator.Null),
                                        new ExpressionBlock(
                                            factory.CodeTransition(),
                                            factory.Code("DateTime.Now")
                                                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                                .Accepts(AcceptedCharactersInternal.NonWhiteSpace))),
                                factory.Markup(" btn2").With(
                                        new LiteralAttributeChunkGenerator(
                                            prefix: new LocationTagged<string>(" ", 29, 0, 29),
                                            value: new LocationTagged<string>("btn2", 30, 0, 30))),
                                    factory.Markup("'").With(SpanChunkGenerator.Null)),
                                factory.Markup(">")),
                            blockFactory.EscapedMarkupTagBlock("</", "p>")),
                        new RazorDiagnostic[0]
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(OptOut_WithBlockData_HTML))]
        [MemberData(nameof(OptOut_WithAttributeData_HTML))]
        public void Rewrite_AllowsTagHelperElementOptOutHTML(
            string documentContent,
            object expectedOutput,
            object expectedErrors)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, (RazorDiagnostic[])expectedErrors, "strong", "p");
        }

        public static IEnumerable<object[]> TextTagsBlockData
        {
            get
            {
                var factory = new SpanFactory();

                // Should re-write text tags that aren't in C# blocks
                yield return new object[]
                {
                    "<text>Hello World</text>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("text",
                            factory.Markup("Hello World")))
                };
                yield return new object[]
                {
                    "@{<text>Hello World</text>}",
                    new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.MarkupTransition("<text>")),
                                factory.Markup("Hello World").Accepts(AcceptedCharactersInternal.None),
                                new MarkupTagBlock(
                                    factory.MarkupTransition("</text>"))),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                        factory.EmptyHtml())
                };
                yield return new object[]
                {
                    "@{<text><p>Hello World</p></text>}",
                    new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            new MarkupBlock(
                                new MarkupTagBlock(
                                    factory.MarkupTransition("<text>")),
                                new MarkupTagHelperBlock("p",
                                    factory.Markup("Hello World")),
                                new MarkupTagBlock(
                                    factory.MarkupTransition("</text>"))),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                        factory.EmptyHtml())
                };
                yield return new object[]
                {
                    "@{<p><text>Hello World</text></p>}",
                    new MarkupBlock(
                        factory.EmptyHtml(),
                        new StatementBlock(
                            factory.CodeTransition(),
                            factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                            new MarkupBlock(
                                new MarkupTagHelperBlock("p",
                                    new MarkupTagHelperBlock("text",
                                        factory.Markup("Hello World")))),
                            factory.EmptyCSharp().AsStatement(),
                            factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                        factory.EmptyHtml())
                };
            }
        }

        [Theory]
        [MemberData(nameof(TextTagsBlockData))]
        public void TagHelperParseTreeRewriter_DoesNotRewriteTextTagTransitionTagHelpers(
            string documentContent,
            object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, "p", "text");
        }

        public static IEnumerable<object[]> SpecialTagsBlockData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);
                yield return new object[]
                {
                    "<foo><!-- Hello World --></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        blockFactory.HtmlCommentBlock (" Hello World "),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[]
                {
                    "<foo><!-- @foo --></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        BlockFactory.HtmlCommentBlock(factory, f=> new SyntaxTreeNode[]{
                            f.Markup(" ").Accepts(AcceptedCharactersInternal.WhiteSpace),
                            new ExpressionBlock(
                                f.CodeTransition(),
                                f.Code("foo")
                                    .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                    .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                            factory.Markup(" ").Accepts(AcceptedCharactersInternal.WhiteSpace) }),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[]
                {
                    "<foo><?xml Hello World ?></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<?xml Hello World ?>"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[]
                {
                    "<foo><?xml @foo ?></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<?xml "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Markup(" ?>"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[]
                {
                    "<foo><!DOCTYPE @foo ></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<!DOCTYPE "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo")
                                   .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                   .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Markup(" >"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[]
                {
                    "<foo><!DOCTYPE hello=\"world\" ></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<!DOCTYPE hello=\"world\" >"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[]
                {
                    "<foo><![CDATA[ Hello World ]]></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<![CDATA[ Hello World ]]>"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
                yield return new object[]
                {
                    "<foo><![CDATA[ @foo ]]></foo>",
                    new MarkupBlock(
                        new MarkupTagBlock(
                            factory.Markup("<foo>")),
                        factory.Markup("<![CDATA[ "),
                        new ExpressionBlock(
                            factory.CodeTransition(),
                            factory.Code("foo")
                                .AsImplicitExpression(CSharpCodeParser.DefaultKeywords)
                                .Accepts(AcceptedCharactersInternal.NonWhiteSpace)),
                        factory.Markup(" ]]>"),
                        new MarkupTagBlock(
                            factory.Markup("</foo>")))
                };
            }
        }

        [Theory]
        [MemberData(nameof(SpecialTagsBlockData))]
        public void TagHelperParseTreeRewriter_DoesNotRewriteSpecialTagTagHelpers(
            string documentContent,
            object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, "!--", "?xml", "![CDATA[", "!DOCTYPE");
        }

        public static IEnumerable<object[]> NestedBlockData
        {
            get
            {
                var factory = new SpanFactory();
                var blockFactory = new BlockFactory(factory);

                yield return new object[]
                {
                    "<p><div></div></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            new MarkupTagHelperBlock("div")))
                };
                yield return new object[]
                {
                    "<p>Hello World <div></div></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hello World "),
                            new MarkupTagHelperBlock("div")))
                };
                yield return new object[]
                {
                    "<p>Hel<p>lo</p></p> <p><div>World</div></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hel"),
                            new MarkupTagHelperBlock("p",
                                factory.Markup("lo"))),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            new MarkupTagHelperBlock("div",
                                factory.Markup("World"))))
                };
                yield return new object[]
                {
                    "<p>Hel<strong>lo</strong></p> <p><span>World</span></p>",
                    new MarkupBlock(
                        new MarkupTagHelperBlock("p",
                            factory.Markup("Hel"),
                            blockFactory.MarkupTagBlock("<strong>"),
                            factory.Markup("lo"),
                            blockFactory.MarkupTagBlock("</strong>")),
                        factory.Markup(" "),
                        new MarkupTagHelperBlock("p",
                            blockFactory.MarkupTagBlock("<span>"),
                            factory.Markup("World"),
                            blockFactory.MarkupTagBlock("</span>")))
                };
            }
        }

        [Theory]
        [MemberData(nameof(NestedBlockData))]
        public void TagHelperParseTreeRewriter_RewritesNestedTagHelperTagBlocks(
            string documentContent,
            object expectedOutput)
        {
            RunParseTreeRewriterTest(documentContent, (MarkupBlock)expectedOutput, "p", "div");
        }

        [Fact]
        public void Rewrite_HandlesMalformedNestedNonTagHelperTags_Correctly()
        {
            var documentContent = "<div>@{</div>}";
            var expectedOutput = new MarkupBlock(
                new MarkupTagBlock(
                    Factory.Markup("<div>")),
                new StatementBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode("{").Accepts(AcceptedCharactersInternal.None),
                        new MarkupBlock(
                            new MarkupTagBlock(
                                Factory.Markup("</div>").Accepts(AcceptedCharactersInternal.None))),
                    Factory.EmptyCSharp().AsStatement(),
                    Factory.MetaCode("}").Accepts(AcceptedCharactersInternal.None)),
                Factory.EmptyHtml());
            var expectedErrors = new[]
            {
                RazorDiagnosticFactory.CreateParsing_UnexpectedEndTag(
                    new SourceSpan(new SourceLocation(9, 0, 9), contentLength: 3), "div"),
            };

            RunParseTreeRewriterTest(documentContent, expectedOutput, expectedErrors);
        }
    }
}