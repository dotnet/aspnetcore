// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using MockHostTypes;

namespace BuildWebHostInvalidSignature;

public class Program
{
    static void Main(string[] args)
    {
    }

    // Missing string[] args
    public static IWebHost BuildWebHost() => null;
}
