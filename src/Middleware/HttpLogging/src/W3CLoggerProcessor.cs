// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging;

internal class W3CLoggerProcessor : FileLoggerProcessor
{
    private readonly W3CLoggingFields _loggingFields;

    public W3CLoggerProcessor(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory) : base(options, environment, factory)
    {
        _loggingFields = options.CurrentValue.LoggingFields;
    }

    public override async Task OnFirstWrite(StreamWriter streamWriter, CancellationToken cancellationToken)
    {
        await WriteMessageLineAsync("#Version: 1.0", streamWriter, cancellationToken);

        await WriteMessageLineAsync("#Start-Date: " + DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), streamWriter, cancellationToken);

        await WriteFieldsDirective(streamWriter, cancellationToken);
    }

    private async Task WriteFieldsDirective(StreamWriter streamWriter, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        await WriteMessageAsync("#Fields:", streamWriter, cancellationToken);

        if (_loggingFields.HasFlag(W3CLoggingFields.Date))
        {
            await WriteMessageAsync(" date", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.Time))
        {
            await WriteMessageAsync(" time", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.ClientIpAddress))
        {
            await WriteMessageAsync(" c-ip", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.UserName))
        {
            await WriteMessageAsync(" cs-username", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.ServerName))
        {
            await WriteMessageAsync(" s-computername", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.ServerIpAddress))
        {
            await WriteMessageAsync(" s-ip", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.ServerPort))
        {
            await WriteMessageAsync(" s-port", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.Method))
        {
            await WriteMessageAsync(" cs-method", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.UriStem))
        {
            await WriteMessageAsync(" cs-uri-stem", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.UriQuery))
        {
            await WriteMessageAsync(" cs-uri-query", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.ProtocolStatus))
        {
            await WriteMessageAsync(" sc-status", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.TimeTaken))
        {
            await WriteMessageAsync(" time-taken", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.ProtocolVersion))
        {
            await WriteMessageAsync(" cs-version", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.Host))
        {
            await WriteMessageAsync(" cs-host", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.UserAgent))
        {
            await WriteMessageAsync(" cs(User-Agent)", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.Cookie))
        {
            await WriteMessageAsync(" cs(Cookie)", streamWriter, cancellationToken);
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.Referer))
        {
            await WriteMessageAsync(" cs(Referer)", streamWriter, cancellationToken);
        }

        await EndMessageAndFlushAsync(streamWriter);
    }

    internal override Task WriteMessageLineAsync(string message, StreamWriter streamWriter, CancellationToken cancellationToken)
    {
        OnWriteLine(message);
        return base.WriteMessageLineAsync(message, streamWriter, cancellationToken);
    }

    // For testing
    internal override Task WriteMessageAsync(string message, StreamWriter streamWriter, CancellationToken cancellationToken)
    {
        OnWrite(message);
        return base.WriteMessageAsync(message, streamWriter, cancellationToken);
    }

    internal Task EndMessageAndFlushAsync(StreamWriter streamWriter)
    {
        OnFlush();
        return streamWriter.FlushAsync();
    }

    // Extensibility point for tests
    internal virtual void OnWriteLine(string message) { }
    internal virtual void OnWrite(string message, bool endOfLine = false) { }
    internal virtual void OnFlush() { }
}
