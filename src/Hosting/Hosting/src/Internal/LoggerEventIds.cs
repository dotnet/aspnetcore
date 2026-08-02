// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Hosting;

internal static class LoggerEventIds
{
    public const int RequestStarting = 1;
    public const int RequestFinished = 2;
    public const int Starting = 3;
    public const int Started = 4;
    public const int Shutdown = 5;
    public const int ApplicationStartupException = 6;
    public const int ApplicationStoppingException = 7;
    public const int ApplicationStoppedException = 8;
    public const int HostedServiceStartException = 9;
    public const int HostedServiceStopException = 10;
    public const int HostingStartupAssemblyException = 11;
    public const int ServerShutdownException = 12;
    public const int HostingStartupAssemblyLoaded = 13;
    public const int ServerListeningOnAddresses = 14;
    public const int PortsOverridenByUrls = 15;
    public const int RequestUnhandled = 16;
}
