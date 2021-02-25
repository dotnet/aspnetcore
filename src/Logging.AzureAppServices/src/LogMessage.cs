// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Extensions.Logging.AzureAppServices
{
    internal readonly struct LogMessage
    {
        public LogMessage(DateTimeOffset timestamp, string message)
        {
            Timestamp = timestamp;
            Message = message;
        }

        public DateTimeOffset Timestamp { get; }
        public string Message { get; }
    }
}
