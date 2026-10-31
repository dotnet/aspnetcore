// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Owin;

internal static class OwinConstants
{
    #region OWIN v1.0.0 - 3.2.1. Request Data

    // http://owin.org/spec/spec/owin-1.0.0.html

    public const string RequestScheme = "owin.RequestScheme";
    public const string RequestMethod = "owin.RequestMethod";
    public const string RequestPathBase = "owin.RequestPathBase";
    public const string RequestPath = "owin.RequestPath";
    public const string RequestQueryString = "owin.RequestQueryString";
    public const string RequestProtocol = "owin.RequestProtocol";
    public const string RequestHeaders = "owin.RequestHeaders";
    public const string RequestBody = "owin.RequestBody";

    #endregion

    #region OWIN v1.0.1 - 3.2.1 Request Data

    // OWIN 1.0.1 http://owin.org/html/owin.html

    public const string RequestId = "owin.RequestId";
    public const string RequestUser = "owin.RequestUser";

    #endregion

    #region OWIN v1.0.0 - 3.2.2. Response Data

    // http://owin.org/spec/spec/owin-1.0.0.html

    public const string ResponseStatusCode = "owin.ResponseStatusCode";
    public const string ResponseReasonPhrase = "owin.ResponseReasonPhrase";
    public const string ResponseProtocol = "owin.ResponseProtocol";
    public const string ResponseHeaders = "owin.ResponseHeaders";
    public const string ResponseBody = "owin.ResponseBody";

    #endregion

    #region OWIN v1.0.0 - 3.2.3. Other Data

    // http://owin.org/spec/spec/owin-1.0.0.html

    public const string CallCancelled = "owin.CallCancelled";

    public const string OwinVersion = "owin.Version";

    #endregion

    #region OWIN Keys for IAppBuilder.Properties

    internal static class Builder
    {
        public const string AddSignatureConversion = "builder.AddSignatureConversion";
        public const string DefaultApp = "builder.DefaultApp";
    }

    #endregion

    #region OWIN Key Guidelines and Common Keys - 6. Common keys

    // http://owin.org/spec/spec/CommonKeys.html

    internal static class CommonKeys
    {
        public const string ClientCertificate = "ssl.ClientCertificate";
        public const string LoadClientCertAsync = "ssl.LoadClientCertAsync";
        public const string RemoteIpAddress = "server.RemoteIpAddress";
        public const string RemotePort = "server.RemotePort";
        public const string LocalIpAddress = "server.LocalIpAddress";
        public const string LocalPort = "server.LocalPort";
        public const string ConnectionId = "server.ConnectionId";
        public const string TraceOutput = "host.TraceOutput";
        public const string Addresses = "host.Addresses";
        public const string AppName = "host.AppName";
        public const string Capabilities = "server.Capabilities";
        public const string OnSendingHeaders = "server.OnSendingHeaders";
        public const string OnAppDisposing = "host.OnAppDisposing";
        public const string Scheme = "scheme";
        public const string Host = "host";
        public const string Port = "port";
        public const string Path = "path";
    }

    #endregion

    #region SendFiles v0.3.0

    // http://owin.org/spec/extensions/owin-SendFile-Extension-v0.3.0.htm

    internal static class SendFiles
    {
        // 3.1. Startup

        public const string Version = "sendfile.Version";
        public const string Support = "sendfile.Support";
        public const string Concurrency = "sendfile.Concurrency";

        // 3.2. Per Request

        public const string SendAsync = "sendfile.SendAsync";
    }

    #endregion

    #region Opaque v0.3.0

    // http://owin.org/spec/extensions/owin-OpaqueStream-Extension-v0.3.0.htm

    internal static class OpaqueConstants
    {
        // 3.1. Startup

        public const string Version = "opaque.Version";

        // 3.2. Per Request

        public const string Upgrade = "opaque.Upgrade";

        // 5. Consumption

        public const string Stream = "opaque.Stream";
        // public const string Version = "opaque.Version"; // redundant, declared above
        public const string CallCancelled = "opaque.CallCancelled";
    }

    #endregion

    #region WebSocket v0.4.0

    // http://owin.org/spec/extensions/owin-OpaqueStream-Extension-v0.3.0.htm

    internal static class WebSocket
    {
        // 3.1. Startup

        public const string Version = "websocket.Version";
        public const string VersionValue = "1.0";

        // 3.2. Per Request

        public const string Accept = "websocket.Accept";
        public const string AcceptAlt = "websocket.AcceptAlt"; // Non-spec

        // 4. Accept

        public const string SubProtocol = "websocket.SubProtocol";

        // 5. Consumption

        public const string SendAsync = "websocket.SendAsync";
        public const string ReceiveAsync = "websocket.ReceiveAsync";
        public const string CloseAsync = "websocket.CloseAsync";
        // public const string Version = "websocket.Version"; // redundant, declared above
        public const string CallCancelled = "websocket.CallCancelled";
        public const string ClientCloseStatus = "websocket.ClientCloseStatus";
        public const string ClientCloseDescription = "websocket.ClientCloseDescription";
    }

    #endregion

    #region Security v0.1.0

    internal static class Security
    {
        // 3.2. Per Request

        public const string User = "server.User";
    }

    #endregion
}
