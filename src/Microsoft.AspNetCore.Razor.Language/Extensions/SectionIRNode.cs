// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public sealed class SectionIRNode : ExtensionIRNode
    {
        public override RazorIRNodeCollection Children { get; } = new DefaultIRNodeCollection();

        public string Name { get; set; }

        public override SourceSpan? Source { get; set; }

        public override void Accept(RazorIRNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<SectionIRNode>(this, visitor);
        }

        public override void WriteNode(CodeTarget target, CSharpRenderingContext context)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var extension = target.GetExtension<ISectionTargetExtension>();
            if (extension == null)
            {
                context.ReportMissingExtension<ISectionTargetExtension>();
                return;
            }

            extension.WriteSection(context, this);
        }
    }
}
