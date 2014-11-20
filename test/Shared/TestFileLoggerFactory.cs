// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Identity.Test
{
    public class TestFileLoggerFactory : ILoggerFactory, IDisposable
    {
        private static Dictionary<string, ILogger> _loggers;

        static TestFileLoggerFactory()
        {
            _loggers = new Dictionary<string, ILogger>();
        }

        public void AddProvider(ILoggerProvider provider)
        {

        }

        public ILogger Create(string name)
        {
            if (!_loggers.ContainsKey(name))
            {
                _loggers.Add(name, new TestFileLogger(name));
            }

            return _loggers[name];
        }

        public void Dispose()
        {
            Parallel.ForEach(_loggers.Values, l =>
            {
                if(l is TestFileLogger)
                {
                    var logger = l as TestFileLogger;
                    File.Delete(logger.FileName);
                }
            });
        }
    } 
}
