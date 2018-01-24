// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Components
{
    /// <summary>
    /// Represents a UI component.
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// Builds a <see cref="RenderTree"/> representing the current state of the component.
        /// </summary>
        /// <param name="builder">A <see cref="RenderTreeBuilder"/> to which the rendered nodes should be appended.</param>
        void BuildRenderTree(RenderTreeBuilder builder);
    }
}
