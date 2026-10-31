// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Routing;

internal class NavigationLockInterop
{
    private const string Prefix = "Blazor._internal.NavigationLock.";

    public const string EnableNavigationPrompt = Prefix + "enableNavigationPrompt";

    public const string DisableNavigationPrompt = Prefix + "disableNavigationPrompt";
}
