// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Evolution.Intermediate.RazorIRAssert;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    // We're purposely lean on tests here because the functionality is well covered by
    // integration tests, and is mostly implemented by the base class.
    public class DefaultDocumentClassifierPassTest
    {
        [Fact]
        public void Execute_IgnoresDocumentsWithDocumentKind()
        {
            // Arrange
            var irDocument = new DocumentIRNode()
            {
                DocumentKind = "ignore",
                Options = RazorParserOptions.CreateDefaultOptions(),
            };

            var pass = new DefaultDocumentClassifierPass();
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
            var irDocument = new DocumentIRNode()
            {
                Options = RazorParserOptions.CreateDefaultOptions(),
            };

            var pass = new DefaultDocumentClassifierPass();
            pass.Engine = RazorEngine.CreateEmpty(b =>{ });

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), irDocument);

            // Assert
            Assert.Equal("default", irDocument.DocumentKind);

            var @namespace = SingleChild<NamespaceDeclarationIRNode>(irDocument);
            var @class = SingleChild<ClassDeclarationIRNode>(@namespace);
            var method = SingleChild<RazorMethodDeclarationIRNode>(@class);
            NoChildren(method);
        }
    }
}
