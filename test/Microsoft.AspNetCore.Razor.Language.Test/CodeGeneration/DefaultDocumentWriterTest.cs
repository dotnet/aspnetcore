// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class DefaultDocumentWriterTest
    {
        [Fact]
        public void WriteDocument_WritesNamespace()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var options = RazorCodeGenerationOptions.CreateDefault();

            var target = CodeTarget.CreateDefault(codeDocument, options);
            var context = new CSharpRenderingContext()
            {
                Options = options,
                Writer = new Legacy.CSharpCodeWriter(),
            };

            var writer = new DefaultDocumentWriter(target, context);

            var document = new DocumentIRNode();
            var builder = RazorIRBuilder.Create(document);
            builder.Add(new NamespaceDeclarationIRNode()
            {
                Content = "TestNamespace",
            });

            // Act
            writer.WriteDocument(document);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"namespace TestNamespace
{
    #line hidden
}
", 
                csharp, 
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteDocument_WritesClass()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var options = RazorCodeGenerationOptions.CreateDefault();

            var target = CodeTarget.CreateDefault(codeDocument, options);
            var context = new CSharpRenderingContext()
            {
                Options = options,
                Writer = new Legacy.CSharpCodeWriter(),
            };
            var writer = new DefaultDocumentWriter(target, context);

            var document = new DocumentIRNode();
            var builder = RazorIRBuilder.Create(document);
            builder.Add(new ClassDeclarationIRNode()
            {
                AccessModifier = "internal",
                BaseType = "TestBase",
                Interfaces = new List<string> { "IFoo", "IBar", },
                Name = "TestClass",
            });

            // Act
            writer.WriteDocument(document);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"internal class TestClass : TestBase, IFoo, IBar
{
}
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteDocument_WritesMethod()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var options = RazorCodeGenerationOptions.CreateDefault();

            var target = CodeTarget.CreateDefault(codeDocument, options);
            var context = new CSharpRenderingContext()
            {
                Options = options,
                Writer = new Legacy.CSharpCodeWriter(),
            };
            var writer = new DefaultDocumentWriter(target, context);

            var document = new DocumentIRNode();
            var builder = RazorIRBuilder.Create(document);
            builder.Add(new MethodDeclarationIRNode()
            {
                AccessModifier = "internal",
                Modifiers = new List<string> { "virtual", "async", },
                Name = "TestMethod",
                ReturnType = "string",
            });

            // Act
            writer.WriteDocument(document);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"#pragma warning disable 1998
internal virtual async string TestMethod()
{
}
#pragma warning restore 1998
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteDocument_WritesField()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var options = RazorCodeGenerationOptions.CreateDefault();

            var target = CodeTarget.CreateDefault(codeDocument, options);
            var context = new CSharpRenderingContext()
            {
                Options = options,
                Writer = new Legacy.CSharpCodeWriter(),
            };
            var writer = new DefaultDocumentWriter(target, context);

            var document = new DocumentIRNode();
            var builder = RazorIRBuilder.Create(document);
            builder.Add(new FieldDeclarationIRNode()
            {
                AccessModifier = "internal",
                Modifiers = new List<string> { "readonly", },
                Name = "_foo",
                Type = "string",
            });

            // Act
            writer.WriteDocument(document);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"internal readonly string _foo;
",
                csharp,
                ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void WriteDocument_WritesProperty()
        {
            // Arrange
            var codeDocument = TestRazorCodeDocument.CreateEmpty();
            var options = RazorCodeGenerationOptions.CreateDefault();

            var target = CodeTarget.CreateDefault(codeDocument, options);
            var context = new CSharpRenderingContext()
            {
                Options = options,
                Writer = new Legacy.CSharpCodeWriter(),
            };
            var writer = new DefaultDocumentWriter(target, context);

            var document = new DocumentIRNode();
            var builder = RazorIRBuilder.Create(document);
            builder.Add(new PropertyDeclarationIRNode()
            {
                AccessModifier = "internal",
                Modifiers = new List<string> { "virtual", },
                Name = "Foo",
                Type = "string",
            });

            // Act
            writer.WriteDocument(document);

            // Assert
            var csharp = context.Writer.Builder.ToString();
            Assert.Equal(
@"internal virtual string Foo { get; set; }
",
                csharp,
                ignoreLineEndingDifferences: true);
        }
    }
}
