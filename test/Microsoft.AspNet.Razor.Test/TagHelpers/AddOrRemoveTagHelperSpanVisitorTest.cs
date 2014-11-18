// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Parser.TagHelpers;
using Microsoft.AspNet.Razor.Test.Framework;
using Microsoft.Internal.Web.Utils;
#if !ASPNETCORE50
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    public class AddOrRemoveTagHelperSpanVisitorTest
    {
        private static readonly SpanFactory Factory = SpanFactory.CreateCsHtml();

#if !ASPNETCORE50
        [Fact]
        public void GetDescriptors_InvokesResolveOnceForAllDirectives()
        {
            // Arrange
            var resolver = new Mock<ITagHelperDescriptorResolver>();
            resolver.Setup(mock => mock.Resolve(It.IsAny<TagHelperDescriptorResolutionContext>()))
                    .Returns(Enumerable.Empty<TagHelperDescriptor>());
            var addOrRemoveTagHelperSpanVisitor = new AddOrRemoveTagHelperSpanVisitor(resolver.Object);
            var document = new MarkupBlock(
                Factory.Code("\"one\"").AsAddTagHelper("one"),
                Factory.Code("\"two\"").AsRemoveTagHelper("two"),
                Factory.Code("\"three\"").AsRemoveTagHelper("three"));

            // Act
            addOrRemoveTagHelperSpanVisitor.GetDescriptors(document);

            // Assert
            resolver.Verify(mock => mock.Resolve(It.IsAny<TagHelperDescriptorResolutionContext>()), Times.Once);
        }
#endif

        [Fact]
        public void GetDescriptors_LocatesTagHelperCodeGenerator_CreatesDirectiveDescriptors()
        {
            // Arrange
            var resolver = new TestTagHelperDescriptorResolver();
            var addOrRemoveTagHelperSpanVisitor = new AddOrRemoveTagHelperSpanVisitor(resolver);
            var document = new MarkupBlock(
                Factory.Code("\"one\"").AsAddTagHelper("one"),
                Factory.Code("\"two\"").AsRemoveTagHelper("two"),
                Factory.Code("\"three\"").AsRemoveTagHelper("three"));
            var expectedRegistrations = new TagHelperDirectiveDescriptor[]
            {
                new TagHelperDirectiveDescriptor("one", TagHelperDirectiveType.AddTagHelper),
                new TagHelperDirectiveDescriptor("two", TagHelperDirectiveType.RemoveTagHelper),
                new TagHelperDirectiveDescriptor("three", TagHelperDirectiveType.RemoveTagHelper),
            };

            // Act
            addOrRemoveTagHelperSpanVisitor.GetDescriptors(document);

            // Assert
            Assert.Equal(expectedRegistrations,
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
            };
            var expectedEndDirectiveDescriptors = new TagHelperDirectiveDescriptor[]
            {
                new TagHelperDirectiveDescriptor("custom", TagHelperDirectiveType.AddTagHelper)
            };
            var addOrRemoveTagHelperSpanVisitor = new CustomAddOrRemoveTagHelperSpanVisitor(
                resolver,
                (descriptors) =>
                {
                    Assert.Equal(expectedInitialDirectiveDescriptors,
                                 descriptors,
                                 TagHelperDirectiveDescriptorComparer.Default);

                    return new TagHelperDescriptorResolutionContext(expectedEndDirectiveDescriptors);
                });
            var document = new MarkupBlock(
                Factory.Code("\"one\"").AsAddTagHelper("one"),
                Factory.Code("\"two\"").AsRemoveTagHelper("two"),
                Factory.Code("\"three\"").AsRemoveTagHelper("three"));


            // Act
            addOrRemoveTagHelperSpanVisitor.GetDescriptors(document);

            // Assert
            Assert.Equal(expectedEndDirectiveDescriptors,
                         resolver.DirectiveDescriptors,
                         TagHelperDirectiveDescriptorComparer.Default);
        }

        [Fact]
        public void GetDescriptors_LocatesAddTagHelperCodeGenerator()
        {
            // Arrange
            var resolver = new TestTagHelperDescriptorResolver();
            var addOrRemoveTagHelperSpanVisitor = new AddOrRemoveTagHelperSpanVisitor(resolver);
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
            addOrRemoveTagHelperSpanVisitor.GetDescriptors(document);

            // Assert
            var directiveDescriptor = Assert.Single(resolver.DirectiveDescriptors);
            Assert.Equal(expectedRegistration, directiveDescriptor, TagHelperDirectiveDescriptorComparer.Default);
        }

        [Fact]
        public void GetDescriptors_LocatesNestedRemoveTagHelperCodeGenerator()
        {
            // Arrange
            var resolver = new TestTagHelperDescriptorResolver();
            var addOrRemoveTagHelperSpanVisitor = new AddOrRemoveTagHelperSpanVisitor(resolver);
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
            addOrRemoveTagHelperSpanVisitor.GetDescriptors(document);

            // Assert
            var directiveDescriptor = Assert.Single(resolver.DirectiveDescriptors);
            Assert.Equal(expectedRegistration, directiveDescriptor, TagHelperDirectiveDescriptorComparer.Default);
        }

        [Fact]
        public void GetDescriptors_RemoveTagHelperNotInDocument_DoesNotThrow()
        {
            // Arrange
            var addOrRemoveTagHelperSpanVisitor =
                new AddOrRemoveTagHelperSpanVisitor(
                    new TestTagHelperDescriptorResolver());
            var document = new MarkupBlock(Factory.Markup("Hello World"));

            // Act & Assert
            Assert.DoesNotThrow(() => addOrRemoveTagHelperSpanVisitor.GetDescriptors(document));
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
                return string.Equals(directiveDescriptorX.LookupText,
                                     directiveDescriptorY.LookupText,
                                     StringComparison.Ordinal) &&
                       directiveDescriptorX.DirectiveType == directiveDescriptorY.DirectiveType;
            }

            public int GetHashCode(TagHelperDirectiveDescriptor directiveDescriptor)
            {
                return HashCodeCombiner.Start()
                                       .Add(base.GetHashCode())
                                       .Add(directiveDescriptor.LookupText)
                                       .Add(directiveDescriptor.DirectiveType)
                                       .CombinedHash;
            }
        }

        private class CustomAddOrRemoveTagHelperSpanVisitor : AddOrRemoveTagHelperSpanVisitor
        {
            private Func<IEnumerable<TagHelperDirectiveDescriptor>, TagHelperDescriptorResolutionContext> _replacer;

            public CustomAddOrRemoveTagHelperSpanVisitor(
                ITagHelperDescriptorResolver descriptorResolver,
                Func<IEnumerable<TagHelperDirectiveDescriptor>, TagHelperDescriptorResolutionContext> replacer)
                : base(descriptorResolver)
            {
                _replacer = replacer;
            }

            protected override TagHelperDescriptorResolutionContext GetTagHelperDescriptorResolutionContext(
                IEnumerable<TagHelperDirectiveDescriptor> descriptors)
            {
                return _replacer(descriptors);
            }
        }
    }
}