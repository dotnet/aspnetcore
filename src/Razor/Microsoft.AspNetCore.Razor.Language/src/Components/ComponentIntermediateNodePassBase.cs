// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal abstract class ComponentIntermediateNodePassBase : IntermediateNodePassBase
{
    protected bool IsComponentDocument(DocumentIntermediateNode document)
    {
        if (document == null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        return string.Equals(document.DocumentKind, ComponentDocumentClassifierPass.ComponentDocumentKind, StringComparison.Ordinal);
    }
}
