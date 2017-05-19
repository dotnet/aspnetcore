// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class DesignTimeTagHelperWriterTest
    {
        [Fact]
        public void WriteDeclareTagHelperFields_DeclaresUsedTagHelperTypes()
        {
            // Arrange
            var writer = new DesignTimeTagHelperWriter();
            var context = GetCSharpRenderingContext(writer);
            var node = new DeclareTagHelperFieldsIRNode();
            node.UsedTagHelperTypeNames.Add("PTagHelper");
            node.UsedTagHelperTypeNames.Add("MyTagHelper");

            // Act
            writer.WriteDeclareTagHelperFields(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"private global::PTagHelper __PTagHelper = null;
private global::MyTagHelper __MyTagHelper = null;
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteCreateTagHelper_RendersCorrectly_UsesSpecifiedTagHelperType()
        {
            // Arrange
            var writer = new DesignTimeTagHelperWriter();
            var context = GetCSharpRenderingContext(writer);
            var node = new CreateTagHelperIRNode()
            {
                TagHelperTypeName = "TestNamespace.MyTagHelper"
            };

            // Act
            writer.WriteCreateTagHelper(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"__TestNamespace_MyTagHelper = CreateTagHelper<global::TestNamespace.MyTagHelper>();
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteSetTagHelperProperty_RendersCorrectly()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes: new Action<BoundAttributeDescriptorBuilder>[]
                    {
                        builder => builder
                            .Name("bound")
                            .PropertyName("FooProp")
                            .TypeName("System.String"),
                    })
            };
            var engine = RazorEngine.Create(builder => builder.AddTagHelpers(descriptors));
            var content = @"
@addTagHelper *, TestAssembly
<input bound=""value"" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument, engine);
            var node = irDocument.Children.Last().Children[2] as SetTagHelperPropertyIRNode;

            var writer = new DesignTimeTagHelperWriter();
            var context = GetCSharpRenderingContext(writer, codeDocument);

            // Act
            writer.WriteSetTagHelperProperty(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"Render Children
__InputTagHelper.FooProp = ""value"";
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteSetTagHelperProperty_NonStringAttribute_RendersCorrectly()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes: new Action<BoundAttributeDescriptorBuilder>[]
                    {
                        builder => builder
                            .Name("bound")
                            .PropertyName("FooProp")
                            .TypeName("System.Int32"),
                    })
            };
            var engine = RazorEngine.Create(builder => builder.AddTagHelpers(descriptors));
            var content = @"
@addTagHelper *, TestAssembly
<input bound=""42"" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument, engine);
            var node = irDocument.Children.Last().Children[2] as SetTagHelperPropertyIRNode;

            var writer = new DesignTimeTagHelperWriter();
            var context = GetCSharpRenderingContext(writer, codeDocument);

            // Act
            writer.WriteSetTagHelperProperty(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"#line 3 ""test.cshtml""
__InputTagHelper.FooProp = 42;

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteSetTagHelperProperty_IndexerAttribute_RendersCorrectly()
        {
            // Arrange
            var descriptors = new[]
            {
                CreateTagHelperDescriptor(
                    tagName: "input",
                    typeName: "InputTagHelper",
                    assemblyName: "TestAssembly",
                    attributes: new Action<BoundAttributeDescriptorBuilder>[]
                    {
                        builder => builder
                            .Name("bound")
                            .PropertyName("FooProp")
                            .TypeName("System.Collections.Generic.Dictionary<System.String, System.Int32>")
                            .AsDictionary("foo-", "System.Int32"),
                    })
            };
            var engine = RazorEngine.Create(builder => builder.AddTagHelpers(descriptors));
            var content = @"
@addTagHelper *, TestAssembly
<input foo-bound=""42"" />";
            var sourceDocument = TestRazorSourceDocument.Create(content);
            var codeDocument = RazorCodeDocument.Create(sourceDocument);
            var irDocument = Lower(codeDocument, engine);
            var node = irDocument.Children.Last().Children[2] as SetTagHelperPropertyIRNode;

            var writer = new DesignTimeTagHelperWriter();
            var context = GetCSharpRenderingContext(writer, codeDocument);

            // Act
            writer.WriteSetTagHelperProperty(context, node);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"#line 3 ""test.cshtml""
__InputTagHelper.FooProp[""bound""] = 42;

#line default
#line hidden
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        private static CSharpRenderingContext GetCSharpRenderingContext(TagHelperWriter writer, RazorCodeDocument codeDocument = null)
        {
            var options = RazorCodeGenerationOptions.CreateDefault();
            var codeWriter = new Legacy.CSharpCodeWriter();
            var context = new CSharpRenderingContext()
            {
                Writer = codeWriter,
                Options = options,
                BasicWriter = new DesignTimeBasicWriter(),
                TagHelperWriter = writer,
                TagHelperRenderingContext = new TagHelperRenderingContext(),
                CodeDocument = codeDocument,
                RenderChildren = n =>
                {
                    codeWriter.WriteLine("Render Children");
                }
            };

            return context;
        }

        private static DocumentIRNode Lower(RazorCodeDocument codeDocument)
        {
            var engine = RazorEngine.Create();

            return Lower(codeDocument, engine);
        }

        private static DocumentIRNode Lower(RazorCodeDocument codeDocument, RazorEngine engine)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorIRLoweringPhase)
                {
                    break;
                }
            }

            var irDocument = codeDocument.GetIRDocument();
            Assert.NotNull(irDocument);

            return irDocument;
        }

        private static TagHelperDescriptor CreateTagHelperDescriptor(
            string tagName,
            string typeName,
            string assemblyName,
            IEnumerable<Action<BoundAttributeDescriptorBuilder>> attributes = null)
        {
            var builder = TagHelperDescriptorBuilder.Create(typeName, assemblyName);

            if (attributes != null)
            {
                foreach (var attributeBuilder in attributes)
                {
                    builder.BindAttribute(attributeBuilder);
                }
            }

            builder.TagMatchingRule(ruleBuilder => ruleBuilder.RequireTagName(tagName));

            var descriptor = builder.Build();

            return descriptor;
        }
    }
}
