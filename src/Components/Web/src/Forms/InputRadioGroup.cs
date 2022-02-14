// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// Groups child <see cref="InputRadio{TValue}"/> components.
/// </summary>
public class InputRadioGroup<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : InputBase<TValue>
{
    private readonly string _defaultGroupName = Guid.NewGuid().ToString("N");
    private InputRadioContext? _context;

    /// <summary>
    /// Gets or sets the child content to be rendering inside the <see cref="InputRadioGroup{TValue}"/>.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the name of the group.
    /// </summary>
    [Parameter] public string? Name { get; set; }

    [CascadingParameter] private InputRadioContext? CascadedContext { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // On the first render, we can instantiate the InputRadioContext
        if (_context is null)
        {
            var changeEventCallback = EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString);
            _context = new InputRadioContext(CascadedContext, changeEventCallback);
        }
        else if (_context.ParentContext != CascadedContext)
        {
            // This should never be possible in any known usage pattern, but if it happens, we want to know
            throw new InvalidOperationException("An InputRadioGroup cannot change context after creation");
        }

        // Mutate the InputRadioContext instance in place. Since this is a non-fixed cascading parameter, the descendant
        // InputRadio/InputRadioGroup components will get notified to re-render and will see the new values.
        _context.GroupName = !string.IsNullOrEmpty(Name) ? Name : _defaultGroupName;
        _context.CurrentValue = CurrentValue;
        _context.FieldClass = EditContext?.FieldCssClass(FieldIdentifier);
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        Debug.Assert(_context != null);

        // Note that we must not set IsFixed=true on the CascadingValue, because the mutations to _context
        // are what cause the descendant InputRadio components to re-render themselves
        builder.OpenComponent<CascadingValue<InputRadioContext>>(0);
        builder.AddAttribute(2, "Value", _context);
        builder.AddAttribute(3, "ChildContent", ChildContent);
        builder.CloseComponent();
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
        => this.TryParseSelectableValueFromString(value, out result, out validationErrorMessage);
}
