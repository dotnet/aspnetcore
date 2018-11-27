// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.IntegrationTests
{
    public class ComponentIntegrationTest : IntegrationTestBase
    {
        public ComponentIntegrationTest()
            : base(generateBaselines: null)
        {
            Configuration = RazorConfiguration.Default;
            FileExtension = ".razor";
        }

        protected override RazorConfiguration Configuration { get; }

        [Fact]
        public void BasicTest()
        {
            var projectEngine = CreateProjectEngine(engine =>
            {
                engine.Features.Add(new InputDocumentKindClassifierPass());
            });

            var projectItem = CreateProjectItemFromFile();

            // Act
            var codeDocument = projectEngine.Process(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());
            AssertCSharpDocumentMatchesBaseline(codeDocument.GetCSharpDocument());
        }

        private class InputDocumentKindClassifierPass : RazorEngineFeatureBase, IRazorDocumentClassifierPass
        {
            // Run before other document classifiers
            public int Order => -1000;

            public void Execute(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
            {
                codeDocument.SetInputDocumentKind(InputDocumentKind.Component);
            }
        }
    }
}
