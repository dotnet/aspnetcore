// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.Components;

namespace Microsoft.AspNetCore.Blazor.Layouts
{
    /// <summary>
    /// Optional base class for components that represent a layout.
    /// Alternatively, Blazor components may implement <see cref="IComponent"/> directly
    /// and declare their own parameter named <see cref="BlazorLayoutComponent.Body"/>.
    /// </summary>
    public abstract class BlazorLayoutComponent : BlazorComponent
    {
        internal const string BodyPropertyName = nameof(Body);

        /// <summary>
        /// Gets the content to be rendered inside the layout.
        /// </summary>
        [Parameter]
        protected RenderFragment Body { get; private set; }
    }
}
