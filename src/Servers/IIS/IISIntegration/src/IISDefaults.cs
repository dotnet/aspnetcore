// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IISIntegration;

/// <summary>
/// String constants used to configure IIS Out-Of-Process.
/// </summary>
public class IISDefaults
{
    /// <summary>
    /// Default authentication scheme, which is "Windows".
    /// </summary>
    public const string AuthenticationScheme = "Windows";
    /// <summary>
    /// Default negotiate string, which is "Negotiate".
    /// </summary>
    public const string Negotiate = "Negotiate";
    /// <summary>
    /// Default NTLM string, which is "NTLM".
    /// </summary>
    public const string Ntlm = "NTLM";
}
