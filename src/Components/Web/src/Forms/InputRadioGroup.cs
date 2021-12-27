// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Groups child <see cref="InputRadio{TValue}"/> components.
/// </summary>
public class InputRadioGroup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : InputBase<TValue>, IInputRadioContext
{
    private readonly string _defaultGroupName = Guid.NewGuid().ToString("N");
    private readonly IList<Action> _renderTriggers = new List<Action>();
    private EventCallback<ChangeEventArgs> _changeCallback;
    private string _groupName = string.Empty;
    private string _fieldClass = string.Empty;

    /// <summary>
    /// Gets or sets the child content to be rendering inside the <see cref="InputRadioGroup{TValue}"/>.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the name of the group.
    /// </summary>
    [Parameter] public string? Name { get; set; }

    [CascadingParameter] private IInputRadioContext? CascadedContext { get; set; }
    string IInputRadioContext.GroupName
        => _groupName;

    object? IInputRadioContext.CurrentValue
        => CurrentValue;

    string IInputRadioContext.FieldClass
        => _fieldClass;

    EventCallback<ChangeEventArgs> IInputRadioContext.ChangeEventCallback
        => _changeCallback;

    void IInputRadioContext.Add(Action renderAction)
    {
        if (!_renderTriggers.Contains(renderAction))
        {
            _renderTriggers.Add(renderAction);
        }
    }

    void IInputRadioContext.Remove(Action renderAction)
        => _renderTriggers.Remove(renderAction);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        _groupName = !string.IsNullOrEmpty(Name) ? Name : _defaultGroupName;
        _fieldClass = EditContext?.FieldCssClass(FieldIdentifier) ?? string.Empty;
        _changeCallback = EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString);
        foreach (var renderTrigger in _renderTriggers)
        {
            renderTrigger();
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<IInputRadioContext>>(0);
        builder.AddAttribute(1, "IsFixed", true);
        builder.AddAttribute(2, "Value", this);
        builder.AddAttribute(3, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        => this.TryParseSelectableValueFromString(value, out result, out validationErrorMessage);

    IInputRadioContext? IInputRadioContext.FindContextInAncestors(string groupName)
        => string.Equals(_groupName, groupName) ? this : CascadedContext?.FindContextInAncestors(groupName);
}
