// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class InjectDirectiveIRNode : ExtensionIRNode
    {
        public string TypeName { get; set; }

        public string MemberName { get; set; }

        public override IList<RazorIRNode> Children { get; } = new RazorIRNode[0];

        public override RazorIRNode Parent { get; set; }

        public override SourceSpan? Source { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<InjectDirectiveIRNode>(this, visitor);
        }

        public override void WriteNode(RuntimeTarget target, CSharpRenderingContext context)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var extension = target.GetExtension<IInjectDirectiveTargetExtension>();
            extension.WriteInjectProperty(context, this);
        }
    }
}
