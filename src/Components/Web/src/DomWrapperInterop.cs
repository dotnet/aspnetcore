// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components;

internal static class DomWrapperInterop
{
    private const string Prefix = "Blazor._internal.domWrapper.";

    public const string Focus = Prefix + "focus";

    public const string FocusBySelector = Prefix + "focusBySelector";
}
