// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http;

public class CookieParseBenchmarks
{
    private readonly StringValues _single = ".AspNetCore.Cookies=CfDJ8BAklVa9EYREk8_ipRUUYJYhRsleKr485k18s_q5XD6vcRJ-DtowUuLCwwMiY728zRZ3rVFY3DEcXDAQUOTtg1e4tkSIVmYLX38Q6mqdFFyw";
    private readonly StringValues _several = "_ga=GA1.2.1234567890.1234567890; _gid=GA1.2.987654321.987654321; .AspNetCore.Antiforgery.abc=CfDJ8BAklVa9EYREk8_ipRUUYJ; ai_user=abc|2024-01-01T00:00:00.000Z; theme=dark";
    private readonly StringValues _escaped = "name=John%20Doe; city=New%20York; tag=a%2Cb";

    [Benchmark]
    public object Single() => RequestCookieCollection.Parse(_single);

    [Benchmark]
    public object Several() => RequestCookieCollection.Parse(_several);

    [Benchmark]
    public object Escaped() => RequestCookieCollection.Parse(_escaped);
}
