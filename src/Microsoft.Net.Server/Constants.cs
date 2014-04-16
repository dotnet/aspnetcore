// -----------------------------------------------------------------------
// <copyright file="Constants.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Microsoft.Net.Server
{
    internal static class Constants
    {
        internal const string VersionKey = "owin.Version";
        internal const string OwinVersion = "1.0";
        internal const string CallCancelledKey = "owin.CallCancelled";

        internal const string ServerCapabilitiesKey = "server.Capabilities";

        internal const string RequestBodyKey = "owin.RequestBody";
        internal const string RequestHeadersKey = "owin.RequestHeaders";
        internal const string RequestSchemeKey = "owin.RequestScheme";
        internal const string RequestMethodKey = "owin.RequestMethod";
        internal const string RequestPathBaseKey = "owin.RequestPathBase";
        internal const string RequestPathKey = "owin.RequestPath";
        internal const string RequestQueryStringKey = "owin.RequestQueryString";
        internal const string HttpRequestProtocolKey = "owin.RequestProtocol";

        internal const string HttpResponseProtocolKey = "owin.ResponseProtocol";
        internal const string ResponseStatusCodeKey = "owin.ResponseStatusCode";
        internal const string ResponseReasonPhraseKey = "owin.ResponseReasonPhrase";
        internal const string ResponseHeadersKey = "owin.ResponseHeaders";
        internal const string ResponseBodyKey = "owin.ResponseBody";

        internal const string ClientCertifiateKey = "ssl.ClientCertificate";

        internal const string RemoteIpAddressKey = "server.RemoteIpAddress";
        internal const string RemotePortKey = "server.RemotePort";
        internal const string LocalIpAddressKey = "server.LocalIpAddress";
        internal const string LocalPortKey = "server.LocalPort";
        internal const string IsLocalKey = "server.IsLocal";
        internal const string ServerOnSendingHeadersKey = "server.OnSendingHeaders";
        internal const string ServerLoggerFactoryKey = "server.LoggerFactory";

        internal const string OpaqueVersionKey = "opaque.Version";
        internal const string OpaqueVersion = "1.0";
        internal const string OpaqueFuncKey = "opaque.Upgrade";
        internal const string OpaqueStreamKey = "opaque.Stream";
        internal const string OpaqueCallCancelledKey = "opaque.CallCancelled";

        internal const string SendFileVersionKey = "sendfile.Version";
        internal const string SendFileVersion = "1.0";
        internal const string SendFileSupportKey = "sendfile.Support";
        internal const string SendFileConcurrencyKey = "sendfile.Concurrency";
        internal const string Overlapped = "Overlapped";

        internal const string HttpScheme = "http";
        internal const string HttpsScheme = "https";
        internal const string SchemeDelimiter = "://";

        internal static Version V1_0 = new Version(1, 0);
        internal static Version V1_1 = new Version(1, 1);
    }
}
