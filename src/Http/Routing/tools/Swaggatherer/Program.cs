// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Swaggatherer;

internal static class Program
{
    public static void Main(string[] args)
    {
        var application = new SwaggathererApplication();
        application.Execute(args);
    }
}
