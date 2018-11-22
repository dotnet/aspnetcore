// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Represents a segment of UI content, implemented as a delegate that
    /// writes the content to a <see cref="RenderTreeBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RenderTreeBuilder"/> to which the content should be written.</param>
    public delegate void RenderFragment(RenderTreeBuilder builder);

    /// <summary>
    /// Represents a segment of UI content for an object of type <typeparamref name="T"/>, implemented as
    /// a function that returns a <see cref="RenderFragment"/>.
    /// </summary>
    /// <typeparam name="T">The type of object.</typeparam>
    /// <param name="value">The value used to build the content.</param>
    public delegate RenderFragment RenderFragment<T>(T value);
}
