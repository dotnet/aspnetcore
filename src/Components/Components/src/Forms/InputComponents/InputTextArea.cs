// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.RenderTree;

namespace Microsoft.AspNetCore.Components.Forms
{
    // TODO: Support rows/cols/etc

    /* This is almost equivalent to a .razor file containing:
     *
     *    @inherits InputBase<string>
     *    <textarea bind="@CurrentValue" id="@Id" class="@CssClass"></textarea>
     *
     * The only reason it's not implemented as a .razor file is that we don't presently have the ability to compile those
     * files within this project. Developers building their own input components should use Razor syntax.
     */

    /// <summary>
    /// A multiline input component for editing <see cref="string"/> values.
    /// </summary>
    public class InputTextArea : InputBase<string>
    {
        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "textarea");
            builder.AddAttribute(1, "id", Id);
            builder.AddAttribute(2, "class", CssClass);
            builder.AddAttribute(3, "value", BindMethods.GetValue(CurrentValue));
            builder.AddAttribute(4, "onchange", BindMethods.SetValueHandler(__value => CurrentValue = __value, CurrentValue));
            builder.CloseElement();
        }

        /// <inheritdoc />
        protected override bool TryParseValueFromString(string value, out string result, out string validationErrorMessage)
        {
            result = value;
            validationErrorMessage = null;
            return true;
        }
    }
}
