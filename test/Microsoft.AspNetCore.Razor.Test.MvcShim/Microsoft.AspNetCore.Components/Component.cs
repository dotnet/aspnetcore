// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components
{
    public abstract class Component : IComponent
    {
        public virtual void BuildRenderTree(RenderTreeBuilder builder)
        {
        }

        protected void WriteLiteral(string literal) { }
    }
}
