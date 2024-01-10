// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Describes context for an <see cref="InputRadio{TValue}"/> component.
/// </summary>
internal sealed class InputRadioContext
{
    private readonly IInputRadioValueProvider _valueProvider;

    public InputRadioContext? ParentContext { get; }
    public EventCallback<ChangeEventArgs> ChangeEventCallback { get; }
    public object? CurrentValue => _valueProvider.CurrentValue;

    // Mutable properties that may change any time an InputRadioGroup is rendered
    public string? GroupName { get; set; }
    public string? FieldClass { get; set; }

    public InputRadioContext(IInputRadioValueProvider valueProvider, InputRadioContext? parentContext, EventCallback<ChangeEventArgs> changeEventCallback)
    {
        _valueProvider = valueProvider;
        ParentContext = parentContext;
        ChangeEventCallback = changeEventCallback;
    }

    /// <summary>
    /// Finds an <see cref="InputRadioContext"/> in the context's ancestors with the matching <paramref name="groupName"/>.
    /// </summary>
    /// <param name="groupName">The group name of the ancestor <see cref="InputRadioContext"/>.</param>
    /// <returns>The <see cref="InputRadioContext"/>, or <c>null</c> if none was found.</returns>
    public InputRadioContext? FindContextInAncestors(string groupName)
        => string.Equals(GroupName, groupName) ? this : ParentContext?.FindContextInAncestors(groupName);
}
