// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class TagHelperBinderSyntaxTreePassTest
    {
        [Fact]
        public void Execute_RewritesTagHelpers()
        {
            // Arrange
            var engine = RazorEngine.Create(builder =>
            {
                var descriptors = new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "form",
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                    }
                };
                var resolver = new TestTagHelperDescriptorResolver(descriptors);
                var tagHelperFeature = new TagHelperFeature(resolver);
                builder.Features.Add(tagHelperFeature);
            });
            var pass = new TagHelperBinderSyntaxTreePass()
            {
                Engine = engine,
            };
            var sourceDocument = CreateTestSourceDocument();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);

            // Act
            var rewrittenTree = pass.Execute(codeDocument, originalTree);

            // Assert
            Assert.Empty(rewrittenTree.Diagnostics);
            Assert.Equal(3, rewrittenTree.Root.Children.Count);
            var formTagHelper = Assert.IsType<TagHelperBlock>(rewrittenTree.Root.Children[2]);
            Assert.Equal("form", formTagHelper.TagName);
            Assert.Equal(3, formTagHelper.Children.Count);
            var inputTagHelper = Assert.IsType<TagHelperBlock>(formTagHelper.Children[1]);
            Assert.Equal("input", inputTagHelper.TagName);
        }

        [Fact]
        public void Execute_NoopsWhenNoTagHelperFeature()
        {
            // Arrange
            var engine = RazorEngine.Create();
            var pass = new TagHelperBinderSyntaxTreePass()
            {
                Engine = engine,
            };
            var sourceDocument = CreateTestSourceDocument();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);

            // Act
            var outputTree = pass.Execute(codeDocument, originalTree);

            // Assert
            Assert.Empty(outputTree.Diagnostics);
            Assert.Same(originalTree, outputTree);
        }

        [Fact]
        public void Execute_NoopsWhenNoResolver()
        {
            // Arrange
            var engine = RazorEngine.Create(builder =>
            {

                var tagHelperFeature = new TagHelperFeature(resolver: null);
                builder.Features.Add(tagHelperFeature);
            });
            var pass = new TagHelperBinderSyntaxTreePass()
            {
                Engine = engine,
            };
            var sourceDocument = CreateTestSourceDocument();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);

            // Act
            var outputTree = pass.Execute(codeDocument, originalTree);

            // Assert
            Assert.Empty(outputTree.Diagnostics);
            Assert.Same(originalTree, outputTree);
        }

        [Fact]
        public void Execute_NoopsWhenNoTagHelperDescriptorsAreResolved()
        {
            // Arrange
            var engine = RazorEngine.Create(builder =>
            {
                var resolver = new TestTagHelperDescriptorResolver(descriptors: Enumerable.Empty<TagHelperDescriptor>());
                var tagHelperFeature = new TagHelperFeature(resolver);
                builder.Features.Add(tagHelperFeature);
            });
            var pass = new TagHelperBinderSyntaxTreePass()
            {
                Engine = engine,
            };
            var sourceDocument = CreateTestSourceDocument();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);

            // Act
            var outputTree = pass.Execute(codeDocument, originalTree);

            // Assert
            Assert.Empty(outputTree.Diagnostics);
            Assert.Same(originalTree, outputTree);
        }

        [Fact]
        public void Execute_RecreatesSyntaxTreeOnResolverErrors()
        {
            // Arrange
            var resolverError = new RazorError("Test error", new SourceLocation(19, 1, 17), length: 12);
            var engine = RazorEngine.Create(builder =>
            {
                var resolver = new ErrorLoggingTagHelperDescriptorResolver(resolverError);
                var tagHelperFeature = new TagHelperFeature(resolver);
                builder.Features.Add(tagHelperFeature);
            });
            var pass = new TagHelperBinderSyntaxTreePass()
            {
                Engine = engine,
            };
            var sourceDocument = CreateTestSourceDocument();
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            var initialError = new RazorError("Initial test error", SourceLocation.Zero, length: 1);
            var erroredOriginalTree = RazorSyntaxTree.Create(
                originalTree.Root,
                originalTree.Source,
                new[] { initialError },
                originalTree.Options);

            // Act
            var outputTree = pass.Execute(codeDocument, erroredOriginalTree);

            // Assert
            Assert.Empty(originalTree.Diagnostics);
            Assert.NotSame(erroredOriginalTree, outputTree);
            Assert.Equal(new[] { initialError, resolverError }, outputTree.Diagnostics);
        }

        [Fact]
        public void Execute_CombinesErrorsOnRewritingErrors()
        {
            // Arrange
            var engine = RazorEngine.Create(builder =>
            {
                var descriptors = new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "form",
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "input",
                    }
                };
                var resolver = new TestTagHelperDescriptorResolver(descriptors);
                var tagHelperFeature = new TagHelperFeature(resolver);
                builder.Features.Add(tagHelperFeature);
            });
            var pass = new TagHelperBinderSyntaxTreePass()
            {
                Engine = engine,
            };
            var content =
            @"
@addTagHelper *, TestAssembly
<form>
    <input value='Hello' type='text' />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var originalTree = RazorSyntaxTree.Parse(sourceDocument);
            var initialError = new RazorError("Initial test error", SourceLocation.Zero, length: 1);
            var expectedRewritingError = new RazorError(
                LegacyResources.FormatTagHelpersParseTreeRewriter_FoundMalformedTagHelper("form"),
                new SourceLocation(Environment.NewLine.Length * 2 + 30, 2, 1),
                length: 4);
            var erroredOriginalTree = RazorSyntaxTree.Create(originalTree.Root, originalTree.Source, new[] { initialError }, originalTree.Options);

            // Act
            var outputTree = pass.Execute(codeDocument, erroredOriginalTree);

            // Assert
            Assert.Empty(originalTree.Diagnostics);
            Assert.NotSame(erroredOriginalTree, outputTree);
            Assert.Equal(new[] { initialError, expectedRewritingError }, outputTree.Diagnostics);
        }

        private static RazorSourceDocument CreateTestSourceDocument()
        {
            var content =
            @"
@addTagHelper *, TestAssembly
<form>
    <input value='Hello' type='text' />
</form>";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            return sourceDocument;
        }

        private class TestTagHelperDescriptorResolver : ITagHelperDescriptorResolver
        {
            private readonly IEnumerable<TagHelperDescriptor> _descriptors;

            public TestTagHelperDescriptorResolver(IEnumerable<TagHelperDescriptor> descriptors)
            {
                _descriptors = descriptors;
            }

            public IEnumerable<TagHelperDescriptor> Resolve(TagHelperDescriptorResolutionContext resolutionContext)
            {
                return _descriptors;
            }
        }

        private class ErrorLoggingTagHelperDescriptorResolver : ITagHelperDescriptorResolver
        {
            private readonly RazorError _error;

            public ErrorLoggingTagHelperDescriptorResolver(RazorError error)
            {
                _error = error;
            }

            public IEnumerable<TagHelperDescriptor> Resolve(TagHelperDescriptorResolutionContext resolutionContext)
            {
                resolutionContext.ErrorSink.OnError(_error);

                return Enumerable.Empty<TagHelperDescriptor>();
            }
        }
    }
}
