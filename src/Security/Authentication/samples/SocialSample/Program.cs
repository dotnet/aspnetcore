// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore;

namespace SocialSample;

public static class Program
{
    public static void Main(string[] args)
    {
        var host = WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .Build();

        host.Run();
    }
}
