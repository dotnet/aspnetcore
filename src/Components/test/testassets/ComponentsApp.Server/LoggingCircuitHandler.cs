// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Server.Circuits;

namespace ComponentsApp.Server;

internal class LoggingCircuitHandler : CircuitHandler
{
    private readonly ILogger<LoggingCircuitHandler> _logger;
    private static Action<ILogger, string, Exception> _circuitOpened;
    private static Action<ILogger, string, Exception> _connectionUp;
    private static Action<ILogger, string, Exception> _connectionDown;
    private static Action<ILogger, string, Exception> _circuitClosed;
    private static Action<ILogger, string, Exception> _unhandledException;
    private static Action<ILogger, string, Exception> _invokeDotNetFromJS;
    private static Action<ILogger, string, Exception> _endInvokeJSFromDotNet;

    public LoggingCircuitHandler(ILogger<LoggingCircuitHandler> logger)
    {
        _logger = logger;

        _circuitOpened = LoggerMessage.Define<string>(
            logLevel: LogLevel.Information,
            1,
            formatString: "Circuit opened for {circuitId}.");

        _connectionUp = LoggerMessage.Define<string>(
            logLevel: LogLevel.Information,
            2,
            formatString: "Connection up for {circuitId}.");

        _connectionDown = LoggerMessage.Define<string>(
            logLevel: LogLevel.Information,
            3,
            formatString: "Connection down for {circuitId}.");

        _circuitClosed = LoggerMessage.Define<string>(
            logLevel: LogLevel.Information,
            4,
            formatString: "Circuit closed for {circuitId}.");

        _unhandledException = LoggerMessage.Define<string>(
            logLevel: LogLevel.Error,
            5,
            formatString: "Unhandled exception for {circuitId}.");

        _invokeDotNetFromJS = LoggerMessage.Define<string>(
            logLevel: LogLevel.Information,
            6,
            formatString: "JS to .NET invocation for {circuitId}.");

        _endInvokeJSFromDotNet = LoggerMessage.Define<string>(
            logLevel: LogLevel.Information,
            7,
            formatString: "End .NET to JS invocation for {circuitId}.");
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken cts)
    {
        _circuitOpened(_logger, circuit.Id, null);
        return base.OnCircuitOpenedAsync(circuit, cts);
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cts)
    {
        _connectionUp(_logger, circuit.Id, null);
        return base.OnConnectionUpAsync(circuit, cts);
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cts)
    {
        _connectionDown(_logger, circuit.Id, null);
        return base.OnConnectionDownAsync(circuit, cts);
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken cts)
    {
        _circuitClosed(_logger, circuit.Id, null);
        return base.OnCircuitClosedAsync(circuit, cts);
    }

    public override Task OnUnhandledExceptionAsync(Circuit circuit, Exception exception, CancellationToken cts)
    {
        _unhandledException(_logger, circuit.Id, exception);
        return base.OnUnhandledExceptionAsync(circuit, exception, cts);
    }

    public override Task OnInvokeDotNetFromJSAsync(Circuit circuit, CancellationToken cts)
    {
        _invokeDotNetFromJS(_logger, circuit.Id, null);
        return base.OnInvokeDotNetFromJSAsync(circuit, cts);
    }

    public override Task OnEndInvokeJSFromDotNetAsync(Circuit circuit, CancellationToken cts)
    {
        _endInvokeJSFromDotNet(_logger, circuit.Id, null);
        return base.OnEndInvokeJSFromDotNetAsync(circuit, cts);
    }
}
