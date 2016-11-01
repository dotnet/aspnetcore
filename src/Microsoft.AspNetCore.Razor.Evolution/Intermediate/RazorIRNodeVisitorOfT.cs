// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public abstract class RazorIRNodeVisitor<TResult>
    {
        public virtual TResult Visit(RazorIRNode node)
        {
            return node.Accept(this);
        }

        public virtual TResult VisitDefault(RazorIRNode node)
        {
            return default(TResult);
        }

        public virtual TResult VisitDocument(RazorIRDocument node)
        {
            return VisitDefault(node);
        }
    }
}
