// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentTemplateTargetExtension : ITemplateTargetExtension
{
    public void WriteTemplate(CodeRenderingContext context, TemplateIntermediateNode node)
    {
        // This is OK because this will only be plugged in by the component code target
        // not globally.
        ((ComponentNodeWriter)context.NodeWriter).WriteTemplate(context, node);
    }
}
