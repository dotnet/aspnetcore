// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class BlazorTemplateTargetExtension : ITemplateTargetExtension
    {
        public void WriteTemplate(CodeRenderingContext context, TemplateIntermediateNode node)
        {
            ((BlazorNodeWriter)context.NodeWriter).WriteTemplate(context, node);
        }
    }
}
