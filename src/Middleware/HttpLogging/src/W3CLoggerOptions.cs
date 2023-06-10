// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HttpLogging;

/// <summary>
/// Options for the <see cref="W3CLogger"/>.
/// </summary>
public sealed class W3CLoggerOptions
{
    private int? _fileSizeLimit = 10 * 1024 * 1024;
    private int? _retainedFileCountLimit = 4;
    private string _fileName = "w3clog-";
    private string _logDirectory = "";
    private TimeSpan _flushInterval = TimeSpan.FromSeconds(1);
    // Update the MaxFilesReached log message in FileLoggerProcessor if this value changes.
    internal const int MaxFileCount = 10000;

    /// <summary>
    /// Gets or sets a strictly positive value representing the maximum log size in bytes or null for no limit.
    /// Once the log is full, no more messages will be appended.
    /// Defaults to <c>10MiB</c>.
    /// </summary>
    public int? FileSizeLimit
    {
        get { return _fileSizeLimit; }
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(FileSizeLimit)} must be positive.");
            }
            _fileSizeLimit = value;
        }
    }

    /// <summary>
    /// Gets or sets a strictly positive value representing the maximum retained file count.
    /// Defaults to <c>4</c>.
    /// Must be between 1 and 10,000, inclusive.
    /// </summary>
    public int? RetainedFileCountLimit
    {
        get { return _retainedFileCountLimit; }
        set
        {
            if (value <= 0 || value > MaxFileCount)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(RetainedFileCountLimit)} must be between 1 and 10,000 (inclusive)");
            }
            _retainedFileCountLimit = value;
        }
    }

    /// <summary>
    /// Gets or sets a string representing the prefix of the file name used to store the logging information.
    /// The current date plus a file number (in the format {YYYYMMDD.X} will be appended to the given value.
    /// Defaults to <c>w3clog-</c>.
    /// </summary>
    public string FileName
    {
        get { return _fileName; }
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            _fileName = value;
        }
    }

    /// <summary>
    /// Gets or sets a string representing the directory where the log file will be written to.
    /// Defaults to <c>./logs/</c> relative to the app directory (ContentRoot).
    /// If a full path is given, that full path will be used. If a relative path is given,
    /// the full path will be that path relative to ContentRoot.
    /// </summary>
    public string LogDirectory
    {
        get { return _logDirectory; }
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            _logDirectory = value;
        }
    }

    /// <summary>
    /// Gets or sets the period after which logs will be flushed to the store.
    /// Defaults to 1 second.
    /// </summary>
    public TimeSpan FlushInterval
    {
        get { return _flushInterval; }
        set
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(value), $"{nameof(FlushInterval)} must be positive.");
            }
            _flushInterval = value;
        }
    }

    /// <summary>
    /// List of additional request header values to log.
    /// <para>
    /// Request headers can contain authentication tokens,
    /// or private information which may have regulatory concerns
    /// under GDPR and other laws. Arbitrary request headers
    /// should not be logged unless logs are secure and
    /// access controlled and the privacy impact assessed.
    /// </para>
    /// </summary>
    public ISet<string> AdditionalRequestHeaders { get; } = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Fields to log. Defaults to logging request and response properties and headers,
    /// plus date/time info and server name.
    /// </summary>
    public W3CLoggingFields LoggingFields { get; set; } = W3CLoggingFields.Date | W3CLoggingFields.Time |
        W3CLoggingFields.ServerName | W3CLoggingFields.Method | W3CLoggingFields.UriStem | W3CLoggingFields.UriQuery |
        W3CLoggingFields.ProtocolStatus | W3CLoggingFields.TimeTaken | W3CLoggingFields.ProtocolVersion |
        W3CLoggingFields.Host | W3CLoggingFields.UserAgent | W3CLoggingFields.Referer | W3CLoggingFields.ConnectionInfoFields;

    internal static ISet<string> FilterRequestHeaders(W3CLoggerOptions options)
    {
        var clonedSet = new SortedSet<string>(options.AdditionalRequestHeaders, StringComparer.InvariantCultureIgnoreCase);

        if (options.LoggingFields.HasFlag(W3CLoggingFields.Host))
        {
            clonedSet.Remove(HeaderNames.Host);
        }
        if (options.LoggingFields.HasFlag(W3CLoggingFields.Referer))
        {
            clonedSet.Remove(HeaderNames.Referer);
        }
        if (options.LoggingFields.HasFlag(W3CLoggingFields.UserAgent))
        {
            clonedSet.Remove(HeaderNames.UserAgent);
        }
        if (options.LoggingFields.HasFlag(W3CLoggingFields.Cookie))
        {
            clonedSet.Remove(HeaderNames.Cookie);
        }
        return clonedSet;
    }
}
