// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Identity.Test
{
    public class TestLoggerFactory : ILoggerFactory
    {
        public LogLevel MinimumLevel { get; set; }

        public void AddProvider(ILoggerProvider provider)
        {

        }

        public ILogger CreateLogger(string name)
        {
            return new TestLogger();
        }
    } 
}
