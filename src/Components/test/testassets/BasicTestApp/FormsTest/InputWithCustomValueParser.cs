// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;

namespace BasicTestApp.FormsTest;

public class InputWithCustomValueParser : InputText
{
    protected override bool TryParseValueFromString(string value, out string result, out string validationErrorMessage)
    {
        if (value == "INVALID")
        {
            result = default;
            validationErrorMessage = "INVALID is not allowed value.";
            return false;
        }
        else
        {
            result = value;
            validationErrorMessage = null;
            return true;
        }
    }
}
