// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    // Extensible directives only have codegen for design time, so we're only testing that.
    public class ExtensibleDirectiveTest : IntegrationTestBase
    {
        [Fact]
        public void NamespaceToken()
        {
            // Arrange
            var engine = RazorEngine.CreateDesignTime(builder =>
            {
                builder.Features.Add(new ApiSetsIRTestAdapter());

                builder.AddDirective(DirectiveDescriptorBuilder.Create("custom").AddNamespace().Build());
            });

            var document = CreateCodeDocument();

            // Act
            engine.Process(document);

            // Assert
            AssertIRMatchesBaseline(document.GetIRDocument());
            AssertDesignTimeDocumentMatchBaseline(document);
        }
    }
}
