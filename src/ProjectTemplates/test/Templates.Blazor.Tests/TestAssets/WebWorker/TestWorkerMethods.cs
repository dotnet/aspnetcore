// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;

// Using global namespace for simpler export access
[SupportedOSPlatform("browser")]
public static partial class TestWorkerMethods
{
    [JSExport]
    public static int Add(int a, int b) => a + b;

    [JSExport]
    public static string Echo(string input) => input;

    [JSExport]
    public static string GetPersonJson()
        => JsonSerializer.Serialize(new { Name = "Alice", Age = 30 });

    [JSExport]
    public static string ThrowError()
        => throw new InvalidOperationException("Test exception from worker");
}
