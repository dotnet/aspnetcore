// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Blazor.Services
{
    internal class WebAssemblyLoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
            // No-op
        }

        public ILogger CreateLogger(string categoryName)
            => new WebAssemblyConsoleLogger<object>();

        public void Dispose()
        {
            // No-op
        }
    }
}
