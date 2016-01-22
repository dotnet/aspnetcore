// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Diagnostics.Elm
{
    public class LogInfo
    {
        public ActivityContext ActivityContext { get; set; }

        public string Name { get; set; }

        public object State { get; set; }

        public Exception Exception { get; set; }

        public string Message { get; set; }

        public LogLevel Severity { get; set; }

        public int EventID { get; set; }

        public DateTimeOffset Time { get; set; }
    }
}