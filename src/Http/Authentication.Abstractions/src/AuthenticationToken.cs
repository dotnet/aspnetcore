// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Name/Value representing a token.
/// </summary>
public class AuthenticationToken
{
    /// <summary>
    /// Name.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Value.
    /// </summary>
    public string Value { get; set; } = default!;
}
