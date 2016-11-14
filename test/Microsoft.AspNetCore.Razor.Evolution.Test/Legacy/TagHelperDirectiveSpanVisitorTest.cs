// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Internal;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    public class TagHelperDirectiveSpanVisitorTest
    {
        public static TheoryData QuotedTagHelperDirectivesData
        {
            get
            {
                var factory = new SpanFactory();

                // document, expectedDescriptors
                return new TheoryData<MarkupBlock, IEnumerable<TagHelperDirectiveDescriptor>>
                {
                    {
                        new MarkupBlock(factory.Code("\"*, someAssembly\"").AsAddTagHelper("*, someAssembly")),
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "*, someAssembly",
                                DirectiveType = TagHelperDirectiveType.AddTagHelper
                            },
                        }
                    },
                    {
                        new MarkupBlock(factory.Code("\"*, someAssembly\"").AsRemoveTagHelper("*, someAssembly")),
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "*, someAssembly",
                                DirectiveType = TagHelperDirectiveType.RemoveTagHelper
                            },
                        }
                    },
                    {
                        new MarkupBlock(factory.Code("\"th:\"").AsTagHelperPrefixDirective("th:")),
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "th:",
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            },
                        }
                    },
                    {
                        new MarkupBlock(factory.Code("   \"*, someAssembly  \"  ").AsAddTagHelper("*, someAssembly  ")),
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "*, someAssembly",
                                DirectiveType = TagHelperDirectiveType.AddTagHelper
                            },
                        }
                    },
                    {
                        new MarkupBlock(factory.Code("   \"*, someAssembly  \"  ").AsRemoveTagHelper("*, someAssembly  ")),
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "*, someAssembly",
                                DirectiveType = TagHelperDirectiveType.RemoveTagHelper
                            },
                        }
                    },
                    {
                        new MarkupBlock(factory.Code("   \"  th  :\"").AsTagHelperPrefixDirective(" th  :")),
                        new[]
                        {
                            new TagHelperDirectiveDescriptor
                            {
                                DirectiveText = "th  :",
                                DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                            },
                        }
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(QuotedTagHelperDirectivesData))]
        public void GetDescriptors_LocatesQuotedTagHelperDirectives_CreatesDirectiveDescriptors(
            object document,
            object expectedDescriptors)
        {
            // Arrange
            var resolver = new TestTagHelperDescriptorResolver();
            var tagHelperDirectiveSpanVisitor = new TagHelperDirectiveSpanVisitor(resolver, new ErrorSink());

            // Act
            tagHelperDirectiveSpanVisitor.GetDescriptors((MarkupBlock)document);

            // Assert
            Assert.Equal(
                (IEnumerable<TagHelperDirectiveDescriptor>)expectedDescriptors,
                resolver.DirectiveDescriptors,
                TagHelperDirectiveDescriptorComparer.Default);
        }

        [Fact]
        public void GetDescriptors_InvokesResolveOnceForAllDirectives()
        {
            // Arrange
            var factory = new SpanFactory();
            var resolver = new Mock<ITagHelperDescriptorResolver>();
            resolver.Setup(mock => mock.Resolve(It.IsAny<TagHelperDescriptorResolutionContext>()))
                    .Returns(Enumerable.Empty<TagHelperDescriptor>());
            var tagHelperDirectiveSpanVisitor = new TagHelperDirectiveSpanVisitor(
                resolver.Object,
                new ErrorSink());
            var document = new MarkupBlock(
                factory.Code("one").AsAddTagHelper("one"),
                factory.Code("two").AsRemoveTagHelper("two"),
                factory.Code("three").AsRemoveTagHelper("three"),
                factory.Code("four").AsTagHelperPrefixDirective("four"));

            // Act
            tagHelperDirectiveSpanVisitor.GetDescriptors(document);

            // Assert
            resolver.Verify(mock => mock.Resolve(It.IsAny<TagHelperDescriptorResolutionContext>()), Times.Once);
        }

        [Fact]
        public void GetDescriptors_LocatesTagHelperChunkGenerator_CreatesDirectiveDescriptors()
        {
            // Arrange
            var factory = new SpanFactory();
            var resolver = new TestTagHelperDescriptorResolver();
            var tagHelperDirectiveSpanVisitor = new TagHelperDirectiveSpanVisitor(resolver, new ErrorSink());
            var document = new MarkupBlock(
                factory.Code("one").AsAddTagHelper("one"),
                factory.Code("two").AsRemoveTagHelper("two"),
                factory.Code("three").AsRemoveTagHelper("three"),
                factory.Code("four").AsTagHelperPrefixDirective("four"));
            var expectedDescriptors = new TagHelperDirectiveDescriptor[]
            {
                new TagHelperDirectiveDescriptor
                {
                    DirectiveText = "one",
                    DirectiveType = TagHelperDirectiveType.AddTagHelper
                },
                new TagHelperDirectiveDescriptor
                {
                    DirectiveText = "two",
                    DirectiveType = TagHelperDirectiveType.RemoveTagHelper
                },
                new TagHelperDirectiveDescriptor
                {
                    DirectiveText = "three",
                    DirectiveType = TagHelperDirectiveType.RemoveTagHelper
                },
                new TagHelperDirectiveDescriptor
                {
                    DirectiveText = "four",
                    DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                }
            };

            // Act
            tagHelperDirectiveSpanVisitor.GetDescriptors(document);

            // Assert
            Assert.Equal(
                expectedDescriptors,
                resolver.DirectiveDescriptors,
                TagHelperDirectiveDescriptorComparer.Default);
        }

        [Fact]
        public void GetDescriptors_CanOverrideResolutionContext()
        {
            // Arrange
            var factory = new SpanFactory();
            var resolver = new TestTagHelperDescriptorResolver();
            var expectedInitialDirectiveDescriptors = new TagHelperDirectiveDescriptor[]
            {
                new TagHelperDirectiveDescriptor
                {
                    DirectiveText = "one",
                    DirectiveType = TagHelperDirectiveType.AddTagHelper
                },
                new TagHelperDirectiveDescriptor
                {
                    DirectiveText = "two",
                    DirectiveType = TagHelperDirectiveType.RemoveTagHelper
                },
                new TagHelperDirectiveDescriptor
                {
                    DirectiveText = "three",
                    DirectiveType = TagHelperDirectiveType.RemoveTagHelper
                },
                new TagHelperDirectiveDescriptor
                {
                    DirectiveText = "four",
                    DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                }
            };
            var expectedEndDirectiveDescriptors = new TagHelperDirectiveDescriptor[]
            {
                new TagHelperDirectiveDescriptor
                {
                    DirectiveText = "custom",
                    DirectiveType = TagHelperDirectiveType.AddTagHelper
                }
            };
            var tagHelperDirectiveSpanVisitor = new CustomTagHelperDirectiveSpanVisitor(
                resolver,
                (descriptors, errorSink) =>
                {
                    Assert.Equal(
                        expectedInitialDirectiveDescriptors,
                        descriptors,
                        TagHelperDirectiveDescriptorComparer.Default);

                    return new TagHelperDescriptorResolutionContext(expectedEndDirectiveDescriptors, errorSink);
                });
            var document = new MarkupBlock(
                factory.Code("one").AsAddTagHelper("one"),
                factory.Code("two").AsRemoveTagHelper("two"),
                factory.Code("three").AsRemoveTagHelper("three"),
                factory.Code("four").AsTagHelperPrefixDirective("four"));


            // Act
            tagHelperDirectiveSpanVisitor.GetDescriptors(document);

            // Assert
            Assert.Equal(expectedEndDirectiveDescriptors,
                         resolver.DirectiveDescriptors,
                         TagHelperDirectiveDescriptorComparer.Default);
        }

        [Fact]
        public void GetDescriptors_LocatesTagHelperPrefixDirectiveChunkGenerator()
        {
            // Arrange
            var factory = new SpanFactory();
            var resolver = new TestTagHelperDescriptorResolver();
            var tagHelperDirectiveSpanVisitor = new TagHelperDirectiveSpanVisitor(resolver, new ErrorSink());
            var document = new MarkupBlock(
                new DirectiveBlock(
                    factory.CodeTransition(),
                    factory
                        .MetaCode(SyntaxConstants.CSharp.TagHelperPrefixKeyword + " ")
                        .Accepts(AcceptedCharacters.None),
                    factory.Code("something").AsTagHelperPrefixDirective("something")));
            var expectedDirectiveDescriptor =
                new TagHelperDirectiveDescriptor
                {
                    DirectiveText = "something",
                    DirectiveType = TagHelperDirectiveType.TagHelperPrefix
                };

            // Act
            tagHelperDirectiveSpanVisitor.GetDescriptors(document);

            // Assert
            var directiveDescriptor = Assert.Single(resolver.DirectiveDescriptors);
            Assert.Equal(
                expectedDirectiveDescriptor,
                directiveDescriptor,
                TagHelperDirectiveDescriptorComparer.Default);
        }

        [Fact]
        public void GetDescriptors_LocatesAddTagHelperChunkGenerator()
        {
            // Arrange
            var factory = new SpanFactory();
            var resolver = new TestTagHelperDescriptorResolver();
            var tagHelperDirectiveSpanVisitor = new TagHelperDirectiveSpanVisitor(resolver, new ErrorSink());
            var document = new MarkupBlock(
                new DirectiveBlock(
                    factory.CodeTransition(),
                    factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    factory.Code("something").AsAddTagHelper("something"))
            );
            var expectedRegistration = new TagHelperDirectiveDescriptor
            {
                DirectiveText = "something",
                DirectiveType = TagHelperDirectiveType.AddTagHelper
            };

            // Act
            tagHelperDirectiveSpanVisitor.GetDescriptors(document);

            // Assert
            var directiveDescriptor = Assert.Single(resolver.DirectiveDescriptors);
            Assert.Equal(expectedRegistration, directiveDescriptor, TagHelperDirectiveDescriptorComparer.Default);
        }

        [Fact]
        public void GetDescriptors_LocatesNestedRemoveTagHelperChunkGenerator()
        {
            // Arrange
            var factory = new SpanFactory();
            var resolver = new TestTagHelperDescriptorResolver();
            var tagHelperDirectiveSpanVisitor = new TagHelperDirectiveSpanVisitor(resolver, new ErrorSink());
            var document = new MarkupBlock(
                new DirectiveBlock(
                    factory.CodeTransition(),
                    factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    factory.Code("something").AsRemoveTagHelper("something"))
            );
            var expectedRegistration = new TagHelperDirectiveDescriptor
            {
                DirectiveText = "something",
                DirectiveType = TagHelperDirectiveType.RemoveTagHelper
            };

            // Act
            tagHelperDirectiveSpanVisitor.GetDescriptors(document);

            // Assert
            var directiveDescriptor = Assert.Single(resolver.DirectiveDescriptors);
            Assert.Equal(expectedRegistration, directiveDescriptor, TagHelperDirectiveDescriptorComparer.Default);
        }

        [Fact]
        public void GetDescriptors_RemoveTagHelperNotInDocument_DoesNotThrow()
        {
            // Arrange
            var factory = new SpanFactory();
            var tagHelperDirectiveSpanVisitor =
                new TagHelperDirectiveSpanVisitor(
                    new TestTagHelperDescriptorResolver(),
                    new ErrorSink());
            var document = new MarkupBlock(factory.Markup("Hello World"));

            // Act
            var descriptors = tagHelperDirectiveSpanVisitor.GetDescriptors(document);

            Assert.Empty(descriptors);
        }

        private class TestTagHelperDescriptorResolver : ITagHelperDescriptorResolver
        {
            public TestTagHelperDescriptorResolver()
            {
                DirectiveDescriptors = new List<TagHelperDirectiveDescriptor>();
            }

            public List<TagHelperDirectiveDescriptor> DirectiveDescriptors { get; }

            public IEnumerable<TagHelperDescriptor> Resolve(TagHelperDescriptorResolutionContext resolutionContext)
            {
                DirectiveDescriptors.AddRange(resolutionContext.DirectiveDescriptors);

                return Enumerable.Empty<TagHelperDescriptor>();
            }
        }

        private class TagHelperDirectiveDescriptorComparer : IEqualityComparer<TagHelperDirectiveDescriptor>
        {
            public static readonly TagHelperDirectiveDescriptorComparer Default =
                new TagHelperDirectiveDescriptorComparer();

            private TagHelperDirectiveDescriptorComparer()
            {
            }

            public bool Equals(TagHelperDirectiveDescriptor directiveDescriptorX,
                               TagHelperDirectiveDescriptor directiveDescriptorY)
            {
                return string.Equals(directiveDescriptorX.DirectiveText,
                                     directiveDescriptorY.DirectiveText,
                                     StringComparison.Ordinal) &&
                       directiveDescriptorX.DirectiveType == directiveDescriptorY.DirectiveType;
            }

            public int GetHashCode(TagHelperDirectiveDescriptor directiveDescriptor)
            {
                var hashCodeCombiner = HashCodeCombiner.Start();
                hashCodeCombiner.Add(base.GetHashCode());
                hashCodeCombiner.Add(directiveDescriptor.DirectiveText);
                hashCodeCombiner.Add(directiveDescriptor.DirectiveType);

                return hashCodeCombiner;
            }
        }

        private class CustomTagHelperDirectiveSpanVisitor : TagHelperDirectiveSpanVisitor
        {
            private Func<IEnumerable<TagHelperDirectiveDescriptor>,
                         ErrorSink,
                         TagHelperDescriptorResolutionContext> _replacer;

            public CustomTagHelperDirectiveSpanVisitor(
                ITagHelperDescriptorResolver descriptorResolver,
                Func<IEnumerable<TagHelperDirectiveDescriptor>,
                     ErrorSink,
                     TagHelperDescriptorResolutionContext> replacer)
                : base(descriptorResolver, new ErrorSink())
            {
                _replacer = replacer;
            }

            protected override TagHelperDescriptorResolutionContext GetTagHelperDescriptorResolutionContext(
                IEnumerable<TagHelperDirectiveDescriptor> descriptors,
                ErrorSink errorSink)
            {
                return _replacer(descriptors, errorSink);
            }
        }
    }
}
