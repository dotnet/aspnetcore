// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Displays the display name for a specified field, reading from <see cref="DisplayAttribute"/>
/// or <see cref="DisplayNameAttribute"/> if present, or falling back to the property name.
/// </summary>
/// <typeparam name="TValue">The type of the field.</typeparam>
public class DisplayName<TValue> : ComponentBase
{
  private Expression<Func<TValue>>? _previousFieldAccessor;
    private string? _displayName;

    /// <summary>
    /// Specifies the field for which the display name should be shown.
    /// </summary>
    [Parameter, EditorRequired]
    public Expression<Func<TValue>>? For { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (For is null)
        {
            throw new InvalidOperationException($"{GetType()} requires a value for the " +
                $"{nameof(For)} parameter.");
        }

        if (For != _previousFieldAccessor)
        {
            _displayName = GetDisplayName(For);
            _previousFieldAccessor = For;
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, _displayName);
    }

    private static string GetDisplayName(Expression<Func<TValue>> expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            var member = memberExpression.Member;

            var displayAttribute = member.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute != null)
            {
                var name = displayAttribute.GetName();
                if (name != null)
                {
                    return name;
                }
            }

            var displayNameAttribute = member.GetCustomAttribute<DisplayNameAttribute>();
            if (displayNameAttribute?.DisplayName is not null)
            {
                return displayNameAttribute.DisplayName;
            }

            return member.Name;
        }

        throw new ArgumentException(
            $"The provided expression contains a {expression.Body.GetType().Name} which is not supported. " +
            $"{nameof(DisplayName<TValue>)} only supports simple member accessors (fields, properties) of an object.",
            nameof(expression));
    }
}
