// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Forms;

internal static class EditFormInterop
{
    private const string JsFunctionsPrefix = "Blazor._internal.Forms.";

    public const string Submit = JsFunctionsPrefix + "submitForm";
}
