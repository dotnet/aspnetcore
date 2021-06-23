// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal class TestW3CLogger : W3CLogger
    {
        public TestW3CLoggerProcessor Processor;

        public TestW3CLogger(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory) : base(options, environment, factory) { }

        public Task WaitForWrites(int numWrites)
        {
            lock (Processor)
            {
                Processor.ExpectedWrites = numWrites;
                Processor.Tcs = new TaskCompletionSource<bool>();
            }
            return Processor.Tcs.Task;
        }

        internal override W3CLoggerProcessor InitializeMessageQueue(IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, ILoggerFactory factory)
        {
            Processor = new TestW3CLoggerProcessor(options, environment, factory);
            return Processor;
        }
    }
}
