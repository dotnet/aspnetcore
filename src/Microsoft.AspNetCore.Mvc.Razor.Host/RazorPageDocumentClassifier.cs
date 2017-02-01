// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Host
{
    public class RazorPageDocumentClassifier : BaseDocumentClassifierPass
    {
        public static readonly string DocumentKind = "mvc.1.0.razor-page";

        protected override string BaseType => "Microsoft.AspNetCore.Mvc.RazorPages.Page";

        protected override string ClassifyDocument(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            string routePrefix;
            if (PageDirective.TryGetRouteTemplate(irDocument, out routePrefix))
            {
                return DocumentKind;
            }

            return null;
        }
    }
}
