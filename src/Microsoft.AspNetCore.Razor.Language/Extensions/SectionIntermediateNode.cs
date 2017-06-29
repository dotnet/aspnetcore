// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public sealed class SectionIntermediateNode : ExtensionIntermediateNode
    {
        public override IntermediateNodeCollection Children { get; } = new DefaultIntermediateNodeCollection();

        public string Name { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<SectionIntermediateNode>(this, visitor);
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

            var extension = target.GetExtension<ISectionTargetExtension>();
            if (extension == null)
            {
                ReportMissingCodeTargetExtension<ISectionTargetExtension>(context);
                return;
            }

            extension.WriteSection(context, this);
        }
    }
}
