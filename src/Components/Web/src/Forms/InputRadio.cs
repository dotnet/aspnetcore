// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// An input component used for selecting a value from a group of choices.
    /// </summary>
    public class InputRadio<TValue> : InputChoice<TValue>
    {
        /// <summary>
        /// Gets or sets the value that will be bound when this radio input is selected.
        /// </summary>
        [AllowNull]
        [MaybeNull]
        [Parameter]
        public TValue SelectedValue { get; set; } = default;

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "type", "radio");
            builder.AddAttribute(3, "class", CssClass);
            builder.AddAttribute(4, "value", BindConverter.FormatValue(FormatValueAsString(SelectedValue)));
            builder.AddAttribute(5, "checked", SelectedValue?.Equals(CurrentValue));
            builder.AddAttribute(6, "onchange", EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString));
            builder.CloseElement();
        }
    }
}
