// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal class TagHelperHtmlAttributeRenderingConventions : CSharpRenderingConventions
    {
        public TagHelperHtmlAttributeRenderingConventions(CSharpCodeWriter writer) : base(writer)
        {
        }

        public override string StartWriteAttributeValueMethod => "AddHtmlAttributeValue(" /* ORIGINAL: AddHtmlAttributeValueMethodName */;
    }
}
