// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components.HotReload;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Displays the display name for a specified field, reading from <see cref="DisplayAttribute"/>
/// or <see cref="DisplayNameAttribute"/> if present, or falling back to the property name.
/// </summary>
/// <typeparam name="TValue">The type of the field.</typeparam>
public class DisplayName<TValue> : IComponent
{
    private static readonly ConcurrentDictionary<MemberInfo, string> _displayNameCache = new();

    private RenderHandle _renderHandle;
    private string? _displayName;

    /// <summary>
    /// Specifies the field for which the display name should be shown.
    /// </summary>
    [Parameter, EditorRequired]
    public Expression<Func<TValue>>? For { get; set; }

    static DisplayName()
    {
        if (HotReloadManager.Default.MetadataUpdateSupported)
        {
            HotReloadManager.Default.OnDeltaApplied += ClearCache;
        }
    }

    /// <inheritdoc />
    void IComponent.Attach(RenderHandle renderHandle)
    {
        _renderHandle = renderHandle;
    }

    /// <inheritdoc />
    Task IComponent.SetParametersAsync(ParameterView parameters)
    {
        parameters.SetParameterProperties(this);

        if (For is null)
        {
            throw new InvalidOperationException($"{GetType()} requires a value for the " +
                $"{nameof(For)} parameter.");
        }

        var newDisplayName = GetDisplayName(For);

        // Only re-render if the display name string has actually changed
        if (newDisplayName != _displayName)
        {
            _displayName = newDisplayName;
            _renderHandle.Render(BuildRenderTree);
        }

        return Task.CompletedTask;
    }

    private void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.AddContent(0, _displayName);
    }

    private static string GetDisplayName(Expression<Func<TValue>> expression)
    {
        if (expression.Body is not MemberExpression memberExpression)
        {
            throw new ArgumentException(
                $"The provided expression contains a {expression.Body.GetType().Name} which is not supported. " +
                $"{nameof(DisplayName<TValue>)} only supports simple member accessors (fields, properties) of an object.",
                nameof(expression));
        }

        var member = memberExpression.Member;

        return _displayNameCache.GetOrAdd(member, static m =>
        {
            var displayAttribute = m.GetCustomAttribute<DisplayAttribute>();
            if (displayAttribute is not null)
            {
                var name = displayAttribute.GetName();
                if (name is not null)
                {
                    return name;
                }
            }

            var displayNameAttribute = m.GetCustomAttribute<DisplayNameAttribute>();
            if (displayNameAttribute?.DisplayName is not null)
            {
                return displayNameAttribute.DisplayName;
            }

            return m.Name;
        });
    }

    private static void ClearCache()
    {
        _displayNameCache.Clear();
    }
}
