// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace SampleApp;

public class Program
{
    public static void Main(string[] args)
    {
        var query = new QueryBuilder()
            {
                { "hello", "world" }
            }.ToQueryString();

        var uri = UriHelper.BuildAbsolute("http", new HostString("contoso.com"), query: query);

        Console.WriteLine(uri);
    }
}
