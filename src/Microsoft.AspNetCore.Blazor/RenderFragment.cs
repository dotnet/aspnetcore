// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.RenderTree;

namespace Microsoft.AspNetCore.Blazor
{
    /// <summary>
    /// Represents a segment of UI content, implemented as a delegate that
    /// writes the content to a <see cref="RenderTreeBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RenderTreeBuilder"/> to which the content should be written.</param>
    public delegate void RenderFragment(RenderTreeBuilder builder);

    /// <summary>
    /// Represents a segment of UI content for an object of type <typeparamref name="T"/>, implemented
    /// as a delegate that writes the content to a <see cref="RenderTreeBuilder"/>.
    /// </summary>
    /// <typeparam name="T">The type of object.</typeparam>
    /// <param name="builder">The <see cref="RenderTreeBuilder"/> to which the content should be written.</param>
    /// <param name="value">The value used to build the content.</param>
    public delegate void RenderFragment<T>(RenderTreeBuilder builder, T value);
}
