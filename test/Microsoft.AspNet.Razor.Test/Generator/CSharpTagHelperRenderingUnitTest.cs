using System.Collections.Generic;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.TagHelpers;
using Xunit;

namespace Microsoft.AspNet.Razor.Test.Generator
{
    public class CSharpTagHelperRenderingUnitTest
    {
        [Fact]
        public void CreatesAUniqueIdForSingleTagHelperChunk()
        {
            // Arrange
            var chunk = CreateTagHelperChunk("div", new[] {
                new TagHelperDescriptor("div", "DivTagHelper", "FakeAssemblyName")
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
            var chunk = CreateTagHelperChunk("div", new[] {
                new TagHelperDescriptor("div", "DivTagHelper", "FakeAssemblyName"),
                new TagHelperDescriptor("div", "Div2TagHelper", "FakeAssemblyName")
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
            var chunk1 = CreateTagHelperChunk("div", new[] {
                new TagHelperDescriptor("div", "DivTagHelper", "FakeAssemblyName")
            });
            var chunk2 = CreateTagHelperChunk("div", new[] {
                new TagHelperDescriptor("div", "DivTagHelper", "FakeAssemblyName")
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
            var parentChunk = CreateTagHelperChunk("div", new[] {
                new TagHelperDescriptor("div", "DivTagHelper", "FakeAssemblyName")
            });
            var childChunk = CreateTagHelperChunk("div", new[] {
                new TagHelperDescriptor("div", "DivTagHelper", "FakeAssemblyName")
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
            var divChunk = CreateTagHelperChunk("div", new[] {
                new TagHelperDescriptor("div", "DivTagHelper", "FakeAssemblyName")
            });
            var spanChunk = CreateTagHelperChunk("span", new[] {
                new TagHelperDescriptor("span", "SpanTagHelper", "FakeAssemblyName")
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
            var chunk1 = CreateTagHelperChunk("div", new[] {
                new TagHelperDescriptor("div", "DivTagHelper", "FakeAssemblyName"),
                new TagHelperDescriptor("div", "Div2TagHelper", "FakeAssemblyName")
            });
            var chunk2 = CreateTagHelperChunk("span", new[] {
                new TagHelperDescriptor("span", "SpanTagHelper", "FakeAssemblyName")
            });
            var chunk3 = CreateTagHelperChunk("span", new[] {
                new TagHelperDescriptor("span", "SpanTagHelper", "FakeAssemblyName"),
                new TagHelperDescriptor("span", "Span2TagHelper", "FakeAssemblyName")
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
                selfClosing: false,
                attributes: new Dictionary<string, Chunk>(),
                descriptors: tagHelperDescriptors)
            {
                Children = new List<Chunk>(),
            };
        }

        private static TrackingUniqueIdsTagHelperCodeRenderer CreateCodeRenderer()
        {
            var writer = new CSharpCodeWriter();
            var codeBuilderContext = CreateContext();
            var visitor = new CSharpCodeVisitor(writer, codeBuilderContext);
            var codeRenderer = new TrackingUniqueIdsTagHelperCodeRenderer(
                visitor,
                writer,
                codeBuilderContext);
            visitor.TagHelperRenderer = codeRenderer;
            return codeRenderer;
        }

        private static CodeBuilderContext CreateContext()
        {
            return new CodeBuilderContext(
                new CodeGeneratorContext(
                    new RazorEngineHost(new CSharpRazorCodeLanguage()),
                    "MyClass",
                    "MyNamespace",
                    string.Empty,
                    shouldGenerateLinePragmas: true),
                new ParserErrorSink());
        }

        private class TrackingUniqueIdsTagHelperCodeRenderer : CSharpTagHelperCodeRenderer
        {
            public TrackingUniqueIdsTagHelperCodeRenderer(
                IChunkVisitor bodyVisitor,
                CSharpCodeWriter writer,
                CodeBuilderContext context)
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