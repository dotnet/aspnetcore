// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal static class DevJwtsDefaults
{
    public static string Scheme => "Bearer";
    public static string Issuer => "dotnet-user-jwts";

    public static string SigningKeyConfigurationKey => "SigningKeys";

    public static int SigningKeyLength => 32;
}
