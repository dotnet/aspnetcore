// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using Microsoft.AspNetCore.Components.Forms;
using Newtonsoft.Json.Linq;

namespace BasicTestApp.FormsTest;
public class DerivedInputDateComponent : InputDate<DateTime?>
{
    // Supports InputsTwoWayBindingComponent test
    // Repro for https://github.com/dotnet/aspnetcore/issues/40097

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out DateTime? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (value == "0001-01-01")
        {
            result = new DateTime(2020, 1, 1);
            validationErrorMessage = null;
            return true;
        }
        return base.TryParseValueFromString(value, out result, out validationErrorMessage);
    }
}
