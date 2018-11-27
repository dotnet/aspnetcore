// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class SuppressPrimaryMethodBodyIntegrationTest : IntegrationTestBase
    {
        public SuppressPrimaryMethodBodyIntegrationTest()
            : base(generateBaselines: null)
        {
            Configuration = RazorConfiguration.Default;
            FileExtension = ".razor";
        }

        protected override RazorConfiguration Configuration { get; }

        [Fact]
        public void BasicTest()
        {
            var engine = CreateProjectEngine(e =>
            {
                e.Features.Add(new SetSuppressPrimaryMethodBodyFeature());
                e.Features.Add(new InputDocumentKindClassifierPass());
            });

            var projectItem = CreateProjectItemFromFile();

            // Act
            var codeDocument = engine.Process(projectItem);

            // Assert
            AssertDocumentNodeMatchesBaseline(codeDocument.GetDocumentIntermediateNode());

            var csharpDocument = codeDocument.GetCSharpDocument();
            AssertCSharpDocumentMatchesBaseline(csharpDocument);
            Assert.Empty(csharpDocument.Diagnostics);
        }

        private class SetSuppressPrimaryMethodBodyFeature : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
        {
            public int Order { get; set; }

            public void Configure(RazorCodeGenerationOptionsBuilder options)
            {
                options.SuppressPrimaryMethodBody = true;
            }
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