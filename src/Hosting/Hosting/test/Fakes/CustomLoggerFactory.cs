// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Hosting.Fakes;

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
