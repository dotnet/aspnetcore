// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging;

internal class TestW3CLogger : W3CLogger
{
    public TestW3CLogger(IOptionsMonitor<W3CLoggerOptions> options, TestW3CLoggerProcessor processor) : base(options, processor)
    {
        Processor = processor;
    }

    public TestW3CLoggerProcessor Processor { get; set; }
}
