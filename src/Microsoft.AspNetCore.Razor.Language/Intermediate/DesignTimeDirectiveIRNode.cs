// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    internal class DesignTimeDirectiveIRNode : ExtensionIRNode
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

            AcceptExtensionNode<DesignTimeDirectiveIRNode>(this, visitor);
        }

        public override void WriteNode(RuntimeTarget target, CSharpRenderingContext context)
        {
            var extension = target.GetExtension<IDesignTimeDirectiveTargetExtension>();
            extension.WriteDesignTimeDirective(context, this);
        }
    }
}
