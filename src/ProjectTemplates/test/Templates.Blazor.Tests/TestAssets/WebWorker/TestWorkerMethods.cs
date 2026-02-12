// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;

namespace $NAMESPACE$;

/// <summary>
/// Test worker methods for E2E testing of WebWorkerClient.
/// These methods are called from the Web Worker thread.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class TestWorkerMethods
{
    [JSExport]
    public static int Add(int a, int b) => a + b;

    [JSExport]
    public static bool IsEven(int n) => n % 2 == 0;

    [JSExport]
    public static double Divide(double a, double b) => a / b;

    [JSExport]
    public static string Echo(string input) => input;

    [JSExport]
    public static string Concat(string a, string b) => a + b;

    [JSExport]
    public static string GetPersonJson()
        => JsonSerializer.Serialize(new { Name = "Alice", Age = 30 });

    [JSExport]
    public static string ThrowError()
        => throw new InvalidOperationException("Test exception from worker");
}
