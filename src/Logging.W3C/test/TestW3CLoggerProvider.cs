// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Logging.W3C.Tests
{
    internal class TestW3CLoggerProvider : W3CLoggerProvider
    {
        public TestW3CLoggerProvider(
            string path,
            W3CLoggingFields fields = W3CLoggingFields.Default,
            string fileName = "serverlog-",
            int maxFileSize = 10 * 1024 * 1024)
            : base(new OptionsWrapperMonitor<W3CLoggerOptions>(new W3CLoggerOptions()
            {
                LogDirectory = path,
                LoggingFields = fields,
                FileName = fileName,
                FileSizeLimit = maxFileSize
            }))
        {
        }
    }
}
