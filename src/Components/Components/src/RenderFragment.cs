// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Represents a segment of UI content, implemented as a delegate that
/// writes the content to a <see cref="RenderTreeBuilder"/>.
/// </summary>
/// <param name="builder">The <see cref="RenderTreeBuilder"/> to which the content should be written.</param>
public delegate void RenderFragment(RenderTreeBuilder builder);

/// <summary>
/// Represents a segment of UI content for an object of type <typeparamref name="TValue"/>, implemented as
/// a function that returns a <see cref="RenderFragment"/>.
/// </summary>
/// <typeparam name="TValue">The type of object.</typeparam>
/// <param name="value">The value used to build the content.</param>
public delegate RenderFragment RenderFragment<TValue>(TValue value);
