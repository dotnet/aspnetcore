// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Routing
{
    public class NullLoggerFactory : ILoggerFactory
    {
        public static NullLoggerFactory Instance = new NullLoggerFactory();

        public ILogger Create(string name)
        {
            return NullLogger.Instance;
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }
    }
}