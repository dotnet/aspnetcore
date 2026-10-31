// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.SignalR.Tests;

// WriteContext, but with a timestamp...
public class LogRecord
{
    public DateTime Timestamp { get; }
    public WriteContext Write { get; }

    public LogRecord(DateTime timestamp, WriteContext write)
    {
        Timestamp = timestamp;
        Write = write;
    }
}
