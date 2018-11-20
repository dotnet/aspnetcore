// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Hosting.Fakes
{
    public class CustomLoggerFactory : ILoggerFactory
    {
        public void CustomConfigureMethod() { }

        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;

        public void Dispose() { }
    }

    public class SubLoggerFactory : CustomLoggerFactory { }

    public class NonSubLoggerFactory : ILoggerFactory
    {
        public void CustomConfigureMethod() { }

        public void AddProvider(ILoggerProvider provider) { }

        public ILogger CreateLogger(string categoryName) => NullLogger.Instance;

        public void Dispose() { }
    }
}
