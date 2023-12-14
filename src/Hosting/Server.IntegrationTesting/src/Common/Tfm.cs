// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.IntegrationTesting;

public static class Tfm
{
    public const string Net462 = "net462";
    public const string NetCoreApp20 = "netcoreapp2.0";
    public const string NetCoreApp21 = "netcoreapp2.1";
    public const string NetCoreApp22 = "netcoreapp2.2";
    public const string NetCoreApp30 = "netcoreapp3.0";
    public const string NetCoreApp31 = "netcoreapp3.1";
    public const string Net50 = "net5.0";
    public const string Net60 = "net6.0";
    public const string Net70 = "net7.0";
    public const string Net80 = "net8.0";
    public const string Net90 = "net9.0";
    public const string Default = Net90;

    public static bool Matches(string tfm1, string tfm2)
    {
        return string.Equals(tfm1, tfm2, StringComparison.OrdinalIgnoreCase);
    }
}
