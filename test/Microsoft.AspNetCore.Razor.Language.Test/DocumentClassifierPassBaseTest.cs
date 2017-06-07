// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.RazorIRAssert;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DocumentClassifierPassBaseTest
    {
        [Fact]
        public void Execute_HasDocumentKind_IgnoresDocument()
        {
            // Arrange
            var irDocument = new DocumentIRNode()
            {
                DocumentKind = "ignore",
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var pass = new TestDocumentClassifierPass();
            pass.Engine = RazorEngine.CreateEmpty(b => { });

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Assert.Equal("ignore", irDocument.DocumentKind);
            NoChildren(irDocument);
        }

        [Fact]
        public void Execute_NoMatch_IgnoresDocument()
        {
            // Arrange
            var irDocument = new DocumentIRNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var pass = new TestDocumentClassifierPass()
            {
                Engine = RazorEngine.CreateEmpty(b => { }),
                ShouldMatch = false,
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Assert.Null(irDocument.DocumentKind);
            NoChildren(irDocument);
        }

        [Fact]
        public void Execute_Match_AddsGlobalTargetExtensions()
        {
            // Arrange
            var irDocument = new DocumentIRNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var expected = new ICodeTargetExtension[]
            {
                new MyExtension1(),
                new MyExtension2(),
            };

            var pass = new TestDocumentClassifierPass();
            pass.Engine = RazorEngine.CreateEmpty(b =>
            {
                for (var i = 0; i < expected.Length; i++)
                {
                    b.AddTargetExtension(expected[i]);
                }
            });

            ICodeTargetExtension[] extensions = null;

            pass.CodeTargetCallback = (builder) => extensions = builder.TargetExtensions.ToArray();

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Assert.Equal(expected, extensions);
        }

        [Fact]
        public void Execute_Match_SetsDocumentType_AndCreatesStructure()
        {
            // Arrange
            var irDocument = new DocumentIRNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var pass = new TestDocumentClassifierPass();
            pass.Engine = RazorEngine.CreateEmpty(b => { });

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Assert.Equal("test", irDocument.DocumentKind);
            Assert.NotNull(irDocument.Target);

            var @namespace = SingleChild<NamespaceDeclarationIRNode>(irDocument);
            var @class = SingleChild<ClassDeclarationIRNode>(@namespace);
            var method = SingleChild<MethodDeclarationIRNode>(@class);
            NoChildren(method);
        }

        [Fact]
        public void Execute_AddsCheckumFirstToDocument()
        {
            // Arrange
            var irDocument = new DocumentIRNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var builder = RazorIRBuilder.Create(irDocument);
            builder.Add(new ChecksumIRNode());

            var pass = new TestDocumentClassifierPass();
            pass.Engine = RazorEngine.CreateEmpty(b => { });

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Children(
                irDocument,
                n => Assert.IsType<ChecksumIRNode>(n),
                n => Assert.IsType<NamespaceDeclarationIRNode>(n));
        }

        [Fact]
        public void Execute_AddsUsingsToNamespace()
        {
            // Arrange
            var irDocument = new DocumentIRNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var builder = RazorIRBuilder.Create(irDocument);
            builder.Add(new UsingStatementIRNode());

            var pass = new TestDocumentClassifierPass();
            pass.Engine = RazorEngine.CreateEmpty(b => { });

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            var @namespace = SingleChild<NamespaceDeclarationIRNode>(irDocument);
            Children(
                @namespace,
                n => Assert.IsType<UsingStatementIRNode>(n),
                n => Assert.IsType<ClassDeclarationIRNode>(n));
        }

        [Fact]
        public void Execute_AddsTagHelperFieldsToClass()
        {
            // Arrange
            var irDocument = new DocumentIRNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var builder = RazorIRBuilder.Create(irDocument);
            builder.Add(new DeclareTagHelperFieldsIRNode());

            var pass = new TestDocumentClassifierPass();
            pass.Engine = RazorEngine.CreateEmpty(b => { });

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            var @namespace = SingleChild<NamespaceDeclarationIRNode>(irDocument);
            var @class = SingleChild<ClassDeclarationIRNode>(@namespace);
            Children(
                @class,
                n => Assert.IsType<DeclareTagHelperFieldsIRNode>(n),
                n => Assert.IsType<MethodDeclarationIRNode>(n));
        }

        [Fact]
        public void Execute_AddsTheRestToMethod()
        {
            // Arrange
            var irDocument = new DocumentIRNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var builder = RazorIRBuilder.Create(irDocument);
            builder.Add(new HtmlContentIRNode());
            builder.Add(new CSharpCodeIRNode());

            var pass = new TestDocumentClassifierPass();
            pass.Engine = RazorEngine.CreateEmpty(b => { });

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            var @namespace = SingleChild<NamespaceDeclarationIRNode>(irDocument);
            var @class = SingleChild<ClassDeclarationIRNode>(@namespace);
            var method = SingleChild<MethodDeclarationIRNode>(@class);
            Children(
                method,
                n => Assert.IsType<HtmlContentIRNode>(n),
                n => Assert.IsType<CSharpCodeIRNode>(n));
        }

        [Fact]
        public void Execute_CanInitializeDefaults()
        {
            // Arrange
            var irDocument = new DocumentIRNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var builder = RazorIRBuilder.Create(irDocument);
            builder.Add(new HtmlContentIRNode());
            builder.Add(new CSharpCodeIRNode());

            var pass = new TestDocumentClassifierPass()
            {
                Engine = RazorEngine.CreateEmpty(b => { }),
                Namespace = "TestNamespace",
                Class = "TestClass",
                Method = "TestMethod",
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            var @namespace = SingleChild<NamespaceDeclarationIRNode>(irDocument);
            Assert.Equal("TestNamespace", @namespace.Content);

            var @class = SingleChild<ClassDeclarationIRNode>(@namespace);
            Assert.Equal("TestClass", @class.Name);

            var method = SingleChild<MethodDeclarationIRNode>(@class);
            Assert.Equal("TestMethod", method.Name);
        }

        [Fact]
        public void Execute_AddsPrimaryAnnotations()
        {
            // Arrange
            var irDocument = new DocumentIRNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var builder = RazorIRBuilder.Create(irDocument);
            builder.Add(new HtmlContentIRNode());
            builder.Add(new CSharpCodeIRNode());

            var pass = new TestDocumentClassifierPass()
            {
                Engine = RazorEngine.CreateEmpty(b => { }),
                Namespace = "TestNamespace",
                Class = "TestClass",
                Method = "TestMethod",
            };

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            var @namespace = SingleChild<NamespaceDeclarationIRNode>(irDocument);
            AnnotationEquals(@namespace, CommonAnnotations.PrimaryNamespace);

            var @class = SingleChild<ClassDeclarationIRNode>(@namespace);
            AnnotationEquals(@class, CommonAnnotations.PrimaryClass);

            var method = SingleChild<MethodDeclarationIRNode>(@class);
            AnnotationEquals(method, CommonAnnotations.PrimaryMethod);
        }

        private class TestDocumentClassifierPass : DocumentClassifierPassBase
        {
            public override int Order => RazorIRPassBase.DefaultFeatureOrder;

            public bool ShouldMatch { get; set; } = true;

            public Action<ICodeTargetBuilder> CodeTargetCallback { get; set; }

            public string Namespace { get; set;  }

            public string Class { get; set; }

            public string Method { get; set; }

            protected override string DocumentKind => "test";

            protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
            {
                return ShouldMatch;
            }

            protected override void OnDocumentStructureCreated(
                RazorCodeDocument codeDocument,
                NamespaceDeclarationIRNode @namespace,
                ClassDeclarationIRNode @class,
                MethodDeclarationIRNode method)
            {
                @namespace.Content = Namespace;
                @class.Name = Class;
                @method.Name = Method;
            }

            protected override void ConfigureTarget(ICodeTargetBuilder builder)
            {
                CodeTargetCallback?.Invoke(builder);
            }
        }

        private class MyExtension1 : ICodeTargetExtension
        {
        }

        private class MyExtension2 : ICodeTargetExtension
        {
        }
    }
}
