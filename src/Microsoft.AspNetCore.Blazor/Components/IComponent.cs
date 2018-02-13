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
        /// Initializes the component.
        /// </summary>
        /// <param name="renderHandle">A <see cref="RenderHandle"/> that allows the component to be rendered.</param>
        void Init(RenderHandle renderHandle);

        /// <summary>
        /// Sets parameters supplied by the component's parent in the render tree.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        void SetParameters(ParameterCollection parameters);

        /// <summary>
        /// Builds a <see cref="RenderTree"/> representing the current state of the component.
        /// </summary>
        /// <param name="builder">A <see cref="RenderTreeBuilder"/> to which the rendered frames should be appended.</param>
        void BuildRenderTree(RenderTreeBuilder builder);
    }
}
