// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor
{
    /// <summary>
    /// Handles an <see cref="UIEventArgs"/> event raised for a <see cref="RenderTreeFrame"/>.
    /// </summary>
    public delegate void UIEventHandler(UIEventArgs e);

    /// <summary>
    /// Handles an <see cref="UIChangeEventArgs"/> event raised for a <see cref="RenderTreeFrame"/>.
    /// </summary>
    public delegate void UIChangeEventHandler(UIChangeEventArgs e);

    /// <summary>
    /// Handles an <see cref="UIKeyboardEventArgs"/> event raised for a <see cref="RenderTreeFrame"/>.
    /// </summary>
    public delegate void UIKeyboardEventHandler(UIKeyboardEventArgs e);

    /// <summary>
    /// Handles an <see cref="UIMouseEventArgs"/> event raised for a <see cref="RenderTreeFrame"/>.
    /// </summary>
    public delegate void UIMouseEventHandler(UIMouseEventArgs e);
}
