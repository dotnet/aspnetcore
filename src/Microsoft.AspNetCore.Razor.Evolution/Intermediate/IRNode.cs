// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public abstract class IRNode
    {
        internal static readonly IRNode[] EmptyArray = new IRNode[0];

        public abstract IList<IRNode> Children { get; }

        public abstract IRNode Parent { get; set; }

        public abstract void Accept(IRNodeVisitor visitor);

        public abstract TResult Accept<TResult>(IRNodeVisitor<TResult> visitor);
    }
}
