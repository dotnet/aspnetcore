// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Diagnostics.Entity
{
    public class DataStoreErrorLoggerProvider : ILoggerProvider
    {
        private readonly DataStoreErrorLogger _logger = new DataStoreErrorLogger();

        public virtual ILogger CreateLogger(string name)
        {
            return _logger;
        }

        public virtual DataStoreErrorLogger Logger
        {
            get { return _logger; }
        }

        public virtual void Dispose()
        {
        }
    }
}