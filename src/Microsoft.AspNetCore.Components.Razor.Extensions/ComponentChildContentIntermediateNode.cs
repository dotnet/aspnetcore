// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.Shared;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class ComponentChildContentIntermediateNode : ExtensionIntermediateNode
    {
        public string AttributeName => BoundAttribute?.Name ?? ComponentsApi.RenderTreeBuilder.ChildContent;

        public BoundAttributeDescriptor BoundAttribute { get; set; }

        public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

        public bool IsParameterized => BoundAttribute?.IsParameterizedChildContentProperty() ?? false;

        public string ParameterName { get; set; }

        public string TypeName { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<ComponentChildContentIntermediateNode>(this, visitor);
        }

        public override void WriteNode(CodeTarget target, CodeRenderingContext context)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var writer = (BlazorNodeWriter)context.NodeWriter;
            writer.WriteComponentChildContent(context, this);
        }
    }
}
