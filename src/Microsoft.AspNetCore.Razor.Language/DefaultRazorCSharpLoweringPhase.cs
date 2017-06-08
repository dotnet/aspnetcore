// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorCSharpLoweringPhase : RazorEnginePhaseBase, IRazorCSharpLoweringPhase
    {
        internal static readonly object NewLineString = "NewLineString";

        internal static readonly object SuppressUniqueIds = "SuppressUniqueIds";

        protected override void ExecuteCore(RazorCodeDocument codeDocument)
        {
            var irDocument = codeDocument.GetIRDocument();
            ThrowForMissingDocumentDependency(irDocument);

            var target = irDocument.Target;
            if (target == null)
            {
                var message = Resources.FormatDocumentMissingTarget(
                    irDocument.DocumentKind,
                    nameof(CodeTarget),
                    nameof(DocumentIRNode.Target));
                throw new InvalidOperationException(message);
            }

            var codeWriter = new CSharpCodeWriter();
            var newLineString = codeDocument.Items[NewLineString];
            if (newLineString != null)
            {
                // Set new line character to a specific string regardless of platform, for testing purposes.
                codeWriter.NewLine = (string)newLineString;
            }

            var renderingContext = new CSharpRenderingContext()
            {
                Writer = codeWriter,
                CodeDocument = codeDocument,
                Options = irDocument.Options,
            };

            var idValue = codeDocument.Items[SuppressUniqueIds];
            if (idValue != null)
            {
                // Generate a static value for unique ids instead of a guid, for testing purposes.
                renderingContext.IdGenerator = () => idValue.ToString();
            }

            var documentWriter = target.CreateWriter(renderingContext);
            documentWriter.WriteDocument(irDocument);

            var diagnostics = new List<RazorDiagnostic>();
            diagnostics.AddRange(irDocument.GetAllDiagnostics());
            diagnostics.AddRange(renderingContext.Diagnostics);

            var csharpDocument = RazorCSharpDocument.Create(
                renderingContext.Writer.GenerateCode(),
                irDocument.Options,
                diagnostics,
                renderingContext.LineMappings);
            codeDocument.SetCSharpDocument(csharpDocument);
        }
    }
}
