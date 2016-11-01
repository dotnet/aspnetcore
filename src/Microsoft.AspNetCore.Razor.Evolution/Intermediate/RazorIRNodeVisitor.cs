// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public abstract class RazorIRNodeVisitor 
    {
        public virtual void Visit(RazorIRNode node)
        {
            node.Accept(this);
        }

        public virtual void VisitDefault(RazorIRNode node)
        {
        }

        public virtual void VisitDocument(RazorIRDocument node)
        {
            VisitDefault(node);
        }
    }
}
