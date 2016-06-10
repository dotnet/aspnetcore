using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.Chunks.Generators;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.CodeGenerators.Visitors;
using Microsoft.AspNetCore.Razor.Compilation.TagHelpers;
using Microsoft.AspNetCore.Razor.Parser.SyntaxTree;
using Microsoft.AspNetCore.Razor.Parser.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Test.CodeGenerators
{
    public class CSharpTagHelperRenderingUnitTest
    {
        [Fact]
        public void CreatesAUniqueIdForSingleTagHelperChunk()
        {
            // Arrange
            var chunk = CreateTagHelperChunk(
                "div",
                new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "div",
                        TypeName = "DivTagHelper",
                        AssemblyName = "FakeAssemblyName"
                    }
                });
            var codeRenderer = CreateCodeRenderer();

            // Act
            codeRenderer.RenderTagHelper(chunk);

            // Assert
            Assert.Equal(1, codeRenderer.GenerateUniqueIdCount);
        }

        [Fact]
        public void UsesTheSameUniqueIdForTagHelperChunkWithMultipleTagHelpers()
        {
            // Arrange
            var chunk = CreateTagHelperChunk(
                "div",
                new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "div",
                        TypeName = "DivTagHelper",
                        AssemblyName = "FakeAssemblyName"
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "div",
                        TypeName = "Div2TagHelper",
                        AssemblyName = "FakeAssemblyName"
                    }
                });
            var codeRenderer = CreateCodeRenderer();

            // Act
            codeRenderer.RenderTagHelper(chunk);

            // Assert
            Assert.Equal(1, codeRenderer.GenerateUniqueIdCount);
        }

        [Fact]
        public void UsesDifferentUniqueIdForMultipleTagHelperChunksForSameTagHelper()
        {
            // Arrange
            var chunk1 = CreateTagHelperChunk(
                "div",
                new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "div",
                        TypeName = "DivTagHelper",
                        AssemblyName = "FakeAssemblyName"
                    }
                });
            var chunk2 = CreateTagHelperChunk(
                "div",
                new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "div",
                        TypeName = "DivTagHelper",
                        AssemblyName = "FakeAssemblyName"
                    }
                });
            var codeRenderer = CreateCodeRenderer();

            // Act
            codeRenderer.RenderTagHelper(chunk1);
            codeRenderer.RenderTagHelper(chunk2);

            // Assert
            Assert.Equal(2, codeRenderer.GenerateUniqueIdCount);
        }

        [Fact]
        public void UsesDifferentUniqueIdForNestedTagHelperChunksForSameTagHelper()
        {
            // Arrange
            var parentChunk = CreateTagHelperChunk(
                "div",
                new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "div",
                        TypeName = "DivTagHelper",
                        AssemblyName = "FakeAssemblyName"
                    }
                });
            var childChunk = CreateTagHelperChunk(
                "div",
                new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "div",
                        TypeName = "DivTagHelper",
                        AssemblyName = "FakeAssemblyName"
                    }
                });
            parentChunk.Children.Add(childChunk);
            var codeRenderer = CreateCodeRenderer();

            // Act
            codeRenderer.RenderTagHelper(parentChunk);

            // Assert
            Assert.Equal(2, codeRenderer.GenerateUniqueIdCount);
        }

        [Fact]
        public void UsesDifferentUniqueIdForMultipleTagHelperChunksForDifferentTagHelpers()
        {
            // Arrange
            var divChunk = CreateTagHelperChunk(
                "div",
                new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "div",
                        TypeName = "DivTagHelper",
                        AssemblyName = "FakeAssemblyName"
                    }
                });
            var spanChunk = CreateTagHelperChunk(
                "span",
                new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "span",
                        TypeName = "SpanTagHelper",
                        AssemblyName = "FakeAssemblyName"
                    }
                });
            var codeRenderer = CreateCodeRenderer();

            // Act
            codeRenderer.RenderTagHelper(divChunk);
            codeRenderer.RenderTagHelper(spanChunk);

            // Assert
            Assert.Equal(2, codeRenderer.GenerateUniqueIdCount);
        }

        [Fact]
        public void UsesCorrectUniqueIdForMultipleTagHelperChunksSomeWithSameSameTagHelpersSomeWithDifferentTagHelpers()
        {
            // Arrange
            var chunk1 = CreateTagHelperChunk(
                "div",
                new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "div",
                        TypeName = "DivTagHelper",
                        AssemblyName = "FakeAssemblyName"
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "div",
                        TypeName = "Div2TagHelper",
                        AssemblyName = "FakeAssemblyName"
                    }
                });
            var chunk2 = CreateTagHelperChunk(
                "span",
                new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "span",
                        TypeName = "SpanTagHelper",
                        AssemblyName = "FakeAssemblyName"
                    }
                });
            var chunk3 = CreateTagHelperChunk(
                "span",
                new[]
                {
                    new TagHelperDescriptor
                    {
                        TagName = "span",
                        TypeName = "SpanTagHelper",
                        AssemblyName = "FakeAssemblyName"
                    },
                    new TagHelperDescriptor
                    {
                        TagName = "span",
                        TypeName = "Span2TagHelper",
                        AssemblyName = "FakeAssemblyName"
                    }
                });
            var codeRenderer = CreateCodeRenderer();

            // Act
            codeRenderer.RenderTagHelper(chunk1);
            codeRenderer.RenderTagHelper(chunk2);
            codeRenderer.RenderTagHelper(chunk3);

            // Assert
            Assert.Equal(3, codeRenderer.GenerateUniqueIdCount);
        }

        private static TagHelperChunk CreateTagHelperChunk(string tagName, IEnumerable<TagHelperDescriptor> tagHelperDescriptors)
        {
            return new TagHelperChunk(
                tagName,
                tagMode: TagMode.StartTagAndEndTag,
                attributes: new List<TagHelperAttributeTracker>(),
                descriptors: tagHelperDescriptors)
            {
                Association = new TagHelperBlock(
                    new TagHelperBlockBuilder(
                        tagName,
                        tagMode: TagMode.StartTagAndEndTag,
                        attributes: new List<TagHelperAttributeNode>(),
                        children: Enumerable.Empty<SyntaxTreeNode>())),
                Children = new List<Chunk>(),
            };
        }

        private static TrackingUniqueIdsTagHelperCodeRenderer CreateCodeRenderer()
        {
            var writer = new CSharpCodeWriter();
            var codeGeneratorContext = CreateContext();
            var visitor = new CSharpCodeVisitor(writer, codeGeneratorContext);
            var codeRenderer = new TrackingUniqueIdsTagHelperCodeRenderer(
                visitor,
                writer,
                codeGeneratorContext);
            visitor.TagHelperRenderer = codeRenderer;
            return codeRenderer;
        }

        private static CodeGeneratorContext CreateContext()
        {
            return new CodeGeneratorContext(
                new ChunkGeneratorContext(
                    new RazorEngineHost(new CSharpRazorCodeLanguage()),
                    "MyClass",
                    "MyNamespace",
                    string.Empty,
                    shouldGenerateLinePragmas: true),
                new ErrorSink());
        }

        private class TrackingUniqueIdsTagHelperCodeRenderer : CSharpTagHelperCodeRenderer
        {
            public TrackingUniqueIdsTagHelperCodeRenderer(
                IChunkVisitor bodyVisitor,
                CSharpCodeWriter writer,
                CodeGeneratorContext context)
                : base(bodyVisitor, writer, context)
            {

            }

            protected override string GenerateUniqueId()
            {
                GenerateUniqueIdCount++;
                return "test";
            }

            public int GenerateUniqueIdCount { get; private set; }
        }
    }
}