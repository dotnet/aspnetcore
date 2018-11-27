// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
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
    }
}
