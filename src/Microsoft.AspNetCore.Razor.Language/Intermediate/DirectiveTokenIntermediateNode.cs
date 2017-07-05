// Copyright(c) .NET Foundation.All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class DirectiveTokenIntermediateNode : IntermediateNode
    {
        public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

        public string Content { get; set; }

        public DirectiveTokenDescriptor Descriptor { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            visitor.VisitDirectiveToken(this);
        }
    }
}