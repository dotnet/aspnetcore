// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Parser.TagHelpers;
using Microsoft.AspNet.Razor.Test.Framework;
#if !ASPNETCORE50
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Razor.TagHelpers
{
    public class AddOrRemoveTagHelperSpanVisitorTest
    {
        private static readonly SpanFactory Factory = SpanFactory.CreateCsHtml();

        private static TagHelperDescriptor PTagHelperDescriptor
        {
            get
            {
                return new TagHelperDescriptor("p", "PTagHelper", ContentBehavior.None);
            }
        }

        private static TagHelperDescriptor DivTagHelperDescriptor
        {
            get
            {
                return new TagHelperDescriptor("div", "DivTagHelper", ContentBehavior.None);
            }
        }

#if !ASPNETCORE50
        [Fact]
        public void GetDescriptors_InvokesResolveForEachLookup()
        {
            // Arrange
            var resolver = new Mock<ITagHelperDescriptorResolver>();
            resolver.Setup(mock => mock.Resolve(It.IsAny<string>())).Returns(Enumerable.Empty<TagHelperDescriptor>());
            var addOrRemoveTagHelperSpanVisitor = new AddOrRemoveTagHelperSpanVisitor(resolver.Object);
            var document = new MarkupBlock(
                Factory.Code("\"one\"").AsAddTagHelper("one"),
                Factory.Code("\"two\"").AsRemoveTagHelper("two"),
                Factory.Code("\"three\"").AsRemoveTagHelper("three"));

            // Act
            var descriptors = addOrRemoveTagHelperSpanVisitor.GetDescriptors(document);

            // Assert
            Assert.Empty(descriptors);
            resolver.Verify(mock => mock.Resolve(It.IsAny<string>()), Times.Exactly(3));
        }
#endif

        [Fact]
        public void GetDescriptors_LocatesNestedRemoveTagHelperCodeGenerator()
        {
            // Arrange
            var resolver = new TestTagHelperDescriptorResolver(
                new Dictionary<string, IEnumerable<TagHelperDescriptor>>
                {
                    { "something", new[] { PTagHelperDescriptor } }
                });
            var addOrRemoveTagHelperSpanVisitor = new AddOrRemoveTagHelperSpanVisitor(resolver);
            var document = new MarkupBlock(
                new DirectiveBlock(
                    Factory.CodeTransition(),
                    Factory.MetaCode(SyntaxConstants.CSharp.RemoveTagHelperKeyword + " ")
                           .Accepts(AcceptedCharacters.None),
                    Factory.Code("\"something\"").AsRemoveTagHelper("something"))
            );

            // Act
            var descriptors = addOrRemoveTagHelperSpanVisitor.GetDescriptors(document);

            // Assert
            Assert.Empty(descriptors);
            var lookup = Assert.Single(resolver.Lookups);
            Assert.Equal("something", lookup);
        }

        [Fact]
        public void GetDescriptors_RemovesSpecifiedTagHelper()
        {
            // Arrange
            var resolver = new TestTagHelperDescriptorResolver(
                new Dictionary<string, IEnumerable<TagHelperDescriptor>>
                {
                    { "twoTagHelpers", new[] { PTagHelperDescriptor, DivTagHelperDescriptor } },
                    { "singleTagHelper", new [] { PTagHelperDescriptor } }
                });
            var addOrRemoveTagHelperSpanVisitor = new AddOrRemoveTagHelperSpanVisitor(resolver);
            var document = new MarkupBlock(
                Factory.Code("\"twoTagHelpers\"").AsAddTagHelper("twoTagHelpers"),
                Factory.Code("\"singleTagHelper\"").AsRemoveTagHelper("singleTagHelper"));

            // Act
            var descriptors = addOrRemoveTagHelperSpanVisitor.GetDescriptors(document);

            // Assert
            var descriptor = Assert.Single(descriptors);
            Assert.Equal(DivTagHelperDescriptor, descriptor, TagHelperDescriptorComparer.Default);
            Assert.Equal(2, resolver.Lookups.Count);
            Assert.Contains("twoTagHelpers", resolver.Lookups);
            Assert.Contains("singleTagHelper", resolver.Lookups);
        }

        [Fact]
        public void GetDescriptors_RemovesAddedTagHelpers()
        {
            // Arrange
            var resolver = new TestTagHelperDescriptorResolver(
                new Dictionary<string, IEnumerable<TagHelperDescriptor>>
                {
                    { "twoTagHelpers", new[] { PTagHelperDescriptor, DivTagHelperDescriptor } }
                });
            var addOrRemoveTagHelperSpanVisitor = new AddOrRemoveTagHelperSpanVisitor(resolver);
            var document = new MarkupBlock(
                Factory.Code("\"twoTagHelpers\"").AsAddTagHelper("twoTagHelpers"),
                Factory.Code("\"twoTagHelpers\"").AsRemoveTagHelper("twoTagHelpers"));

            // Act
            var descriptors = addOrRemoveTagHelperSpanVisitor.GetDescriptors(document);

            // Assert
            Assert.Empty(descriptors);
            Assert.Equal(Enumerable.Repeat("twoTagHelpers", 2), resolver.Lookups);
        }

        [Fact]
        public void GetDescriptors_RemoveTagHelper_OrderMatters()
        {
            // Arrange
            var expectedDescriptors = new[] { PTagHelperDescriptor, DivTagHelperDescriptor };
            var resolver = new TestTagHelperDescriptorResolver(
                new Dictionary<string, IEnumerable<TagHelperDescriptor>>
                {
                    { "twoTagHelpers", expectedDescriptors }
                });
            var addOrRemoveTagHelperSpanVisitor = new AddOrRemoveTagHelperSpanVisitor(resolver);
            var document = new MarkupBlock(
                Factory.Code("\"twoTagHelpers\"").AsRemoveTagHelper("twoTagHelpers"),
                Factory.Code("\"twoTagHelpers\"").AsAddTagHelper("twoTagHelpers"));

            // Act
            var descriptors = addOrRemoveTagHelperSpanVisitor.GetDescriptors(document);

            // Assert
            Assert.Equal(expectedDescriptors, descriptors, TagHelperDescriptorComparer.Default);
            Assert.Equal(Enumerable.Repeat("twoTagHelpers", 2), resolver.Lookups);
        }

        [Fact]
        public void GetDescriptors_RemoveTagHelperInDocument_ThrowsIfNullResolver()
        {
            // Arrange
            var addOrRemoveTagHelperSpanVisitor = new AddOrRemoveTagHelperSpanVisitor(descriptorResolver: null);
            var document = new MarkupBlock(
                Factory.Code("\"something\"").AsRemoveTagHelper("something"));
            var expectedMessage = "Cannot use directive 'removetaghelper' when a Microsoft.AspNet.Razor.TagHelpers." +
                                  "ITagHelperDescriptorResolver has not been provided to the Microsoft.AspNet.Razor." +
                                  "Parser.RazorParser.";

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                addOrRemoveTagHelperSpanVisitor.GetDescriptors(document);
            });

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void GetDescriptors_RemoveTagHelperNotInDocument_DoesNotThrow()
        {
            // Arrange
            var addOrRemoveTagHelperSpanVisitor = new AddOrRemoveTagHelperSpanVisitor(descriptorResolver: null);
            var document = new MarkupBlock(Factory.Markup("Hello World"));

            // Act & Assert
            Assert.DoesNotThrow(() => addOrRemoveTagHelperSpanVisitor.GetDescriptors(document));
        }

        // TODO: Add @addtaghelper directive unit tests. Tracked by https://github.com/aspnet/Razor/issues/202.

        private class TestTagHelperDescriptorResolver : ITagHelperDescriptorResolver
        {
            private readonly IReadOnlyDictionary<string, IEnumerable<TagHelperDescriptor>> _lookupTable;

            public TestTagHelperDescriptorResolver(
                IReadOnlyDictionary<string, IEnumerable<TagHelperDescriptor>> lookupTable)
            {
                _lookupTable = lookupTable;

                Lookups = new List<string>();
            }

            public List<string> Lookups { get; private set; }

            public IEnumerable<TagHelperDescriptor> Resolve(string lookupText)
            {
                Lookups.Add(lookupText);

                IEnumerable<TagHelperDescriptor> descriptors;
                if (_lookupTable.TryGetValue(lookupText, out descriptors))
                {
                    return descriptors;
                }

                return Enumerable.Empty<TagHelperDescriptor>();
            }
        }
    }
}