// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Host
{
    public class MvcViewDocumentClassifierPass : BaseDocumentClassifierPass
    {
        public static readonly string DocumentKind = "mvc.1.0.view";

        protected override string BaseType => "Microsoft.AspNetCore.Mvc.Razor.RazorPage<TModel>";

        protected override string ClassifyDocument(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
            => DocumentKind;
    }
}
