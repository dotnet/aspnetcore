// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Forms;

namespace BasicTestApp.FormsTest;

public class DerivedInputSelectComponent : InputSelect<DayOfWeek>
{
    // Supports InputsTwoWayBindingComponent test
    // Repro for https://github.com/dotnet/aspnetcore/issues/40097

    protected override bool TryParseValueFromString(string value, [MaybeNullWhen(false)] out DayOfWeek result, [NotNullWhen(false)] out string validationErrorMessage)
    {
        result = DayOfWeek.Monday;
        validationErrorMessage = null;
        return true;
    }
}
