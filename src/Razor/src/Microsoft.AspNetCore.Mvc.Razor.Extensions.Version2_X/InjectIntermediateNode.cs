// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X
{
    public class InjectIntermediateNode : ExtensionIntermediateNode
    {
        public string TypeName { get; set; }

        public string MemberName { get; set; }

        public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            AcceptExtensionNode<InjectIntermediateNode>(this, visitor);
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

            var extension = target.GetExtension<IInjectTargetExtension>();
            if (extension == null)
            {
                ReportMissingCodeTargetExtension<IInjectTargetExtension>(context);
                return;
            }

            extension.WriteInjectProperty(context, this);
        }

        public override void FormatNode(IntermediateNodeFormatter formatter)
        {
            formatter.WriteContent(MemberName);

            formatter.WriteProperty(nameof(MemberName), MemberName);
            formatter.WriteProperty(nameof(TypeName), TypeName);
        }
    }
}
