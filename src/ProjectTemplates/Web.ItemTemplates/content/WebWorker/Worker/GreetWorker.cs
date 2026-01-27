// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace WebWorkerTemplate.Worker;

[SupportedOSPlatform("browser")]
public static partial class GreetWorker
{
    [JSExport]
    public static string Greet(string name) => $"Hello {name}!";
}

