// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public abstract class RazorIRNode
    {
        internal static readonly RazorIRNode[] EmptyArray = new RazorIRNode[0];

        public abstract IList<RazorIRNode> Children { get; }

        public abstract RazorIRNode Parent { get; set; }

        public abstract void Accept(RazorIRNodeVisitor visitor);

        public abstract TResult Accept<TResult>(RazorIRNodeVisitor<TResult> visitor);
    }
}
