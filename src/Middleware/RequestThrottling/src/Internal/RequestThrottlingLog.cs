// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.RequestThrottling.Internal
{
    internal static class RequestThrottlingLog
    {
        private static readonly Action<ILogger, int, Exception> _requestEnqueued =
            LoggerMessage.Define<int>(LogLevel.Debug, new EventId(1, "Request Enqueued"), "Server is busy; request queued in middleware. Current queue length: {queuedRequests}.");

        private static readonly Action<ILogger, int, Exception> _requestDequeued =
            LoggerMessage.Define<int>(LogLevel.Debug, new EventId(2, "Request Dequeued"), "Space availible on server; request has left queue. Current queue length: {queuedRequests}.");

        internal static void RequestEnqueued(ILogger logger, int queuedRequests)
        {
            _requestEnqueued(logger, queuedRequests, null);
        }

        internal static void RequestDequeued(ILogger logger, int queuedRequests)
        {
            _requestDequeued(logger, queuedRequests, null);
        }
    }
}
