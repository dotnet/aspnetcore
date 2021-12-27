// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Describes context for an <see cref="InputRadio{TValue}"/> component.
/// </summary>
internal interface IInputRadioContext
{
    /// <summary>
    /// Gets the name of the input radio group.
    /// </summary>
    string GroupName { get; }

    /// <summary>
    /// Gets the current selected value in the input radio group.
    /// </summary>
    object? CurrentValue { get; }

    /// <summary>
    /// Gets a css class indicating the validation state of input radio elements.
    /// </summary>
    string FieldClass { get; }

    /// <summary>
    /// Gets the event callback to be invoked when the selected value is changed.
    /// </summary>
    EventCallback<ChangeEventArgs> ChangeEventCallback { get; }

    /// <summary>
    /// Finds an <see cref="IInputRadioContext"/> in the context's ancestors with the matching <paramref name="groupName"/>.
    /// </summary>
    /// <param name="groupName">The group name of the ancestor <see cref="IInputRadioContext"/>.</param>
    /// <returns>The <see cref="IInputRadioContext"/>, or <c>null</c> if none was found.</returns>
    IInputRadioContext? FindContextInAncestors(string groupName);

    /// <summary>
    /// called by a <see cref="InputRadio{TValue}" /> to add itself to a list that gets called when the
    /// corresponding <see cref="InputRadioGroup{TValue}" /> needs to rerender it's children.
    /// </summary>
    /// <param name="renderAction"></param>
    void Add(Action renderAction);

    /// <summary>
    /// removes renderaction from list
    /// </summary>
    /// <param name="renderAction"></param>
    void Remove(Action renderAction);
}
