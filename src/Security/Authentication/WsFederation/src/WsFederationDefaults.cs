// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.WsFederation;

/// <summary>
/// Default values related to WsFederation authentication handler
/// </summary>
public static class WsFederationDefaults
{
    /// <summary>
    /// The default authentication type used when registering the WsFederationHandler.
    /// </summary>
    public const string AuthenticationScheme = "WsFederation";

    /// <summary>
    /// The default display name used when registering the WsFederationHandler.
    /// </summary>
    public const string DisplayName = "WsFederation";

    /// <summary>
    /// Constant used to identify userstate inside AuthenticationProperties that have been serialized in the 'wctx' parameter.
    /// </summary>
    public static readonly string UserstatePropertiesKey = "WsFederation.Userstate";
}
