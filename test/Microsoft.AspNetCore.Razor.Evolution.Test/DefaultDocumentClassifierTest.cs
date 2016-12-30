// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Evolution.Intermediate.RazorIRAssert;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public class DefaultDocumentClassifierTest
    {
        [Fact]
        public void Execute_IgnoresDocumentsWithDocumentKind()
        {
            // Arrange
            var irDocument = new DocumentIRNode()
            {
                DocumentKind = "ignore",
            };

            var pass = new DefaultDocumentClassifier();
            pass.Engine = RazorEngine.CreateEmpty(b => { });

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Assert.Equal("ignore", irDocument.DocumentKind);
            NoChildren(irDocument);
        }

        [Fact]
        public void Execute_CreatesClassStructure()
        {
            // Arrange
            var irDocument = new DocumentIRNode();

            var pass = new DefaultDocumentClassifier();
            pass.Engine = RazorEngine.CreateEmpty(b =>{ });

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Assert.Equal(DefaultDocumentClassifier.DocumentKind, irDocument.DocumentKind);

            var @namespace = SingleChild<NamespaceDeclarationIRNode>(irDocument);
            var @class = SingleChild<ClassDeclarationIRNode>(@namespace);
            var method = SingleChild<RazorMethodDeclarationIRNode>(@class);
            NoChildren(method);
        }

        [Fact]
        public void Execute_AddsCheckumFirstToDocument()
        {
            // Arrange
            var irDocument = new DocumentIRNode();

            var builder = RazorIRBuilder.Create(irDocument);
            builder.Add(new ChecksumIRNode());

            var pass = new DefaultDocumentClassifier();
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
            var irDocument = new DocumentIRNode();

            var builder = RazorIRBuilder.Create(irDocument);
            builder.Add(new UsingStatementIRNode());

            var pass = new DefaultDocumentClassifier();
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
            var irDocument = new DocumentIRNode();

            var builder = RazorIRBuilder.Create(irDocument);
            builder.Add(new DeclareTagHelperFieldsIRNode());

            var pass = new DefaultDocumentClassifier();
            pass.Engine = RazorEngine.CreateEmpty(b => { });

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            var @namespace = SingleChild<NamespaceDeclarationIRNode>(irDocument);
            var @class = SingleChild<ClassDeclarationIRNode>(@namespace);
            Children(
                @class,
                n => Assert.IsType<DeclareTagHelperFieldsIRNode>(n),
                n => Assert.IsType<RazorMethodDeclarationIRNode>(n));
        }

        [Fact]
        public void Execute_AddsTheRestToMethod()
        {
            // Arrange
            var irDocument = new DocumentIRNode();

            var builder = RazorIRBuilder.Create(irDocument);
            builder.Add(new HtmlContentIRNode());
            builder.Add(new CSharpStatementIRNode());

            var pass = new DefaultDocumentClassifier();
            pass.Engine = RazorEngine.CreateEmpty(b => { });

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            var @namespace = SingleChild<NamespaceDeclarationIRNode>(irDocument);
            var @class = SingleChild<ClassDeclarationIRNode>(@namespace);
            var method = SingleChild<RazorMethodDeclarationIRNode>(@class);
            Children(
                method,
                n => Assert.IsType<HtmlContentIRNode>(n),
                n => Assert.IsType<CSharpStatementIRNode>(n));
        }
    }
}
