// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging;

#pragma warning disable CA1852 // Seal internal types
internal class W3CLoggerProcessor : FileLoggerProcessor
#pragma warning restore CA1852 // Seal internal types
{
    private readonly W3CLoggingFields _loggingFields;
    private readonly ISet<string>? _additionalRequestHeaders;

    public W3CLoggerProcessor(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory) : base(options, environment, factory)
    {
        _loggingFields = options.CurrentValue.LoggingFields;
        _additionalRequestHeaders = W3CLoggerOptions.FilterRequestHeaders(options.CurrentValue);
    }

    public override async Task OnFirstWrite(StreamWriter streamWriter, CancellationToken cancellationToken)
    {
        await WriteMessageAsync("#Version: 1.0", streamWriter, cancellationToken);

        await WriteMessageAsync("#Start-Date: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), streamWriter, cancellationToken);

        await WriteMessageAsync(GetFieldsDirective(), streamWriter, cancellationToken);
    }

    private string GetFieldsDirective()
    {
        // 152 is the length of the default fields directive
        var sb = new ValueStringBuilder(152);
        sb.Append("#Fields:");
        if (_loggingFields.HasFlag(W3CLoggingFields.Date))
        {
            sb.Append(" date");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.Time))
        {
            sb.Append(" time");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.ClientIpAddress))
        {
            sb.Append(" c-ip");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.UserName))
        {
            sb.Append(" cs-username");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.ServerName))
        {
            sb.Append(" s-computername");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.ServerIpAddress))
        {
            sb.Append(" s-ip");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.ServerPort))
        {
            sb.Append(" s-port");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.Method))
        {
            sb.Append(" cs-method");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.UriStem))
        {
            sb.Append(" cs-uri-stem");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.UriQuery))
        {
            sb.Append(" cs-uri-query");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.ProtocolStatus))
        {
            sb.Append(" sc-status");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.TimeTaken))
        {
            sb.Append(" time-taken");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.ProtocolVersion))
        {
            sb.Append(" cs-version");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.Host))
        {
            sb.Append(" cs-host");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.UserAgent))
        {
            sb.Append(" cs(User-Agent)");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.Cookie))
        {
            sb.Append(" cs(Cookie)");
        }
        if (_loggingFields.HasFlag(W3CLoggingFields.Referer))
        {
            sb.Append(" cs(Referer)");
        }

        if (_additionalRequestHeaders != null)
        {
            foreach (var header in _additionalRequestHeaders)
            {
                sb.Append($" cs({header})");
            }
        }

        return sb.ToString();
    }

    // For testing
    internal override Task WriteMessageAsync(string message, StreamWriter streamWriter, CancellationToken cancellationToken)
    {
        OnWrite(message);
        return base.WriteMessageAsync(message, streamWriter, cancellationToken);
    }

    // Extensibility point for tests
    internal virtual void OnWrite(string message) { }
}
