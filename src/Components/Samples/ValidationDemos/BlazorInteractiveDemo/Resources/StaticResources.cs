// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace BlazorInteractiveDemo.Resources;

/// <summary>
/// Static resources used for the ResourceType/ResourceName localization approach.
/// These are compile-time constants — no runtime IStringLocalizer involved.
/// </summary>
public static class StaticResources
{
    public static string PhoneLabel => "Phone Number";
    public static string PhoneRequired => "A phone number is required.";
}
