// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    internal static class LoggerEventIds
    {
        public static readonly EventId ConfiguringAPIResource = new EventId(1, "ConfiguringAPIResource");
        public static readonly EventId ConfiguringLocalAPIResource = new EventId(2, "ConfiguringLocalAPIResource");
        public static readonly EventId ConfiguringClient = new EventId(3, "ConfiguringClient");
        public static readonly EventId AllowedApplicationNotDefienedForIdentityResource = new EventId(4, "AllowedApplicationNotDefienedForIdentityResource");
        public static readonly EventId AllApplicationsAllowedForIdentityResource = new EventId(5, "AllApplicationsAllowedForIdentityResource");
        public static readonly EventId ApplicationsAllowedForIdentityResource = new EventId(6, "ApplicationsAllowedForIdentityResource");
        public static readonly EventId AllowedApplicationNotDefienedForApiResource = new EventId(7, "AllowedApplicationNotDefienedForApiResource");
        public static readonly EventId AllApplicationsAllowedForApiResource = new EventId(8, "AllApplicationsAllowedForApiResource");
        public static readonly EventId ApplicationsAllowedForApiResource = new EventId(9, "ApplicationsAllowedForApiResource");
        public static readonly EventId DevelopmentKeyLoaded = new EventId(10, "DevelopmentKeyLoaded");
        public static readonly EventId CertificateLoadedFromFile = new EventId(11, "CertificateLoadedFromFile");
        public static readonly EventId CertificateLoadedFromStore = new EventId(12, "CertificateLoadedFromStore");
        public static readonly EventId EndingSessionFailed = new EventId(13, "EndingSessionFailed");
    }
}
