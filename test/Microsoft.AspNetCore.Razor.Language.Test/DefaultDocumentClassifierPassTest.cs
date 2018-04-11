// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;
using static Microsoft.AspNetCore.Razor.Language.Intermediate.IntermediateNodeAssert;

namespace Microsoft.AspNetCore.Razor.Language
{
    // We're purposely lean on tests here because the functionality is well covered by
    // integration tests, and is mostly implemented by the base class.
    public class DefaultDocumentClassifierPassTest
    {
        [Fact]
        public void Execute_IgnoresDocumentsWithDocumentKind()
        {
            // Arrange
            var documentNode = new DocumentIntermediateNode()
            {
                DocumentKind = "ignore",
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var pass = new DefaultDocumentClassifierPass();
            pass.Engine = RazorProjectEngine.Create().Engine;

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), documentNode);

            // Assert
            Assert.Equal("ignore", documentNode.DocumentKind);
            NoChildren(documentNode);
        }

        [Fact]
        public void Execute_CreatesClassStructure()
        {
            // Arrange
            var documentNode = new DocumentIntermediateNode()
            {
                Options = RazorCodeGenerationOptions.CreateDefault(),
            };

            var pass = new DefaultDocumentClassifierPass();
            pass.Engine = RazorProjectEngine.Create().Engine;

            // Act
            pass.Execute(TestRazorCodeDocument.CreateEmpty(), documentNode);

            // Assert
            Assert.Equal("default", documentNode.DocumentKind);

            var @namespace = SingleChild<NamespaceDeclarationIntermediateNode>(documentNode);
            var @class = SingleChild<ClassDeclarationIntermediateNode>(@namespace);
            var method = SingleChild<MethodDeclarationIntermediateNode>(@class);
            NoChildren(method);
        }
    }
}
