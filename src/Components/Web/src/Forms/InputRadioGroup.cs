// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// Groups child <see cref="InputRadio{TValue}"/> components.
    /// </summary>
    public class InputRadioGroup : ComponentBase
    {
        internal string? GroupName { get; private set; }

        /// <summary>
        /// Gets or sets the child content to be rendering inside the <see cref="InputRadioGroup"/>.
        /// </summary>
        [Parameter] public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the name of the group.
        /// </summary>
        [Parameter] public string? Name { get; set; }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            GroupName ??= !string.IsNullOrEmpty(Name) ? Name : Guid.NewGuid().ToString("N");
        }

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Debug.Assert(GroupName != null);

            builder.OpenComponent<CascadingValue<InputRadioGroup>>(0);
            builder.AddAttribute(1, "IsFixed", true);
            builder.AddAttribute(2, "Value", this);
            builder.AddAttribute(3, "ChildContent", ChildContent);
            builder.CloseComponent();
        }
    }
}
