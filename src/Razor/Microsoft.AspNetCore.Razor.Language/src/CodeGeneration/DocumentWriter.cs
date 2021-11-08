// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public abstract class DocumentWriter
{
    public static DocumentWriter CreateDefault(CodeTarget codeTarget, RazorCodeGenerationOptions options)
    {
        if (codeTarget == null)
        {
            throw new ArgumentNullException(nameof(codeTarget));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        return new DefaultDocumentWriter(codeTarget, options);
    }

    public abstract RazorCSharpDocument WriteDocument(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode);
}
