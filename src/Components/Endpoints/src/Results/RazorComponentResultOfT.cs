// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Http.HttpResults;

/// <summary>
/// An <see cref="IResult"/> that renders a Razor Component.
/// </summary>
public class RazorComponentResult<[DynamicallyAccessedMembers(Component)] TComponent>
    : RazorComponentResult where TComponent: IComponent
{
    /// <summary>
    /// Constructs an instance of <see cref="RazorComponentResult"/>.
    /// </summary>
    public RazorComponentResult() : base(typeof(TComponent))
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="RazorComponentResult"/>.
    /// </summary>
    /// <param name="parameters">Parameters for the component.</param>
    public RazorComponentResult(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] object parameters) : base(typeof(TComponent), parameters)
    {
    }

    /// <summary>
    /// Constructs an instance of <see cref="RazorComponentResult"/>.
    /// </summary>
    /// <param name="parameters">Parameters for the component.</param>
    public RazorComponentResult(IReadOnlyDictionary<string, object?> parameters) : base(typeof(TComponent), parameters)
    {
    }
}
