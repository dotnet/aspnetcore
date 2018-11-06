// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging.Testing
{
    public interface ILoggedTest : IDisposable
    {
        ILogger Logger { get; }

        ILoggerFactory LoggerFactory { get; }

        ITestOutputHelper TestOutputHelper { get; }

        // For back compat
        IDisposable StartLog(out ILoggerFactory loggerFactory, LogLevel minLogLevel, string testName);

        void Initialize(MethodInfo methodInfo, object[] testMethodArguments, ITestOutputHelper testOutputHelper);
    }
}
