// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Forms;

namespace BasicTestApp.FormsTest;

public class DerivedInputNumberComponent : InputNumber<int?>
{
    // Repro for https://github.com/dotnet/aspnetcore/issues/40097

    protected override bool TryParseValueFromString(string value, out int? result, out string validationErrorMessage)
    {
        if (value == "1")
        {
            result = 0;
            validationErrorMessage = null;
            return true;
        }
        return base.TryParseValueFromString(value, out result, out validationErrorMessage);
    }
}
