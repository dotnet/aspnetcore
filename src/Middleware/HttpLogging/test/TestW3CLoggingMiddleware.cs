// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging
{
    internal sealed class TestW3CLoggingMiddleware : W3CLoggingMiddleware
    {

        public TestW3CLogger Logger;
        private readonly int _numWrites;

        public TestW3CLoggingMiddleware(RequestDelegate next, IOptionsMonitor<W3CLoggerOptions> options, IHostEnvironment environment, int numWrites) : base(next, options, environment)
        {
            _numWrites = numWrites;
        }

        internal override W3CLogger InitializeLogger(IOptionsMonitor<W3CLoggerOptions> options)
        {
            Logger = new TestW3CLogger(options, _numWrites);
            return Logger;
        }
    }
}
