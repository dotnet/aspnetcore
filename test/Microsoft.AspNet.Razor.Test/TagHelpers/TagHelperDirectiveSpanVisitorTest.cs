// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Parser.TagHelpers;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.Framework.Internal;
#if !DNXCORE50
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    public class TagHelperDirectiveSpanVisitorTest
    {
        private static readonly SpanFactory Factory = SpanFactory.CreateCsHtml();

#if !DNXCORE50
        [Fact]
        public void GetDescriptors_InvokesResolveOnceForAllDirectives()
        {
            // Arrange
            var resolver = new Mock<ITagHelperDescriptorResolver>();
            resolver.Setup(mock => mock.Resolve(It.IsAny<TagHelperDescriptorResolutionContext>()))
                    .Returns(Enumerable.Empty<TagHelperDescriptor>());
            var tagHelperDirectiveSpanVisitor = new TagHelperDirectiveSpanVisitor(
                resolver.Object,
                new ErrorSink());
            var document = new MarkupBlock(
                Factory.Code("\"one\"").AsAddTagHelper("one"),
                Factory.Code("\"two\"").AsRemoveTagHelper("two"),
                Factory.Code("\"three\"").AsRemoveTagHelper("three"),
                Factory.Code("\"four\"").AsTagHelperPrefixDirective("four"));

            // Act
            tagHelperDirectiveSpanVisitor.GetDescriptors(document);

            // Assert
            resolver.Verify(mock => mock.Resolve(It.IsAny<TagHelperDescriptorResolutionContext>()), Times.Once);
        }
#endif

        [Fact]
        public void GetDescriptors_LocatesTagHelperChunkGenerator_CreatesDirectiveDescriptors()
        {
            // Arrange
            var resolver = new TestTagHelperDescriptorResolver();
            var tagHelperDirectiveSpanVisitor = new TagHelperDirectiveSpanVisitor(resolver, new ErrorSink());
            var document = new MarkupBlock(
                Factory.Code("\"one\"").AsAddTagHelper("one"),
                Factory.Code("\"two\"").AsRemoveTagHelper("two"),
                Factory.Code("\"three\"").AsRemoveTagHelper("three"),
                Factory.Code("\"four\"").AsTagHelperPrefixDirective("four"));
            var expectedDescriptors = new TagHelperDirectiveDescriptor[]
            {
                new TagHelperDirectiveDescriptor("one", TagHelperDirectiveType.AddTagHelper),
                new TagHelperDirectiveDescriptor("two", TagHelperDirectiveType.RemoveTagHelper),
                new TagHelperDirectiveDescriptor("three", TagHelperDirectiveType.RemoveTagHelper),
                new TagHelperDirectiveDescriptor("four", TagHelperDirectiveType.TagHelperPrefix),
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
            var resolver = new TestTagHelperDescriptorResolver();
            var expectedInitialDirectiveDescriptors = new TagHelperDirectiveDescriptor[]
            {
                new TagHelperDirectiveDescriptor("one", TagHelperDirectiveType.AddTagHelper),
                new TagHelperDirectiveDescriptor("two", TagHelperDirectiveType.RemoveTagHelper),
                new TagHelperDirectiveDescriptor("three", TagHelperDirectiveType.RemoveTagHelper),
                new TagHelperDirectiveDescriptor("four", TagHelperDirectiveType.TagHelperPrefix),
            };
            var expectedEndDirectiveDescriptors = new TagHelperDirectiveDescriptor[]
            {
                new TagHelperDirectiveDescriptor("custom", TagHelperDirectiveType.AddTagHelper)
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
                Factory.Code("\"one\"").AsAddTagHelper("one"),
                Factory.Code("\"two\"").AsRemoveTagHelper("two"),
                Factory.Code("\"three\"").AsRemoveTagHelper("three"),
                Factory.Code("\"four\"").AsTagHelperPrefixDirective("four"));


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
            var resolver = new TestTagHelperDescriptorResolver();
            var tagHelperDirectiveSpanVisitor = new TagHelperDirectiveSpanVisitor(resolver, new ErrorSink());
            var document = new MarkupBlock(
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory
                        .MetaCode(SyntaxConstants.CSharp.TagHelperPrefixKeyword + " ")
                        .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"something\"").AsTagHelperPrefixDirective("something")));
            var expectedDirectiveDescriptor =
                new TagHelperDirectiveDescriptor("something", TagHelperDirectiveType.TagHelperPrefix);

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
            var resolver = new TestTagHelperDescriptorResolver();
            var tagHelperDirectiveSpanVisitor = new TagHelperDirectiveSpanVisitor(resolver, new ErrorSink());
            var document = new MarkupBlock(
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"something\"").AsAddTagHelper("something"))
            );
            var expectedRegistration =
                new TagHelperDirectiveDescriptor("something", TagHelperDirectiveType.AddTagHelper);

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
            var resolver = new TestTagHelperDescriptorResolver();
            var tagHelperDirectiveSpanVisitor = new TagHelperDirectiveSpanVisitor(resolver, new ErrorSink());
            var document = new MarkupBlock(
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"something\"").AsRemoveTagHelper("something"))
            );
            var expectedRegistration =
                new TagHelperDirectiveDescriptor("something", TagHelperDirectiveType.RemoveTagHelper);

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
            var tagHelperDirectiveSpanVisitor =
                new TagHelperDirectiveSpanVisitor(
                    new TestTagHelperDescriptorResolver(),
                    new ErrorSink());
            var document = new MarkupBlock(Factory.Markup("Hello World"));

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
                return HashCodeCombiner.Start()
                                       .Add(base.GetHashCode())
                                       .Add(directiveDescriptor.DirectiveText)
                                       .Add(directiveDescriptor.DirectiveType)
                                       .CombinedHash;
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
