// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HttpLogging;

// Carries everything W3CLoggerProcessor needs to write one log line directly to a StreamWriter,
// without W3CLogger having to format a complete line up front just to get it onto the queue.
// LoggingFields is captured at Log()/enqueue time so the set of fields written reflects the
// options that were in effect when the entry was produced, even if options change before the
// background processor gets to it.
internal readonly struct W3CLogEntry
{
    public W3CLogEntry(string[] elements, string[] additionalHeaders, W3CLoggingFields loggingFields)
    {
        Elements = elements;
        AdditionalHeaders = additionalHeaders;
        LoggingFields = loggingFields;
    }

    public string[] Elements { get; }

    public string[] AdditionalHeaders { get; }

    public W3CLoggingFields LoggingFields { get; }
}
