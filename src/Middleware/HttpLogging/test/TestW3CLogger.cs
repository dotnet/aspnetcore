// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal class TestW3CLogger : W3CLogger
    {
        public TestW3CLoggerProcessor Processor;
        private readonly int _numWrites;

        public TestW3CLogger(IOptionsMonitor<W3CLoggerOptions> options, int numWrites) : base(options)
        {
            _numWrites = numWrites;
        }

        public async Task WaitForWrites()
        {
            while (Processor.WriteCount != _numWrites)
            {
                await Task.Delay(100);
            }
        }

        internal override W3CLoggerProcessor InitializeMessageQueue(IOptionsMonitor<W3CLoggerOptions> options)
        {
            Processor = new TestW3CLoggerProcessor(options);
            return Processor;
        }
    }
}
