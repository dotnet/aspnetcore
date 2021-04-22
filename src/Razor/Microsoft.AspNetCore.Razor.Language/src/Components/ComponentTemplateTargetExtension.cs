// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    internal class ComponentTemplateTargetExtension : ITemplateTargetExtension
    {
        public void WriteTemplate(CodeRenderingContext context, TemplateIntermediateNode node)
        {
            // This is OK because this will only be plugged in by the component code target
            // not globally.
            ((ComponentNodeWriter)context.NodeWriter).WriteTemplate(context, node);
        }
    }
}
