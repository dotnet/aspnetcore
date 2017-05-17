// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public sealed class TemplateIRNode : ExtensionIRNode
    {
        public override IList<RazorIRNode> Children { get; } = new List<RazorIRNode>();

        public override RazorIRNode Parent { get; set; }

        public override SourceSpan? Source { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<TemplateIRNode>(this, visitor);
        }

        public override void WriteNode(CodeTarget target, CSharpRenderingContext context)
        {
            var extension = target.GetExtension<ITemplateTargetExtension>();
            if (extension == null)
            {
                context.ReportMissingExtension<ITemplateTargetExtension>();
                return;
            }

            extension.WriteTemplate(context, this);
        }
    }
}
