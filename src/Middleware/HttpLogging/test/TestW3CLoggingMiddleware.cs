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

        public TestW3CLoggingMiddleware(RequestDelegate next, IOptionsMonitor<W3CLoggerOptions> options, TestW3CLogger w3cLogger) : base(next, options, w3cLogger)
        {
            Logger = w3cLogger;
        }

    }
}
