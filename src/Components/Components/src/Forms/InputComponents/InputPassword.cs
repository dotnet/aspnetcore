// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Forms
{
    /// <summary>
    /// An input component for editing password strings.
    /// </summary>
    public class InputPassword : InputText
    {
        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "input");
            builder.AddAttribute(1, "id", Id);
            builder.AddAttribute(2, "class", CssClass);
            builder.AddAttribute(3, "value", BindMethods.GetValue(CurrentValue));
            builder.AddAttribute(4, "onchange", BindMethods.SetValueHandler(__value => CurrentValue = __value, CurrentValue));
            builder.AddAttribute(5, "type", "password");
            builder.CloseElement();
        }
    }
}
