// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file contains stub implementations of all public API types for source-build on non-Windows platforms.
// These stubs allow reference assemblies to contain the correct API surface so that code referencing
// HttpSys types can compile on any platform, even though HttpSys only works on Windows at runtime.

using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Security.AccessControl;

namespace Microsoft.AspNetCore.Server.HttpSys
{
    /// <summary>
    /// Specifies protocols for authentication.
    /// </summary>
    [Flags]
    public enum AuthenticationSchemes
    {
        /// <summary>
        /// No authentication is enabled. This should only be used when HttpSysOptions.Authentication.AllowAnonymous is enabled (see <see cref="AuthenticationManager.AllowAnonymous"/>).
        /// </summary>
        None = 0x0,

        /// <summary>
        /// Specifies basic authentication.
        /// </summary>
        Basic = 0x1,

        /// <summary>
        /// Specifies NTLM authentication.
        /// </summary>
        NTLM = 0x4,

        /// <summary>
        /// Negotiates with the client to determine the authentication scheme. If both client and server support Kerberos, it is used;
        /// otherwise, NTLM is used.
        /// </summary>
        Negotiate = 0x8,

        /// <summary>
        /// Specifies Kerberos authentication.
        /// </summary>
        Kerberos = 0x10
    }

    /// <summary>
    /// Describes the client certificate negotiation method for HTTPS connections.
    /// </summary>
    public enum ClientCertificateMethod
    {
        /// <summary>
        /// A client certificate will not be populated on the request.
        /// </summary>
        NoCertificate = 0,

        /// <summary>
        /// A client certificate will be populated if already present at the start of a request.
        /// </summary>
        AllowCertificate,

        /// <summary>
        /// The TLS session can be renegotiated to request a client certificate.
        /// </summary>
        AllowRenegotation
    }

    /// <summary>
    /// Enum declaring the allowed values for the verbosity level when http.sys reject requests due to throttling.
    /// </summary>
    public enum Http503VerbosityLevel : long
    {
        /// <summary>
        /// A 503 response is not sent; the connection is reset. This is the default HTTP Server API behavior.
        /// </summary>
        Basic = 0,

        /// <summary>
        /// The HTTP Server API sends a 503 response with a "Service Unavailable" reason phrase.
        /// </summary>
        Limited = 1,

        /// <summary>
        /// The HTTP Server API sends a 503 response with a detailed reason phrase.
        /// </summary>
        Full = 2
    }

    /// <summary>
    /// Defines the types of request processing timestamps exposed via the Http.Sys HTTP_REQUEST_TIMING_INFO extensibility point.
    /// </summary>
    /// <remarks>
    /// Use <see cref="IHttpSysRequestTimingFeature"/> to access these timestamps.
    /// </remarks>
    public enum HttpSysRequestTimingType
    {
        /// <summary>
        /// Time the connection started.
        /// </summary>
        ConnectionStart,

        /// <summary>
        /// Time the first HTTP byte is received.
        /// </summary>
        DataStart,

        /// <summary>
        /// Time TLS certificate loading starts.
        /// </summary>
        TlsCertificateLoadStart,

        /// <summary>
        /// Time TLS certificate loading ends.
        /// </summary>
        TlsCertificateLoadEnd,

        /// <summary>
        /// Time TLS leg one handshake starts.
        /// </summary>
        TlsHandshakeLeg1Start,

        /// <summary>
        /// Time TLS leg one handshake ends.
        /// </summary>
        TlsHandshakeLeg1End,

        /// <summary>
        /// Time TLS leg two handshake starts.
        /// </summary>
        TlsHandshakeLeg2Start,

        /// <summary>
        /// Time TLS leg two handshake ends.
        /// </summary>
        TlsHandshakeLeg2End,

        /// <summary>
        /// Time TLS attribute query starts.
        /// </summary>
        TlsAttributesQueryStart,

        /// <summary>
        /// Time TLS attribute query ends.
        /// </summary>
        TlsAttributesQueryEnd,

        /// <summary>
        /// Time TLS client cert query starts.
        /// </summary>
        TlsClientCertQueryStart,

        /// <summary>
        /// Time TLS client cert query ends.
        /// </summary>
        TlsClientCertQueryEnd,

        /// <summary>
        /// Time HTTP2 streaming starts.
        /// </summary>
        Http2StreamStart,

        /// <summary>
        /// Time HTTP2 header decoding starts.
        /// </summary>
        Http2HeaderDecodeStart,

        /// <summary>
        /// Time HTTP2 header decoding ends.
        /// </summary>
        Http2HeaderDecodeEnd,

        /// <summary>
        /// Time HTTP header parsing starts.
        /// </summary>
        RequestHeaderParseStart,

        /// <summary>
        /// Time HTTP header parsing ends.
        /// </summary>
        RequestHeaderParseEnd,

        /// <summary>
        /// Time Http.Sys starts to determine which request queue to route the request to.
        /// </summary>
        RequestRoutingStart,

        /// <summary>
        /// Time Http.Sys has determined which request queue to route the request to.
        /// </summary>
        RequestRoutingEnd,

        /// <summary>
        /// Time the request is queued for inspection.
        /// </summary>
        RequestQueuedForInspection,

        /// <summary>
        /// Time the request is delivered for inspection.
        /// </summary>
        RequestDeliveredForInspection,

        /// <summary>
        /// Time the request has finished being inspected.
        /// </summary>
        RequestReturnedAfterInspection,

        /// <summary>
        /// Time the request is queued for delegation.
        /// </summary>
        RequestQueuedForDelegation,

        /// <summary>
        /// Time the request is delivered for delegation.
        /// </summary>
        RequestDeliveredForDelegation,

        /// <summary>
        /// Time the request was delegated.
        /// </summary>
        RequestReturnedAfterDelegation,

        /// <summary>
        /// Time the request was queued to the final request queue for processing.
        /// </summary>
        RequestQueuedForIO,

        /// <summary>
        /// Time the request was delivered to the final request queue for processing.
        /// </summary>
        RequestDeliveredForIO,

        /// <summary>
        /// Time HTTP3 streaming starts.
        /// </summary>
        Http3StreamStart,

        /// <summary>
        /// Time HTTP3 header decoding starts.
        /// </summary>
        Http3HeaderDecodeStart,

        /// <summary>
        /// Time HTTP3 header decoding ends.
        /// </summary>
        Http3HeaderDecodeEnd,
    }

    /// <summary>
    /// Used to indicate if this server instance should create a new Http.Sys request queue
    /// or attach to an existing one.
    /// </summary>
    public enum RequestQueueMode
    {
        /// <summary>
        /// Create a new queue. This will fail if there's an existing queue with the same name.
        /// </summary>
        Create = 0,

        /// <summary>
        /// Attach to an existing queue with the name given. This will fail if the queue does not already exist.
        /// Most configuration options do not apply when attaching to an existing queue.
        /// </summary>
        Attach,

        /// <summary>
        /// Create a queue with the given name if it does not already exist, otherwise attach to the existing queue.
        /// Most configuration options do not apply when attaching to an existing queue.
        /// </summary>
        CreateOrAttach
    }

    /// <summary>
    /// This exposes the creation of delegation rules on request queues owned by the server.
    /// </summary>
    public interface IServerDelegationFeature
    {
        /// <summary>
        /// Create a delegation rule on request queue owned by the server.
        /// </summary>
        /// <param name="queueName">The name of the Http.Sys request queue.</param>
        /// <param name="urlPrefix">The URL of the Http.Sys Url Prefix.</param>
        /// <returns>
        /// Creates a <see cref="DelegationRule"/> that can used to delegate individual requests.
        /// </returns>
        DelegationRule CreateDelegationRule(string queueName, string urlPrefix);
    }

    /// <summary>
    /// Interface for delegating requests to other Http.Sys request queues.
    /// </summary>
    public interface IHttpSysRequestDelegationFeature
    {
        /// <summary>
        /// Indicates if the server can delegate this request to another HttpSys request queue.
        /// </summary>
        bool CanDelegate { get; }

        /// <summary>
        /// Attempt to delegate the request to another Http.Sys request queue. The request body
        /// must not be read nor the response started before this is invoked. Check <see cref="CanDelegate"/>
        /// before invoking.
        /// </summary>
        /// <param name="destination">The rule maintaining the handle to the destination queue.</param>
        void DelegateRequest(DelegationRule destination);
    }

    /// <summary>
    /// This exposes the Http.Sys HTTP_REQUEST_INFO extensibility point as opaque data for the caller to interperate.
    /// </summary>
    public interface IHttpSysRequestInfoFeature
    {
        /// <summary>
        /// A collection of the HTTP_REQUEST_INFO for the current request. The integer represents the identifying
        /// HTTP_REQUEST_INFO_TYPE enum value. The Memory is opaque bytes that need to be interperted in the format
        /// specified by the enum value.
        /// </summary>
        public IReadOnlyDictionary<int, ReadOnlyMemory<byte>> RequestInfo { get; }
    }

    /// <summary>
    /// Provides API to read HTTP_REQUEST_PROPERTY value from the HTTP.SYS request.
    /// </summary>
    public interface IHttpSysRequestPropertyFeature
    {
        /// <summary>
        /// Reads the TLS client hello from HTTP.SYS
        /// </summary>
        /// <param name="tlsClientHelloBytesDestination">Where the raw bytes of the TLS Client Hello message are written.</param>
        /// <param name="bytesReturned">
        /// Returns the number of bytes written to <paramref name="tlsClientHelloBytesDestination"/>.
        /// Or can return the size of the buffer needed if <paramref name="tlsClientHelloBytesDestination"/> wasn't large enough.
        /// </param>
        /// <returns>
        /// True, if fetching TLS client hello was successful, false if <paramref name="tlsClientHelloBytesDestination"/> size is not large enough.
        /// If unsuccessful for other reason throws an exception.
        /// </returns>
        bool TryGetTlsClientHello(Span<byte> tlsClientHelloBytesDestination, out int bytesReturned);
    }

    /// <summary>
    /// This exposes the Http.Sys HTTP_REQUEST_TIMING_INFO extensibility point which contains request processing timestamp data from Http.Sys.
    /// </summary>
    public interface IHttpSysRequestTimingFeature
    {
        /// <summary>
        /// Gets all Http.Sys timing timestamps
        /// </summary>
        ReadOnlySpan<long> Timestamps { get; }

        /// <summary>
        /// Gets the timestamp for the given timing.
        /// </summary>
        /// <param name="timestampType">The timestamp type to get.</param>
        /// <param name="timestamp">The value of the timestamp if set.</param>
        /// <returns>True if the given timing was set (i.e., non-zero).</returns>
        bool TryGetTimestamp(HttpSysRequestTimingType timestampType, out long timestamp);

        /// <summary>
        /// Gets the elapsed time between the two given timings.
        /// </summary>
        /// <param name="startingTimestampType">The timestamp type marking the beginning of the time period.</param>
        /// <param name="endingTimestampType">The timestamp type marking the end of the time period.</param>
        /// <param name="elapsed">A <see cref="TimeSpan"/> for the elapsed time between the starting and ending timestamps.</param>
        /// <returns>True if both given timings were set (i.e., non-zero).</returns>
        bool TryGetElapsedTime(HttpSysRequestTimingType startingTimestampType, HttpSysRequestTimingType endingTimestampType, out TimeSpan elapsed);
    }

    /// <summary>
    /// Constants for HttpSys.
    /// </summary>
    public static class HttpSysDefaults
    {
        /// <summary>
        /// The name of the authentication scheme used.
        /// </summary>
        public const string AuthenticationScheme = "Windows";
    }

    /// <summary>
    /// Exception thrown by HttpSys when an error occurs
    /// </summary>
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
    public class HttpSysException : Win32Exception
    {
        internal HttpSysException()
            : base()
        {
        }

        internal HttpSysException(int errorCode)
            : base(errorCode)
        {
        }

        internal HttpSysException(int errorCode, string message)
            : base(errorCode, message)
        {
        }

        /// <inheritdoc />
        public override int ErrorCode
        {
            get
            {
                return NativeErrorCode;
            }
        }
    }

    /// <summary>
    /// Exposes the Http.Sys authentication configurations.
    /// </summary>
    public sealed class AuthenticationManager
    {
        internal AuthenticationManager()
        {
        }

        /// <summary>
        /// When attaching to an existing queue this setting must match the one used to create the queue.
        /// </summary>
        public AuthenticationSchemes Schemes { get; set; }

        /// <summary>
        /// Indicates if anonymous requests will be surfaced to the application or challenged by the server.
        /// The default value is true.
        /// </summary>
        public bool AllowAnonymous { get; set; } = true;

        /// <summary>
        /// If true the server should set HttpContext.User. If false the server will only provide an
        /// identity when explicitly requested by the AuthenticationScheme. The default is true.
        /// </summary>
        public bool AutomaticAuthentication { get; set; } = true;

        /// <summary>
        /// Sets the display name shown to users on login pages. The default is null.
        /// </summary>
        public string? AuthenticationDisplayName { get; set; }

        /// <summary>
        /// If true, the Kerberos authentication credentials are persisted per connection
        /// and re-used for subsequent anonymous requests on the same connection.
        /// Kerberos or Negotiate authentication must be enabled. The default is false.
        /// </summary>
        public bool EnableKerberosCredentialCaching { get; set; }

        /// <summary>
        /// If true, the server captures user credentials from the thread that starts the
        /// host and impersonates that user during Kerberos or Negotiate authentication.
        /// Kerberos or Negotiate authentication must be enabled. The default is false.
        /// </summary>
        public bool CaptureCredentials { get; set; }
    }

    /// <summary>
    /// Exposes the Http.Sys timeout configurations. These may also be configured in the registry.
    /// These settings do not apply when attaching to an existing queue.
    /// </summary>
    public sealed class TimeoutManager
    {
        internal TimeoutManager()
        {
        }

        /// <summary>
        /// The time, in seconds, allowed for the request entity body to arrive. The default timer is 2 minutes.
        /// </summary>
        public TimeSpan EntityBody { get; set; }

        /// <summary>
        /// The time, in seconds, allowed for the HTTP Server API to drain the entity body on a Keep-Alive connection.
        /// The default timer is 2 minutes.
        /// </summary>
        public TimeSpan DrainEntityBody { get; set; }

        /// <summary>
        /// The time, in seconds, allowed for the request to remain in the request queue before the application picks
        /// it up. The default timer is 2 minutes.
        /// </summary>
        public TimeSpan RequestQueue { get; set; }

        /// <summary>
        /// The time, in seconds, allowed for an idle connection. The default timer is 2 minutes.
        /// </summary>
        public TimeSpan IdleConnection { get; set; }

        /// <summary>
        /// The time, in seconds, allowed for the HTTP Server API to parse the request header. The default timer is
        /// 2 minutes.
        /// </summary>
        public TimeSpan HeaderWait { get; set; }

        /// <summary>
        /// The minimum send rate, in bytes-per-second, for the response. The default response send rate is 150
        /// bytes-per-second.
        /// </summary>
        public long MinSendBytesPerSecond { get; set; }
    }

    /// <summary>
    /// Rule that maintains a handle to the Request Queue and UrlPrefix to
    /// delegate to.
    /// </summary>
    public class DelegationRule : IDisposable
    {
        internal DelegationRule()
        {
        }

        /// <summary>
        /// The name of the Http.Sys request queue
        /// </summary>
        public string QueueName { get; } = string.Empty;

        /// <summary>
        /// The URL of the Http.Sys Url Prefix
        /// </summary>
        public string UrlPrefix { get; } = string.Empty;

        /// <inheritdoc />
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// A set of URL parameters used to listen for incoming requests.
    /// </summary>
    public class UrlPrefix
    {
        private UrlPrefix()
        {
        }

        /// <summary>
        /// Gets a value that determines if the prefix's scheme is HTTPS.
        /// </summary>
        public bool IsHttps { get; }

        /// <summary>
        /// Gets the scheme used by the prefix.
        /// </summary>
        public string Scheme { get; } = string.Empty;

        /// <summary>
        /// Gets the host domain name used by the prefix.
        /// </summary>
        public string Host => throw new PlatformNotSupportedException();

        /// <summary>
        /// Gets a string representation of the port used by the prefix.
        /// </summary>
        public string Port { get; } = string.Empty;

        /// <summary>
        /// Gets an integer representation of the port used by the prefix.
        /// </summary>
        public int PortValue { get; }

        /// <summary>
        /// Gets the path component of the prefix.
        /// </summary>
        public string Path { get; } = string.Empty;

        /// <summary>
        /// Gets a string representation of the prefix
        /// </summary>
        public string FullPrefix { get; } = string.Empty;

        /// <summary>
        /// Creates a <see cref="UrlPrefix"/> from the given string.
        /// </summary>
        /// <param name="prefix">The string that the <see cref="UrlPrefix"/> will be created from.</param>
        /// <returns>A new <see cref="UrlPrefix"/>.</returns>
        public static UrlPrefix Create(string prefix) => throw new PlatformNotSupportedException();

        /// <summary>
        /// Creates a <see cref="UrlPrefix"/> from the given components.
        /// </summary>
        /// <param name="scheme">http or https.</param>
        /// <param name="host">The host name.</param>
        /// <param name="port">The port.</param>
        /// <param name="path">The path.</param>
        /// <returns>A new <see cref="UrlPrefix"/>.</returns>
        public static UrlPrefix Create(string scheme, string host, string port, string path) => throw new PlatformNotSupportedException();

        /// <summary>
        /// Creates a <see cref="UrlPrefix"/> from the given components.
        /// </summary>
        /// <param name="scheme">http or https.</param>
        /// <param name="host">The host name.</param>
        /// <param name="portValue">The port value.</param>
        /// <param name="path">The path.</param>
        /// <returns>A new <see cref="UrlPrefix"/>.</returns>
        public static UrlPrefix Create(string scheme, string host, int? portValue, string path) => throw new PlatformNotSupportedException();

        /// <inheritdoc />
        public override bool Equals(object? obj) => throw new PlatformNotSupportedException();

        /// <inheritdoc />
        public override int GetHashCode() => throw new PlatformNotSupportedException();

        /// <inheritdoc />
        public override string ToString() => throw new PlatformNotSupportedException();
    }

    /// <summary>
    /// A collection or URL prefixes
    /// </summary>
    public class UrlPrefixCollection : ICollection<UrlPrefix>
    {
        internal UrlPrefixCollection()
        {
        }

        /// <inheritdoc />
        public int Count => 0;

        /// <summary>
        /// Gets a value that determines if this collection is readOnly.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Creates a <see cref="UrlPrefix"/> from the given string, and adds it to this collection.
        /// </summary>
        /// <param name="prefix">The string representing the <see cref="UrlPrefix"/> to add to this collection.</param>
        public void Add(string prefix)
        {
        }

        /// <summary>
        /// Adds a <see cref="UrlPrefix"/> to this collection.
        /// </summary>
        /// <param name="item">The prefix to add to this collection.</param>
        public void Add(UrlPrefix item)
        {
        }

        /// <inheritdoc />
        public void Clear()
        {
        }

        /// <inheritdoc />
        public bool Contains(UrlPrefix item) => false;

        /// <inheritdoc />
        public void CopyTo(UrlPrefix[] array, int arrayIndex)
        {
        }

        /// <inheritdoc />
        public bool Remove(string prefix) => false;

        /// <inheritdoc />
        public bool Remove(UrlPrefix item) => false;

        /// <summary>
        /// Returns an enumerator that iterates through this collection.
        /// </summary>
        /// <returns>An enumerator for the collection.</returns>
        public IEnumerator<UrlPrefix> GetEnumerator() => ((IEnumerable<UrlPrefix>)Array.Empty<UrlPrefix>()).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// Contains the options used by HttpSys.
    /// </summary>
    public class HttpSysOptions
    {
        /// <summary>
        /// Initializes a new <see cref="HttpSysOptions"/>.
        /// </summary>
        public HttpSysOptions()
        {
        }

        /// <summary>
        /// The name of the Http.Sys request queue.
        /// The default is <c>null</c> (Anonymous queue).
        /// </summary>
        public string? RequestQueueName { get; set; }

        /// <summary>
        /// This indicates whether the server is responsible for creating and configuring the request queue,
        /// or if it should attach to an existing queue.
        /// The default is <c>RequestQueueMode.Create</c>.
        /// </summary>
        public RequestQueueMode RequestQueueMode { get; set; }

        /// <summary>
        /// Indicates how client certificates should be populated. The default is to allow a certificate without renegotiation.
        /// </summary>
        public ClientCertificateMethod ClientCertificateMethod { get; set; } = ClientCertificateMethod.AllowCertificate;

        /// <summary>
        /// The maximum number of concurrent accepts.
        /// </summary>
        public int MaxAccepts { get; set; }

        /// <summary>
        /// Attempt kernel-mode caching for responses with eligible headers.
        /// The default is <c>true</c>.
        /// </summary>
        public bool EnableResponseCaching { get; set; } = true;

        /// <summary>
        /// The url prefixes to register with Http.Sys.
        /// </summary>
        public UrlPrefixCollection UrlPrefixes { get; } = new UrlPrefixCollection();

        /// <summary>
        /// Http.Sys authentication settings.
        /// </summary>
        public AuthenticationManager Authentication { get; } = new AuthenticationManager();

        /// <summary>
        /// Exposes the Http.Sys timeout configurations.
        /// </summary>
        public TimeoutManager Timeouts { get; } = new TimeoutManager();

        /// <summary>
        /// Gets or Sets if response body writes that fail due to client disconnects should throw exceptions or
        /// complete normally. The default is <c>false</c>.
        /// </summary>
        public bool ThrowWriteExceptions { get; set; }

        /// <summary>
        /// Enable buffering of response data in the Kernel. The default value is <c>false</c>.
        /// </summary>
        public bool EnableKernelResponseBuffering { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of concurrent connections to accept. Set <c>-1</c> for infinite.
        /// Set to <c>null</c> to use the registry's machine-wide setting.
        /// The default value is <c>null</c>.
        /// </summary>
        public long? MaxConnections { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of requests that will be queued up in Http.Sys.
        /// The default is 1000.
        /// </summary>
        public long RequestQueueLimit { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the security descriptor for the request queue.
        /// </summary>
        public GenericSecurityDescriptor? RequestQueueSecurityDescriptor { get; set; }

        /// <summary>
        /// Gets or sets the maximum allowed size of any request body in bytes.
        /// When set to null, the maximum request body size is unlimited.
        /// The default is set to 30,000,000 bytes, which is approximately 28.6MB.
        /// </summary>
        public long? MaxRequestBodySize { get; set; } = 30000000;

        /// <summary>
        /// Control whether synchronous input/output is allowed for the HttpContext.Request.Body and HttpContext.Response.Body.
        /// The default is <c>false</c>.
        /// </summary>
        public bool AllowSynchronousIO { get; set; }

        /// <summary>
        /// Gets or sets a value that controls how http.sys reacts when rejecting requests due to throttling conditions.
        /// </summary>
        public Http503VerbosityLevel Http503Verbosity { get; set; }

        /// <summary>
        /// Inline request processing instead of dispatching to the threadpool.
        /// </summary>
        public bool UnsafePreferInlineScheduling { get; set; }

        /// <summary>
        /// Configures request headers to use Latin1 encoding.
        /// </summary>
        public bool UseLatin1RequestHeaders { get; set; }
    }
}

namespace Microsoft.AspNetCore.Hosting
{
    using Microsoft.AspNetCore.Server.HttpSys;

    /// <summary>
    /// Provides extensions method to use Http.sys as the server for the web host.
    /// </summary>
    public static class WebHostBuilderHttpSysExtensions
    {
        /// <summary>
        /// Specify Http.sys as the server to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
        /// </param>
        /// <returns>
        /// A reference to the <see cref="IWebHostBuilder" /> parameter object.
        /// </returns>
        [SupportedOSPlatform("windows")]
        public static IWebHostBuilder UseHttpSys(this IWebHostBuilder hostBuilder) => hostBuilder;

        /// <summary>
        /// Specify Http.sys as the server to be used by the web host.
        /// </summary>
        /// <param name="hostBuilder">
        /// The Microsoft.AspNetCore.Hosting.IWebHostBuilder to configure.
        /// </param>
        /// <param name="options">
        /// A callback to configure Http.sys options.
        /// </param>
        /// <returns>
        /// A reference to the <see cref="IWebHostBuilder" /> parameter object.
        /// </returns>
        [SupportedOSPlatform("windows")]
        public static IWebHostBuilder UseHttpSys(this IWebHostBuilder hostBuilder, Action<HttpSysOptions> options) => hostBuilder;
    }
}
