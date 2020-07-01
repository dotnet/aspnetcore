// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Groups child <see cref="InputRadio{TValue}"/> components.
    /// </summary>
    public class InputRadioGroup<TValue> : InputBase<TValue>
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
            var changeEventCallback = EventCallback.Factory.CreateBinder<string?>(this, __value => CurrentValueAsString = __value, CurrentValueAsString);
            var groupName = !string.IsNullOrEmpty(Name) ? Name : _defaultGroupName;

            _context = new InputRadioContext(CascadedContext, groupName, CurrentValue, changeEventCallback);
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Debug.Assert(_context != null);

            builder.OpenElement(0, "div");
            builder.AddMultipleAttributes(1, AdditionalAttributes);
            builder.AddAttribute(2, "class", CssClass);
            builder.OpenComponent<CascadingValue<InputRadioContext>>(3);
            builder.AddAttribute(4, "IsFixed", true);
            builder.AddAttribute(5, "Value", _context);
            builder.AddAttribute(6, "ChildContent", ChildContent);
            builder.CloseComponent();
            builder.CloseElement();
        }

        /// <inheritdoc />
        protected override bool TryParseValueFromString(string? value, [MaybeNull] out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
            => this.TryParseSelectableValueFromString(value, out result, out validationErrorMessage);
    }
}
