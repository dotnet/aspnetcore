// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.Tools.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public class ConsoleReporter : IReporter
{
    private readonly object _writeLock = new object();

    public ConsoleReporter(IConsole console)
        : this(console, verbose: false, quiet: false)
    { }

    public ConsoleReporter(IConsole console, bool verbose, bool quiet)
    {
        Ensure.NotNull(console, nameof(console));

        Console = console;
        IsVerbose = verbose;
        IsQuiet = quiet;
    }

    protected IConsole Console { get; }
    public bool IsVerbose { get; set; }
    public bool IsQuiet { get; set; }

    protected virtual void WriteLine(TextWriter writer, string message, ConsoleColor? color)
    {
        lock (_writeLock)
        {
            if (color.HasValue)
            {
                Console.ForegroundColor = color.Value;
            }

            writer.WriteLine(message);

            if (color.HasValue)
            {
                Console.ResetColor();
            }
        }
    }

    public virtual void Error(string message)
        => WriteLine(Console.Error, message, ConsoleColor.Red);
    public virtual void Warn(string message)
        => WriteLine(Console.Out, message, ConsoleColor.Yellow);

    public virtual void Output(string message)
    {
        if (IsQuiet)
        {
            return;
        }
        WriteLine(Console.Out, message, color: null);
    }

    public virtual void Verbose(string message)
    {
        if (!IsVerbose)
        {
            return;
        }

        WriteLine(Console.Out, message, ConsoleColor.DarkGray);
    }
}
