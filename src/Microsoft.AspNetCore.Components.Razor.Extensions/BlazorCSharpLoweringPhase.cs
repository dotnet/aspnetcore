// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class BlazorRazorCSharpLoweringPhase : RazorEnginePhaseBase, IRazorCSharpLoweringPhase
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var documentNode = codeDocument.GetDocumentIntermediateNode();
            ThrowForMissingDocumentDependency(documentNode);
#pragma warning disable CS0618
            var writer = new DocumentWriterWorkaround().Create(documentNode.Target, documentNode.Options);
#pragma warning restore CS0618
            try
            {
                var cSharpDocument = writer.WriteDocument(codeDocument, documentNode);
                codeDocument.SetCSharpDocument(cSharpDocument);
            }
            catch (RazorCompilerException ex)
            {
                // Currently the Blazor code generation has some 'fatal errors' that can cause code generation
                // to fail completely. This class is here to make that implementation work gracefully.
                var cSharpDocument = RazorCSharpDocument.Create("", documentNode.Options, new[] { ex.Diagnostic });
                codeDocument.SetCSharpDocument(cSharpDocument);
            }
        }

        private class DocumentWriterWorkaround : DocumentWriter
        {
            public override RazorCSharpDocument WriteDocument(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
            {
                throw new NotImplementedException();
            }
        }
    }
}