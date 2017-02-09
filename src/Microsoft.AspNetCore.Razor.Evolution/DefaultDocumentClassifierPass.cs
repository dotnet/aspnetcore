// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultDocumentClassifierPass : DocumentClassifierPassBase
    {
        public override int Order => RazorIRPass.DefaultDocumentClassifierOrder;

        protected override string DocumentKind => "default";

        protected override bool IsMatch(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            return true;
        }
    }
}
