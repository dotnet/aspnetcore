// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms;

/// <summary>
/// An input component used for selecting a value from a group of choices.
/// </summary>
public class InputRadio<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TValue> : ComponentBase
{
    bool _trueValueToggle;

    /// <summary>
    /// Gets context for this <see cref="InputRadio{TValue}"/>.
    /// </summary>
    internal InputRadioContext? Context { get; private set; }

    /// <summary>
    /// Gets or sets a collection of additional attributes that will be applied to the input element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)] public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// Gets or sets the value of this input.
    /// </summary>
    [Parameter]
    public TValue? Value { get; set; }

    /// <summary>
    /// Gets or sets the name of the parent input radio group.
    /// </summary>
    [Parameter] public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the associated <see cref="ElementReference"/>.
    /// <para>
    /// May be <see langword="null"/> if accessed before the component is rendered.
    /// </para>
    /// </summary>
    [DisallowNull] public ElementReference? Element { get; protected set; }

    [CascadingParameter] private InputRadioContext? CascadedContext { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        Context = string.IsNullOrEmpty(Name) ? CascadedContext : CascadedContext?.FindContextInAncestors(Name);

        if (Context == null)
        {
            throw new InvalidOperationException($"{GetType()} must have an ancestor {typeof(InputRadioGroup<TValue>)} " +
                $"with a matching 'Name' property, if specified.");
        }
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        Debug.Assert(Context != null);

        builder.OpenElement(0, "input");
        builder.AddMultipleAttributes(1, AdditionalAttributes);
        builder.AddAttributeIfNotNullOrEmpty(2, "class", AttributeUtilities.CombineClassNames(AdditionalAttributes, Context.FieldClass));
        builder.AddAttribute(3, "type", "radio");
        builder.AddAttribute(4, "name", Context.GroupName);
        builder.AddAttribute(5, "value", BindConverter.FormatValue(Value?.ToString()));
        builder.AddAttribute(6, "checked", Context.CurrentValue?.Equals(Value) == true ? GetToggledTrueValue() : null);
        builder.AddAttribute(7, "onchange", Context.ChangeEventCallback);
        builder.SetUpdatesAttributeName("checked");
        builder.AddElementReferenceCapture(8, __inputReference => Element = __inputReference);
        builder.CloseElement();
    }

    // This is an unfortunate hack, but is needed for the scenario described by test InputRadioGroupWorksWithMutatingSetter.
    // Radio groups are special in that modifying one <input type=radio> instantly and implicitly also modifies the previously
    // selected one in the same group. As such, our SetUpdatesAttributeName mechanism isn't sufficient to stay in sync with the
    // DOM, because the 'change' event will fire on the new <input type=radio> you just selected, not the previously-selected
    // one, and so the previously-selected one doesn't get notified to update its state in the old rendertree. So, if the setter
    // reverts the incoming value, the previously-selected one would produce an empty diff (because its .NET value hasn't changed)
    // and hence it would be left unselected in the DOM. If you don't understand why this is a problem, try commenting out the
    // line that toggles _trueValueToggle and see the E2E test fail.
    //
    // This hack works around that by causing InputRadio *always* to force its own 'checked' state to be true in the DOM if it's
    // true in .NET, whether or not it was true before, by continally changing the value that represents 'true'. This doesn't
    // really cause any significant increase in traffic because if we're rendering this InputRadio at all, sending one more small
    // attribute value is inconsequential.
    //
    // Ultimately, a better solution would be to make SetUpdatesAttributeName smarter still so that it knows about the special
    // semantics of radio buttons so that, when one <input type="radio"> changes, it treats any previously-selected sibling
    // as needing DOM sync as well. That's a more sophisticated change and might not even be useful if the radio buttons
    // aren't truly siblings and are in different DOM subtrees (and especially if they were rendered by different components!)
    private string GetToggledTrueValue()
    {
        _trueValueToggle = !_trueValueToggle;
        return _trueValueToggle ? "a" : "b";
    }
}
