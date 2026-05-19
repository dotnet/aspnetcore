// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn;

/// <summary>
/// Wrapper around the Autobahn Test Suite's "wstest" app.
/// </summary>
public class Wstest : Executable
{
    private static readonly Lazy<Wstest> _instance = new Lazy<Wstest>(Create);

    public static readonly string DefaultLocation = LocateWstest();

    public static Wstest Default => _instance.Value;

    public Wstest(string path) : base(path) { }

    private static Wstest Create()
    {
        var location = LocateWstest();

        return (location == null || !File.Exists(location)) ? null : new Wstest(location);
    }

    private static string LocateWstest()
    {
        var location = Environment.GetEnvironmentVariable("ASPNETCORE_WSTEST_PATH");
        if (string.IsNullOrEmpty(location))
        {
            location = Locate("wstest");
        }

        return location;
    }
}
