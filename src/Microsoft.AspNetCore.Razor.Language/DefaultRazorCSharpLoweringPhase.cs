// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorCSharpLoweringPhase : RazorEnginePhaseBase, IRazorCSharpLoweringPhase
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var documentNode = codeDocument.GetDocumentIntermediateNode();
            ThrowForMissingDocumentDependency(documentNode);

            var target = documentNode.Target;
            if (target == null)
            {
                var message = Resources.FormatDocumentMissingTarget(
                    documentNode.DocumentKind,
                    nameof(CodeTarget),
                    nameof(DocumentIntermediateNode.Target));
                throw new InvalidOperationException(message);
            }

            var writer = new DefaultDocumentWriter(documentNode.Target, documentNode.Options);
            var cSharpDocument = writer.WriteDocument(codeDocument, documentNode);
            codeDocument.SetCSharpDocument(cSharpDocument);
        }
    }
}
