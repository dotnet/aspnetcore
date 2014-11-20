// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Identity.Test
{
    public class TestFileLogger : ILogger
    {
        public string FileName { get; set; }

        public object FileLock { get; private set; } = new object();

        public TestFileLogger(string name)
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IdentityTests");
            Directory.CreateDirectory(directory);
            FileName = Path.Combine(directory, (name + DateTime.Now.Ticks + "log.txt"));
            if (!File.Exists(FileName))
            {
                File.Create(FileName).Close();
            }
        }

        public IDisposable BeginScope(object state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Write(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            lock (FileLock)
            {
                File.AppendAllLines(FileName, new string[] { state.ToString() });
            }
        }
    }
}