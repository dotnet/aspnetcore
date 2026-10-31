// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.Tools.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public class PhysicalConsole : IConsole
{
    private PhysicalConsole()
    {
        Console.CancelKeyPress += (o, e) =>
        {
            CancelKeyPress?.Invoke(o, e);
        };
    }

    public static IConsole Singleton { get; } = new PhysicalConsole();

    public event ConsoleCancelEventHandler CancelKeyPress;
    public TextWriter Error => Console.Error;
    public TextReader In => Console.In;
    public TextWriter Out => Console.Out;
    public bool IsInputRedirected => Console.IsInputRedirected;
    public bool IsOutputRedirected => Console.IsOutputRedirected;
    public bool IsErrorRedirected => Console.IsErrorRedirected;
    public ConsoleColor ForegroundColor
    {
        get => Console.ForegroundColor;
        set => Console.ForegroundColor = value;
    }

    public void ResetColor() => Console.ResetColor();
}
