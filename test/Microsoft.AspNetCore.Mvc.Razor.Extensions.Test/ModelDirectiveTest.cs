// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class ModelDirectiveTest
    {
        [Fact]
        public void ModelDirective_GetModelType_GetsTypeFromLastWellFormedDirective()
        {
            // Arrange
            var codeDocument = CreateDocument(@"
@model Type1
@model Type2
@model
");

            var engine = CreateEngine();

            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            var result = ModelDirective.GetModelType(irDocument);

            // Assert
            Assert.Equal("Type2", result);
        }

        [Fact]
        public void ModelDirective_GetModelType_DefaultsToDynamic()
        {
            // Arrange
            var codeDocument = CreateDocument(@" ");

            var engine = CreateEngine();

            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            var result = ModelDirective.GetModelType(irDocument);

            // Assert
            Assert.Equal("dynamic", result);
        }

        [Fact]
        public void ModelDirectivePass_Execute_ReplacesTModelInBaseType()
        {
            // Arrange
            var codeDocument = CreateDocument(@"
@inherits BaseType<TModel>
@model Type1
");

            var engine = CreateEngine();
            var pass = new ModelDirective.Pass(designTime: false)
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var @class = FindClassNode(irDocument);
            Assert.NotNull(@class);
            Assert.Equal("BaseType<Type1>", @class.BaseType);
        }

        [Fact]
        public void ModelDirectivePass_Execute_ReplacesTModelInBaseType_DifferentOrdering()
        {
            // Arrange
            var codeDocument = CreateDocument(@"
@model Type1
@inherits BaseType<TModel>
@model Type2
");

            var engine = CreateEngine();
            var pass = new ModelDirective.Pass(designTime: false)
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var @class = FindClassNode(irDocument);
            Assert.NotNull(@class);
            Assert.Equal("BaseType<Type2>", @class.BaseType);
        }

        [Fact]
        public void ModelDirectivePass_Execute_NoOpWithoutTModel()
        {
            // Arrange
            var codeDocument = CreateDocument(@"
@inherits BaseType
@model Type1
");

            var engine = CreateEngine();
            var pass = new ModelDirective.Pass(designTime: false)
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var @class = FindClassNode(irDocument);
            Assert.NotNull(@class);
            Assert.Equal("BaseType", @class.BaseType);
        }

        [Fact]
        public void ModelDirectivePass_Execute_ReplacesTModelInBaseType_DefaultDynamic()
        {
            // Arrange
            var codeDocument = CreateDocument(@"
@inherits BaseType<TModel>
");

            var engine = CreateEngine();
            var pass = new ModelDirective.Pass(designTime: false)
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var @class = FindClassNode(irDocument);
            Assert.NotNull(@class);
            Assert.Equal("BaseType<dynamic>", @class.BaseType);
        }

        [Fact]
        public void ModelDirectivePass_DesignTime_AddsTModelUsingStatement()
        {
            // Arrange
            var codeDocument = CreateDocument(@"
@inherits BaseType<TModel>
");

            var engine = CreateEngine();
            var pass = new ModelDirective.Pass(designTime: true)
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var @class = FindClassNode(irDocument);
            Assert.NotNull(@class);
            Assert.Equal("BaseType<dynamic>", @class.BaseType);

            var @namespace = FindNamespaceNode(irDocument);
            var usingNode = Assert.IsType<UsingStatementIRNode>(@namespace.Children[0]);
            Assert.Equal($"TModel = global::{typeof(object).FullName}", usingNode.Content);
        }

        [Fact]
        public void ModelDirectivePass_DesignTime_WithModel_AddsTModelUsingStatement()
        {
            // Arrange
            var codeDocument = CreateDocument(@"
@inherits BaseType<TModel>
@model SomeType
");

            var engine = CreateEngine();
            var pass = new ModelDirective.Pass(designTime: true)
            {
                Engine = engine,
            };

            var irDocument = CreateIRDocument(engine, codeDocument);

            // Act
            pass.Execute(codeDocument, irDocument);

            // Assert
            var @class = FindClassNode(irDocument);
            Assert.NotNull(@class);
            Assert.Equal("BaseType<SomeType>", @class.BaseType);

            var @namespace = FindNamespaceNode(irDocument);
            var usingNode = Assert.IsType<UsingStatementIRNode>(@namespace.Children[0]);
            Assert.Equal($"TModel = global::System.Object", usingNode.Content);
        }

        private RazorCodeDocument CreateDocument(string content)
        {
            var source = RazorSourceDocument.Create(content, "test.cshtml");
            return RazorCodeDocument.Create(source);
        }

        private ClassDeclarationIRNode FindClassNode(RazorIRNode node)
        {
            var visitor = new ClassNodeVisitor();
            visitor.Visit(node);
            return visitor.Node;
        }

        private NamespaceDeclarationIRNode FindNamespaceNode(RazorIRNode node)
        {
            var visitor = new NamespaceNodeVisitor();
            visitor.Visit(node);
            return visitor.Node;
        }

        private RazorEngine CreateEngine()
        {
            return RazorEngine.Create(b =>
            {
                // Notice we're not registering the ModelDirective.Pass here so we can run it on demand.
                b.AddDirective(ModelDirective.Directive);
            });
        }

        private DocumentIRNode CreateIRDocument(RazorEngine engine, RazorCodeDocument codeDocument)
        {
            for (var i = 0; i < engine.Phases.Count; i++)
            {
                var phase = engine.Phases[i];
                phase.Execute(codeDocument);

                if (phase is IRazorDocumentClassifierPhase)
                {
                    break;
                }
            }

            return codeDocument.GetIRDocument();
        }

        private string GetCSharpContent(RazorIRNode node)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i] as RazorIRToken;
                if (child.Kind == RazorIRToken.TokenKind.CSharp)
                {
                    builder.Append(child.Content);
                }
            }

            return builder.ToString();
        }

        private class ClassNodeVisitor : RazorIRNodeWalker
        {
            public ClassDeclarationIRNode Node { get; set; }

            public override void VisitClassDeclaration(ClassDeclarationIRNode node)
            {
                Node = node;
            }
        }

        private class NamespaceNodeVisitor : RazorIRNodeWalker
        {
            public NamespaceDeclarationIRNode Node { get; set; }

            public override void VisitNamespaceDeclaration(NamespaceDeclarationIRNode node)
            {
                Node = node;
            }
        }
    }
}