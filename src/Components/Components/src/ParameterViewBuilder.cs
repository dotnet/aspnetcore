// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components;

/// <summary>
/// Provides a mechanism to build a <see cref="ParameterView" /> with an unbound lifetime.
/// </summary>
internal readonly struct ParameterViewBuilder
{
    private const string GeneratedParameterViewElementName = "__ARTIFICIAL_PARAMETER_VIEW";
    private readonly RenderTreeFrame[] _frames;

    /// <summary>
    /// Constructs an instance of <see cref="ParameterViewBuilder" />.
    /// </summary>
    /// <param name="maxCapacity">The maximum number of parameters that can be held.</param>
    public ParameterViewBuilder(int maxCapacity)
    {
        _frames = new RenderTreeFrame[maxCapacity + 1];
        _frames[0] = RenderTreeFrame
            .Element(0, GeneratedParameterViewElementName)
            .WithElementSubtreeLength(1);
    }

    /// <summary>
    /// Adds a parameter.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    public void Add(string name, object? value)
    {
        var nextIndex = _frames[0].ElementSubtreeLengthField++;
        _frames[nextIndex] = RenderTreeFrame.Attribute(0, name, value);
    }

    /// <summary>
    /// Supplies a completed <see cref="ParameterView" />.
    /// </summary>
    /// <returns>The <see cref="ParameterView" />.</returns>
    public ParameterView ToParameterView()
    {
        return new ParameterView(ParameterViewLifetime.Unbound, _frames, 0);
    }
}
