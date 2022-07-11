// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Displays a display name of provided property.
/// </summary>
public class DisplayName<TValue> : ComponentBase, IDisposable
{
    private string? _displayName;

    /// <summary>
    /// Gets or sets a collection of additional attributes that will be applied to the created <c>label</c> element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Specifies the field for which display name should be displayed.
    /// </summary>
    [Parameter] public Expression<Func<TValue>>? For { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (For == null)
        {
            throw new InvalidOperationException($"{GetType()} requires a value for the " +
                                                $"{nameof(For)} parameter.");
        }

        if (For.Body is not MemberExpression memberExpression)
        {
            throw new InvalidExpressionException($"{GetType()} recieved an invalid expression.");
        }

        if (memberExpression.Member is not PropertyInfo propertyInfo)
        {
            throw new InvalidExpressionException($"{GetType()} received an invalid property.");
        }

        _displayName = propertyInfo.GetCustomAttributes(typeof(DisplayAttribute), true).Cast<DisplayAttribute>()
            .FirstOrDefault()?.Name ?? propertyInfo.Name;
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "label");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddContent(2, _displayName);
        builder.CloseElement();
    }

    /// <summary>
    /// Called to dispose this instance.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> if called within <see cref="IDisposable.Dispose"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    void IDisposable.Dispose()
    {
        Dispose(disposing: true);
    }
}
