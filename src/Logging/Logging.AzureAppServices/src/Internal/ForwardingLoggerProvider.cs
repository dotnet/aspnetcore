// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.AzureAppServices.Internal
{
    internal class ForwardingLoggerProvider : ILoggerProvider
    {
        private readonly ILoggerFactory _loggerFactory;

        public ForwardingLoggerProvider(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public void Dispose()
        {
            _loggerFactory.Dispose();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggerFactory.CreateLogger(categoryName);
        }
    }
}