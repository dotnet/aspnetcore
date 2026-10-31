// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.JwtBearer;

/// <summary>
/// Default values used by <see cref="JwtBearerHandler"/> for JWT bearer authentication.
/// </summary>
public static class JwtBearerDefaults
{
    /// <summary>
    /// Default value for AuthenticationScheme property in the <see cref="JwtBearerOptions"/>.
    /// </summary>
    public const string AuthenticationScheme = "Bearer";
}
