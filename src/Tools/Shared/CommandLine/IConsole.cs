// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.Extensions.Tools.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
public interface IConsole
{
    event ConsoleCancelEventHandler CancelKeyPress;
    TextWriter Out { get; }
    TextWriter Error { get; }
    TextReader In { get; }
    bool IsInputRedirected { get; }
    bool IsOutputRedirected { get; }
    bool IsErrorRedirected { get; }
    ConsoleColor ForegroundColor { get; set; }
    void ResetColor();
}
