// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Razor.Evolution.Intermediate
{
    public abstract class IRNodeVisitor 
    {
        public virtual void Visit(IRNode node)
        {
            node.Accept(this);
        }

        public virtual void VisitDefault(IRNode node)
        {
        }

        public virtual void VisitDocument(IRDocument node)
        {
            VisitDefault(node);
        }
    }
}
