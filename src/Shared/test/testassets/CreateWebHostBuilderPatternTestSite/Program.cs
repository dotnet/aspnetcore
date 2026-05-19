// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MockHostTypes;

namespace CreateWebHostBuilderPatternTestSite;

public class Program
{
    public static void Main(string[] args)
    {
        var webHost = CreateWebHostBuilder(args).Build();
    }

    // Do not change the signature of this method. It's used for tests.
    private static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
        new WebHostBuilder();
}
