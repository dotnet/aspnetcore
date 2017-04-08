// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class TagHelperHtmlAttributeRuntimeBasicWriter : RuntimeBasicWriter
    {
        // This will be used when HtmlAttributeValueIRNode and CSharpAttributeValueIRNode are moved to writers.
        public new string WriteAttributeValueMethod { get; set; } = "AddHtmlAttributeValue";
    }
}
