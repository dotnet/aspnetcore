// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

public class RequestCookieCollectionBenchmarks
{
    private StringValues _cookie;

    [IterationSetup]
    public void Setup()
    {
        _cookie = ".AspNetCore.Cookies=CfDJ8BAklVa9EYREk8_ipRUUYJYhRsleKr485k18s_q5XD6vcRJ-DtowUuLCwwMiY728zRZ3rVFY3DEcXDAQUOTtg1e4tkSIVmYLX38Q6mqdFFyw-8dksclDywe9vnN84cEWvfV0wP3EgOsJGHaND7kTJ47gr7Pc1tLHWOm4Pe7Q1vrT9EkcTMr1Wts3aptBl3bdOLLqjmSdgk-OI7qG7uQGz1OGdnSer6-KLUPBcfXblzs4YCjvwu3bGnM42xLGtkZNIF8izPpyqKkIf7ec6O6LEHMp4gcq86PGHCXHn5NKuNSD";
    }

    [Benchmark]
    public void Parse_TypicalCookie()
    {
        _ = RequestCookieCollection.Parse(_cookie);
    }
}
