// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    internal partial class RequestQueue
    {
        private static class Log
        {
            private static readonly Action<ILogger, string?, Exception?> _attachedToQueue =
                LoggerMessage.Define<string?>(LogLevel.Information, LoggerEventIds.AttachedToQueue, "Attached to an existing request queue '{RequestQueueName}', some options do not apply.");

            public static void AttachedToQueue(ILogger logger, string? requestQueueName)
            {
                _attachedToQueue(logger, requestQueueName, null);
            }
        }
    }
}
