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

        public TestW3CLoggingMiddleware(RequestDelegate next, IOptionsMonitor<TestW3CLoggerOptions> options, IHostEnvironment environment) : base(next, options, environment) { }

        internal override W3CLogger InitializeLogger(IOptionsMonitor<W3CLoggerOptions> options)
        {
            Logger = new TestW3CLogger(options, ((TestW3CLoggerOptions)options.CurrentValue).NumWrites);
            return Logger;
        }
    }
}
