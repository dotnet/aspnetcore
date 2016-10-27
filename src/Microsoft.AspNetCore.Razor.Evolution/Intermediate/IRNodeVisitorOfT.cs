// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public abstract class IRNodeVisitor<TResult>
    {
        public virtual TResult Visit(IRNode node)
        {
            return node.Accept(this);
        }

        public virtual TResult VisitDefault(IRNode node)
        {
            return default(TResult);
        }

        public virtual TResult VisitDocument(IRDocument node)
        {
            return VisitDefault(node);
        }
    }
}
