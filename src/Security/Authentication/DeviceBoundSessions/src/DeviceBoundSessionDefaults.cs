// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Default values for the Device Bound Session Credentials authentication scheme.
/// </summary>
[Experimental("ASP0030", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public static class DeviceBoundSessionDefaults
{
    /// <summary>
    /// The default authentication scheme name.
    /// </summary>
    public const string AuthenticationScheme = "DeviceBoundSession";

    /// <summary>
    /// The default registration path.
    /// </summary>
    public const string RegistrationPath = "/.well-known/dbsc/registration";

    /// <summary>
    /// The default refresh path.
    /// </summary>
    public const string RefreshPath = "/.well-known/dbsc/refresh";
}
