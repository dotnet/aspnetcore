// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace BasicTestApp.FormsTest;

public class DerivedInputTextareaComponent : InputTextArea
{
    // Supports InputsTwoWayBindingComponent test
    // Repro for https://github.com/dotnet/aspnetcore/issues/40097

    protected override bool TryParseValueFromString(string value, out string result, out string validationErrorMessage)
    {
        if (value == "24h")
        {
            result = "24:00:00";
        }
        else
        {
            result = value;
        }
        validationErrorMessage = null;
        return true;
    }
}
