// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Hosting
{
    internal static class LoggerEventIds
    {
        public static readonly EventId RequestStarting = new EventId(1, "RequestStarting");
        public static readonly EventId RequestFinished = new EventId(2, "RequestFinished");
        public static readonly EventId Starting = new EventId(3, "Starting");
        public static readonly EventId Started = new EventId(4, "Started");
        public static readonly EventId Shutdown = new EventId(5, "Shutdown");
        public static readonly EventId ApplicationStartupException = new EventId(6, "ApplicationStartupException");
        public static readonly EventId ApplicationStoppingException = new EventId(7, "ApplicationStoppingException");
        public static readonly EventId ApplicationStoppedException = new EventId(8, "ApplicationStoppedException");
        public static readonly EventId HostedServiceStartException = new EventId(9, "HostedServiceStartException");
        public static readonly EventId HostedServiceStopException = new EventId(10, "HostedServiceStopException");
        public static readonly EventId HostingStartupAssemblyException = new EventId(11, "HostingStartupAssemblyException");
        public static readonly EventId ServerShutdownException = new EventId(12, "ServerShutdownException");
    }
}
