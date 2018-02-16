// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor.Layouts
{
    /// <summary>
    /// Indicates that the type represents a layout.
    /// </summary>
    public interface ILayoutComponent : IComponent
    {
        /// <summary>
        /// Gets or sets the content to be rendered inside the layout.
        /// </summary>
        RenderFragment Body { get; set; }
    }
}
