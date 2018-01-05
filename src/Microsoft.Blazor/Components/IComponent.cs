// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.UITree;

namespace Microsoft.Blazor.Components
{
    /// <summary>
    /// Represents a UI component.
    /// </summary>
    public interface IComponent
    {
        /// <summary>
        /// Builds a <see cref="UITree"/> representing the current state of the component.
        /// </summary>
        /// <param name="builder">A <see cref="UITreeBuilder"/> to which the rendered nodes should be appended.</param>
        void BuildUITree(UITreeBuilder builder);
    }
}
