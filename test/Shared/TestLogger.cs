// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Identity.Test
{
    public class TestLogger : ILogger
    {
        public IList<string> LogMessages { get; private set; } = new List<string>();

        public IDisposable BeginScope(object state)
        {
            LogMessages.Add(state?.ToString());
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            if (formatter == null)
            {
                LogMessages.Add(state.ToString());
            }
            else
            {
                LogMessages.Add(formatter(state, exception));
            }
        }
    }
}