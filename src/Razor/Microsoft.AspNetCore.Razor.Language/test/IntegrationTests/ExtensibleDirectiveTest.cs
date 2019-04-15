// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    // Extensible directives only have codegen for design time, so we're only testing that.
    public class ExtensibleDirectiveTest : IntegrationTestBase
    {
        public ExtensibleDirectiveTest()
            : base(generateBaselines: null)
        {
        }

        [Fact]
        public void NamespaceToken()
        {
            // Arrange
            var engine = CreateProjectEngine(builder =>
            {
                builder.ConfigureDocumentClassifier();

                builder.AddDirective(DirectiveDescriptor.CreateDirective("custom", DirectiveKind.SingleLine, b => b.AddNamespaceToken()));
            });

            var projectItem = CreateProjectItemFromFile();

            // Act
            var codeDocument = engine.ProcessDesignTime(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(codeDocument.GetCSharpDocument());
            AssertSourceMappingsMatchBaseline(codeDocument);
        }
    }
}
