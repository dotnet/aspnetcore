// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Optional base class for components that represent a layout.
    /// Alternatively, components may implement <see cref="IComponent"/> directly
    /// and declare their own parameter named <see cref="Body"/>.
    /// </summary>
    public abstract class LayoutComponentBase : ComponentBase
    {
        internal const string BodyPropertyName = nameof(Body);

        /// <summary>
        /// Gets the content to be rendered inside the layout.
        /// </summary>
        [Parameter]
        public RenderFragment Body { get; set; }
    }
}
