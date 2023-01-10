// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.JSInterop.WebAssembly;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services;

internal sealed class WebAssemblyConsoleLogger<T> : ILogger<T>, ILogger
{
    private const string _loglevelPadding = ": ";
    private static readonly string _messagePadding = new(' ', GetLogLevelString(LogLevel.Information).Length + _loglevelPadding.Length);
    private static readonly string _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
    private static readonly StringBuilder _logBuilder = new StringBuilder();

    private readonly string _name;
    private readonly WebAssemblyJSRuntime _jsRuntime;

    public WebAssemblyConsoleLogger(IJSRuntime jsRuntime)
        : this(string.Empty, (WebAssemblyJSRuntime)jsRuntime) // Cast for DI
    {
    }

    public WebAssemblyConsoleLogger(string name, WebAssemblyJSRuntime jsRuntime)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return NoOpDisposable.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return logLevel != LogLevel.None;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(formatter);

        var message = formatter(state, exception);

        if (!string.IsNullOrEmpty(message) || exception != null)
        {
            WriteMessage(logLevel, _name, eventId.Id, message, exception);
        }
    }

    private void WriteMessage(LogLevel logLevel, string logName, int eventId, string message, Exception? exception)
    {
        lock (_logBuilder)
        {
            try
            {
                CreateDefaultLogMessage(_logBuilder, logLevel, logName, eventId, message, exception);
                var formattedMessage = _logBuilder.ToString();

                switch (logLevel)
                {
                    case LogLevel.Trace:
                    case LogLevel.Debug:
                        // Although https://console.spec.whatwg.org/#loglevel-severity claims that
                        // "console.debug" and "console.log" are synonyms, that doesn't match the
                        // behavior of browsers in the real world. Chromium only displays "debug"
                        // messages if you enable "Verbose" in the filter dropdown (which is off
                        // by default). As such "console.debug" is the best choice for messages
                        // with a lower severity level than "Information".
                        ConsoleLoggerInterop.ConsoleDebug(formattedMessage);
                        break;
                    case LogLevel.Information:
                        ConsoleLoggerInterop.ConsoleInfo(formattedMessage);
                        break;
                    case LogLevel.Warning:
                        ConsoleLoggerInterop.ConsoleWarn(formattedMessage);
                        break;
                    case LogLevel.Error:
                        ConsoleLoggerInterop.ConsoleError(formattedMessage);
                        break;
                    case LogLevel.Critical:
                        ConsoleLoggerInterop.DotNetCriticalError(formattedMessage);
                        break;
                    default: // invalid enum values
                        Debug.Assert(logLevel != LogLevel.None, "This method is never called with LogLevel.None.");
                        _jsRuntime.InvokeVoid("console.log", formattedMessage);
                        break;
                }
            }
            finally
            {
                _logBuilder.Clear();
            }
        }
    }

    private static void CreateDefaultLogMessage(StringBuilder logBuilder, LogLevel logLevel, string logName, int eventId, string message, Exception? exception)
    {
        logBuilder.Append(GetLogLevelString(logLevel));
        logBuilder.Append(_loglevelPadding);
        logBuilder.Append(logName);
        logBuilder.Append('[');
        logBuilder.Append(eventId);
        logBuilder.Append(']');

        if (!string.IsNullOrEmpty(message))
        {
            // message
            logBuilder.AppendLine();
            logBuilder.Append(_messagePadding);

            var len = logBuilder.Length;
            logBuilder.Append(message);
            logBuilder.Replace(Environment.NewLine, _newLineWithMessagePadding, len, message.Length);
        }

        // Example:
        // System.InvalidOperationException
        //    at Namespace.Class.Function() in File:line X
        if (exception != null)
        {
            // exception message
            logBuilder.AppendLine();
            logBuilder.Append(exception.ToString());
        }
    }

    private static string GetLogLevelString(LogLevel logLevel)
    {
        switch (logLevel)
        {
            case LogLevel.Trace:
                return "trce";
            case LogLevel.Debug:
                return "dbug";
            case LogLevel.Information:
                return "info";
            case LogLevel.Warning:
                return "warn";
            case LogLevel.Error:
                return "fail";
            case LogLevel.Critical:
                return "crit";
            default:
                throw new ArgumentOutOfRangeException(nameof(logLevel));
        }
    }

    private sealed class NoOpDisposable : IDisposable
    {
        public static NoOpDisposable Instance = new NoOpDisposable();

        public void Dispose() { }
    }
}

internal static partial class ConsoleLoggerInterop
{
    [JSImport("globalThis.console.debug")]
    public static partial void ConsoleDebug(string message);
    [JSImport("globalThis.console.info")]
    public static partial void ConsoleInfo(string message);
    [JSImport("globalThis.console.warn")]
    public static partial void ConsoleWarn(string message);
    [JSImport("globalThis.console.error")]
    public static partial void ConsoleError(string message);
    [JSImport("Blazor._internal.dotNetCriticalError", "blazor-internal")]
    public static partial void DotNetCriticalError(string message);
}
