// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Parser;
using Microsoft.AspNetCore.Razor.Parser.Internal;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class MvcRazorCodeParserTest
    {
        public static TheoryData ViewImportsData
        {
            get
            {
                // chunkTrees, expectedDirectiveDescriptors
                return new TheoryData<ChunkTree[], TagHelperDirectiveDescriptor[]>
                {
                    {
                        new[] { CreateChunkTree(new TagHelperPrefixDirectiveChunk { Prefix = "THP" }) },
                        new[] { CreateDirectiveDescriptor("THP", TagHelperDirectiveType.TagHelperPrefix) }
                    },
                    {
                        new[] { CreateChunkTree(new AddTagHelperChunk { LookupText = "ATH" }) },
                        new[] { CreateDirectiveDescriptor("ATH", TagHelperDirectiveType.AddTagHelper) }
                    },
                    {
                        new[]
                        {
                            CreateChunkTree(
                                new AddTagHelperChunk { LookupText = "ATH1" },
                                new AddTagHelperChunk { LookupText = "ATH2" })
                        },
                        new[]
                        {
                            CreateDirectiveDescriptor("ATH1", TagHelperDirectiveType.AddTagHelper),
                            CreateDirectiveDescriptor("ATH2", TagHelperDirectiveType.AddTagHelper)
                        }
                    },
                    {
                        new[] { CreateChunkTree(new RemoveTagHelperChunk { LookupText = "RTH" }) },
                        new[] { CreateDirectiveDescriptor("RTH", TagHelperDirectiveType.RemoveTagHelper) }
                    },
                    {
                        new[]
                        {
                            CreateChunkTree(
                                new RemoveTagHelperChunk { LookupText = "RTH1" },
                                new RemoveTagHelperChunk { LookupText = "RTH2" })
                        },
                        new[]
                        {
                            CreateDirectiveDescriptor("RTH1", TagHelperDirectiveType.RemoveTagHelper),
                            CreateDirectiveDescriptor("RTH2", TagHelperDirectiveType.RemoveTagHelper)
                        }
                    },
                    {
                        new[]
                        {
                            CreateChunkTree(new TagHelperPrefixDirectiveChunk { Prefix = "THP2" }),
                            CreateChunkTree(new TagHelperPrefixDirectiveChunk { Prefix = "THP1" }),
                        },
                        new[] { CreateDirectiveDescriptor("THP1", TagHelperDirectiveType.TagHelperPrefix) }
                    },
                    {
                        new[]
                        {
                            CreateChunkTree(
                                new TagHelperPrefixDirectiveChunk { Prefix = "THP" },
                                new RemoveTagHelperChunk { LookupText = "RTH" },
                                new AddTagHelperChunk { LookupText = "ATH" })
                        },
                        new[]
                        {
                            CreateDirectiveDescriptor("RTH", TagHelperDirectiveType.RemoveTagHelper),
                            CreateDirectiveDescriptor("ATH", TagHelperDirectiveType.AddTagHelper),
                            CreateDirectiveDescriptor("THP", TagHelperDirectiveType.TagHelperPrefix),
                        }
                    },
                    {
                        new[]
                        {
                            CreateChunkTree(new RemoveTagHelperChunk { LookupText = "RTH" }),
                            CreateChunkTree(
                                new LiteralChunk { Text = "Hello world" },
                                new AddTagHelperChunk { LookupText = "ATH" }),
                        },
                        new[]
                        {
                            CreateDirectiveDescriptor("RTH", TagHelperDirectiveType.RemoveTagHelper),
                            CreateDirectiveDescriptor("ATH", TagHelperDirectiveType.AddTagHelper),
                        }
                    },
                    {
                        new[]
                        {
                            CreateChunkTree(new RemoveTagHelperChunk { LookupText = "RTH" }),
                            CreateChunkTree(
                                new LiteralChunk { Text = "Hello world" },
                                new AddTagHelperChunk { LookupText = "ATH" }),
                            CreateChunkTree(new TagHelperPrefixDirectiveChunk { Prefix = "THP" }),
                        },
                        new[]
                        {
                            CreateDirectiveDescriptor("RTH", TagHelperDirectiveType.RemoveTagHelper),
                            CreateDirectiveDescriptor("ATH", TagHelperDirectiveType.AddTagHelper),
                            CreateDirectiveDescriptor("THP", TagHelperDirectiveType.TagHelperPrefix),
                        }
                    },
                    {
                        new[]
                        {
                            CreateChunkTree(new TagHelperPrefixDirectiveChunk { Prefix = "THP2" }),
                            CreateChunkTree(new RemoveTagHelperChunk { LookupText = "RTH" }),
                            CreateChunkTree(new AddTagHelperChunk { LookupText = "ATH" }),
                            CreateChunkTree(new TagHelperPrefixDirectiveChunk { Prefix = "THP1" }),
                        },
                        new[]
                        {
                            CreateDirectiveDescriptor("RTH", TagHelperDirectiveType.RemoveTagHelper),
                            CreateDirectiveDescriptor("ATH", TagHelperDirectiveType.AddTagHelper),
                            CreateDirectiveDescriptor("THP1", TagHelperDirectiveType.TagHelperPrefix),
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(ViewImportsData))]
        public void GetTagHelperDescriptors_ReturnsExpectedDirectiveDescriptors(
            ChunkTree[] chunkTrees,
            TagHelperDirectiveDescriptor[] expectedDirectiveDescriptors)
        {
            // Arrange
            var builder = new BlockBuilder { Type = BlockType.Comment };
            var block = new Block(builder);

            IList<TagHelperDirectiveDescriptor> descriptors = null;
            var resolver = new Mock<ITagHelperDescriptorResolver>();
            resolver.Setup(r => r.Resolve(It.IsAny<TagHelperDescriptorResolutionContext>()))
                    .Callback((TagHelperDescriptorResolutionContext context) =>
                    {
                        descriptors = context.DirectiveDescriptors;
                    })
                    .Returns(Enumerable.Empty<TagHelperDescriptor>())
                    .Verifiable();

            var baseParser = new RazorParser(
                new CSharpCodeParser(),
                new HtmlMarkupParser(),
                tagHelperDescriptorResolver: resolver.Object);
            var parser = new TestableMvcRazorParser(baseParser, chunkTrees, defaultInheritedChunks: new Chunk[0]);

            // Act
            parser.GetTagHelperDescriptorsPublic(block, errorSink: new ErrorSink()).ToArray();

            // Assert
            Assert.NotNull(descriptors);
            Assert.Equal(expectedDirectiveDescriptors.Length, descriptors.Count);

            for (var i = 0; i < expectedDirectiveDescriptors.Length; i++)
            {
                var expected = expectedDirectiveDescriptors[i];
                var actual = descriptors[i];

                Assert.Equal(expected.DirectiveText, actual.DirectiveText, StringComparer.Ordinal);
                Assert.Equal(SourceLocation.Zero, actual.Location);
                Assert.Equal(expected.DirectiveType, actual.DirectiveType);
            }
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("   ;  ", "")]
        [InlineData("    ", "")]
        [InlineData(";;", "")]
        [InlineData("a", "a")]
        [InlineData("a;", "a")]
        [InlineData("abcd", "abcd")]
        [InlineData("abc;d", "abc;d")]
        [InlineData("a bc d", "a bc d")]
        [InlineData("a\t\tbc\td\t", "a\t\tbc\td")]
        [InlineData("abc;", "abc")]
        [InlineData("  abc;", "abc")]
        [InlineData("\tabc;", "abc")]
        [InlineData(";; abc;", ";; abc")]
        [InlineData(";;\tabc;", ";;\tabc")]
        [InlineData("\t;;abc;", ";;abc")]
        [InlineData("abc;; ;", "abc")]
        [InlineData("abc;;\t;", "abc")]
        [InlineData("\tabc  \t;", "abc")]
        [InlineData("abc;;\r\n;", "abc")]
        [InlineData("abcd \n", "abcd")]
        [InlineData("\r\n\r  \n\t  abcd \t \t \n  \r\n", "abcd")]
        [InlineData("pqrs\r", "pqrs")]
        public void RemoveWhitespaceAndTrailingSemicolons_ReturnsExpectedValues(string input, string expectedOutput)
        {
            // Arrange and Act
            var output = MvcRazorCodeParser.RemoveWhitespaceAndTrailingSemicolons(input);

            // Assert
            Assert.Equal(expectedOutput, output, StringComparer.Ordinal);
        }

        private static ChunkTree CreateChunkTree(params Chunk[] chunks)
        {
            return new ChunkTree
            {
                Children = chunks
            };
        }

        private static TagHelperDirectiveDescriptor CreateDirectiveDescriptor(
            string directiveText,
            TagHelperDirectiveType directiveType)
        {
            return new TagHelperDirectiveDescriptor
            {
                DirectiveText = directiveText,
                Location = SourceLocation.Undefined,
                DirectiveType = directiveType
            };
        }

        private class TestableMvcRazorParser : MvcRazorParser
        {
            public TestableMvcRazorParser(
                RazorParser parser,
                IReadOnlyList<ChunkTree> chunkTrees,
                IReadOnlyList<Chunk> defaultInheritedChunks)
                : base(parser, chunkTrees, defaultInheritedChunks, typeof(ModelExpression).FullName)
            {
            }

            public IEnumerable<TagHelperDescriptor> GetTagHelperDescriptorsPublic(
                Block documentRoot,
                ErrorSink errorSink)
            {
                return GetTagHelperDescriptors(documentRoot, errorSink);
            }
        }
    }
}