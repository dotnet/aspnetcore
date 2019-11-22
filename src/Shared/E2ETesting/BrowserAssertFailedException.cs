// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Xunit.Sdk;

namespace OpenQA.Selenium
{
    // Used to report errors when we find errors in the browser. This is useful
    // because the underlying assert probably doesn't provide good information in that
    // case.
    public class BrowserAssertFailedException : XunitException
    {
        public BrowserAssertFailedException(IReadOnlyList<LogEntry> logs, Exception innerException)
            : base(BuildMessage(logs), innerException)
        {
        }

        private static string BuildMessage(IReadOnlyList<LogEntry> logs)
        {
            return
                "Encountered browser errors while running assertion." + Environment.NewLine +
                string.Join(Environment.NewLine, logs);
        }
    }
}
