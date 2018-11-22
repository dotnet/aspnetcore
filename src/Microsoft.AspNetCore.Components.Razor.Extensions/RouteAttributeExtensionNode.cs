// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.Shared;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class RouteAttributeExtensionNode : ExtensionIntermediateNode
    {
        public RouteAttributeExtensionNode(string template)
        {
            Template = template;
        }

        public string Template { get; }

        public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

        public override void Accept(IntermediateNodeVisitor visitor) => AcceptExtensionNode(this, visitor);

        public override void WriteNode(CodeTarget target, CodeRenderingContext context)
        {
            context.CodeWriter.Write("[");
            context.CodeWriter.Write(ComponentsApi.RouteAttribute.FullTypeName);
            context.CodeWriter.Write("(\"");
            context.CodeWriter.Write(Template);
            context.CodeWriter.Write("\")");
            context.CodeWriter.Write("]");
            context.CodeWriter.WriteLine();
        }
    }
}
