// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.DataProtection.Managed;

/// <summary>
/// <see cref="System.Security.Authentication.HashAlgorithmType"/> is obsolete,
/// so using a custom in-place enumeration here
/// </summary>
internal enum HashAlgorithmType
{
    Other,
    HMACSHA256,
    HMACSHA512
}
