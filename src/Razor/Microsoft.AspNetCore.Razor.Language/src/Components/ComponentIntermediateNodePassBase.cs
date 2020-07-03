// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
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
}
