// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Components.Gateway;

if (args is ["--info"])
{
    Console.WriteLine($"""
        Runtime: {RuntimeInformation.FrameworkDescription}
        RID: {RuntimeInformation.RuntimeIdentifier}
        Dynamic code supported: {RuntimeFeature.IsDynamicCodeSupported}
        """);

    return;
}

BlazorGateway.BuildWebHost(args).Run();
