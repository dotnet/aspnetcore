// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpSys.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Windows.Win32.Networking.HttpServer;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal sealed partial class Request
{
    private X509Certificate2? _clientCert;
    // TODO: https://github.com/aspnet/HttpSysServer/issues/231
    // private byte[] _providedTokenBindingId;
    // private byte[] _referredTokenBindingId;

    private BoundaryType _contentBoundaryType;

    private long? _contentLength;
    private RequestStream? _nativeStream;

    private AspNetCore.HttpSys.Internal.SocketAddress? _localEndPoint;
    private AspNetCore.HttpSys.Internal.SocketAddress? _remoteEndPoint;

    private bool _isDisposed;

    internal Request(RequestContext requestContext)
    {
        // TODO: Verbose log
        RequestContext = requestContext;
        _contentBoundaryType = BoundaryType.None;

        RequestId = requestContext.RequestId;
        // For HTTP/2 Http.Sys assigns each request a unique connection id for use with API calls, but the RawConnectionId represents the real connection.
        UConnectionId = requestContext.ConnectionId;
        RawConnectionId = requestContext.RawConnectionId;
        SslStatus = requestContext.SslStatus;

        KnownMethod = requestContext.VerbId;
        Method = requestContext.GetVerb()!;

        RawUrl = requestContext.GetRawUrl()!;

        var cookedUrl = requestContext.GetCookedUrl();
        QueryString = cookedUrl.GetQueryString() ?? string.Empty;

        var rawUrlInBytes = requestContext.GetRawUrlInBytes();
        var originalPath = RequestUriBuilder.DecodeAndUnescapePath(rawUrlInBytes);

        PathBase = string.Empty;
        Path = originalPath;
        var prefix = requestContext.Server.Options.UrlPrefixes.GetPrefix((int)requestContext.UrlContext);

        // 'OPTIONS * HTTP/1.1'
        if (KnownMethod == HTTP_VERB.HttpVerbOPTIONS && string.Equals(RawUrl, "*", StringComparison.Ordinal))
        {
            PathBase = string.Empty;
            Path = string.Empty;
        }
        // Prefix may be null if the requested has been transferred to our queue
        else if (prefix is not null)
        {
            var pathBase = prefix.PathWithoutTrailingSlash;

            // url: /base/path, prefix: /base/, base: /base, path: /path
            // url: /, prefix: /, base: , path: /
            if (originalPath.Equals(pathBase, StringComparison.Ordinal))
            {
                // Exact match, no need to preserve the casing
                PathBase = pathBase;
                Path = string.Empty;
            }
            else if (originalPath.Equals(pathBase, StringComparison.OrdinalIgnoreCase))
            {
                // Preserve the user input casing
                PathBase = originalPath;
                Path = string.Empty;
            }
            else if (originalPath.StartsWith(prefix.Path, StringComparison.Ordinal))
            {
                // Exact match, no need to preserve the casing
                PathBase = pathBase;
                Path = originalPath[pathBase.Length..];
            }
            else if (originalPath.StartsWith(prefix.Path, StringComparison.OrdinalIgnoreCase))
            {
                // Preserve the user input casing
                PathBase = originalPath[..pathBase.Length];
                Path = originalPath[pathBase.Length..];
            }
            else
            {
                // Http.Sys path base matching is based on the cooked url which applies some non-standard normalizations that we don't use
                // like collapsing duplicate slashes "//", converting '\' to '/', and un-escaping "%2F" to '/'. Find the right split and
                // ignore the normalizations.
                var originalOffset = 0;
                var baseOffset = 0;
                while (originalOffset < originalPath.Length && baseOffset < pathBase.Length)
                {
                    var baseValue = pathBase[baseOffset];
                    var offsetValue = originalPath[originalOffset];
                    if (baseValue == offsetValue
                        || char.ToUpperInvariant(baseValue) == char.ToUpperInvariant(offsetValue))
                    {
                        // case-insensitive match, continue
                        originalOffset++;
                        baseOffset++;
                    }
                    else if (baseValue == '/' && offsetValue == '\\')
                    {
                        // Http.Sys considers these equivalent
                        originalOffset++;
                        baseOffset++;
                    }
                    else if (baseValue == '/' && originalPath.AsSpan(originalOffset).StartsWith("%2F", StringComparison.OrdinalIgnoreCase))
                    {
                        // Http.Sys un-escapes this
                        originalOffset += 3;
                        baseOffset++;
                    }
                    else if (baseOffset > 0 && pathBase[baseOffset - 1] == '/'
                        && (offsetValue == '/' || offsetValue == '\\'))
                    {
                        // Duplicate slash, skip
                        originalOffset++;
                    }
                    else if (baseOffset > 0 && pathBase[baseOffset - 1] == '/'
                        && originalPath.AsSpan(originalOffset).StartsWith("%2F", StringComparison.OrdinalIgnoreCase))
                    {
                        // Duplicate slash equivalent, skip
                        originalOffset += 3;
                    }
                    else
                    {
                        // Mismatch, fall back
                        // The failing test case here is "/base/call//../bat//path1//path2", reduced to "/base/call/bat//path1//path2",
                        // where http.sys collapses "//" before "../", but we do "../" first. We've lost the context that there were dot segments,
                        // or duplicate slashes, how do we figure out that "call/" can be eliminated?
                        originalOffset = 0;
                        break;
                    }
                }
                PathBase = originalPath[..originalOffset];
                Path = originalPath[originalOffset..];
            }
        }
        else if (requestContext.Server.Options.UrlPrefixes.TryMatchLongestPrefix(IsHttps, cookedUrl.GetHost()!, originalPath, out var pathBase, out var path))
        {
            PathBase = pathBase;
            Path = path;
        }

        ProtocolVersion = RequestContext.GetVersion();

        Headers = new RequestHeaders(RequestContext);

        User = RequestContext.GetUser();

        SniHostName = string.Empty;
        if (IsHttps)
        {
            GetTlsHandshakeResults();
        }

        // GetTlsTokenBindingInfo(); TODO: https://github.com/aspnet/HttpSysServer/issues/231

        // Finished directly accessing the HTTP_REQUEST structure.
        RequestContext.ReleasePins();
        // TODO: Verbose log parameters

        RemoveContentLengthIfTransferEncodingContainsChunked();
    }

    internal ulong UConnectionId { get; }

    internal ulong RawConnectionId { get; }

    // No ulongs in public APIs...
    public long ConnectionId => RawConnectionId != 0 ? (long)RawConnectionId : (long)UConnectionId;

    internal ulong RequestId { get; }

    private SslStatus SslStatus { get; }

    private RequestContext RequestContext { get; }

    // With the leading ?, if any
    public string QueryString { get; }

    public long? ContentLength
    {
        get
        {
            if (_contentBoundaryType == BoundaryType.None)
            {
                // Note Http.Sys adds the Transfer-Encoding: chunked header to HTTP/2 requests with bodies for back compat.
                var transferEncoding = Headers[HeaderNames.TransferEncoding].ToString();
                if (IsChunked(transferEncoding))
                {
                    _contentBoundaryType = BoundaryType.Chunked;
                }
                else
                {
                    string? length = Headers[HeaderNames.ContentLength];
                    if (length != null &&
                        long.TryParse(length.Trim(), NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat, out var value))
                    {
                        _contentBoundaryType = BoundaryType.ContentLength;
                        _contentLength = value;
                    }
                    else
                    {
                        _contentBoundaryType = BoundaryType.Invalid;
                    }
                }
            }

            return _contentLength;
        }
    }

    public RequestHeaders Headers { get; }

    internal HTTP_VERB KnownMethod { get; }

    internal bool IsHeadMethod => KnownMethod == HTTP_VERB.HttpVerbHEAD;

    public string Method { get; }

    public Stream Body => EnsureRequestStream() ?? Stream.Null;

    private RequestStream? EnsureRequestStream()
    {
        if (_nativeStream == null && HasEntityBody)
        {
            _nativeStream = new RequestStream(RequestContext);
        }
        return _nativeStream;
    }

    public bool HasRequestBodyStarted => _nativeStream?.HasStarted ?? false;

    public long? MaxRequestBodySize
    {
        get => EnsureRequestStream()?.MaxSize;
        set
        {
            EnsureRequestStream();
            if (_nativeStream != null)
            {
                _nativeStream.MaxSize = value;
            }
        }
    }

    public string PathBase { get; }

    public string Path { get; }

    public bool IsHttps => SslStatus != SslStatus.Insecure;

    public string RawUrl { get; }

    public Version ProtocolVersion { get; }

    public bool HasEntityBody
    {
        get
        {
            // accessing the ContentLength property delay creates _contentBoundaryType
            return (ContentLength.HasValue && ContentLength.Value > 0 && _contentBoundaryType == BoundaryType.ContentLength)
                || _contentBoundaryType == BoundaryType.Chunked;
        }
    }

    private AspNetCore.HttpSys.Internal.SocketAddress RemoteEndPoint
    {
        get
        {
            if (_remoteEndPoint == null)
            {
                _remoteEndPoint = RequestContext.GetRemoteEndPoint()!;
            }

            return _remoteEndPoint;
        }
    }

    private AspNetCore.HttpSys.Internal.SocketAddress LocalEndPoint
    {
        get
        {
            if (_localEndPoint == null)
            {
                _localEndPoint = RequestContext.GetLocalEndPoint()!;
            }

            return _localEndPoint;
        }
    }

    // TODO: Lazy cache?
    public IPAddress? RemoteIpAddress => RemoteEndPoint.GetIPAddress();

    public IPAddress? LocalIpAddress => LocalEndPoint.GetIPAddress();

    public int RemotePort => RemoteEndPoint.GetPort();

    public int LocalPort => LocalEndPoint.GetPort();

    public string Scheme => IsHttps ? Constants.HttpsScheme : Constants.HttpScheme;

    // HTTP.Sys allows you to upgrade anything to opaque unless content-length > 0 or chunked are specified.
    internal bool IsUpgradable => ProtocolVersion == HttpVersion.Version11 && !HasEntityBody && ComNetOS.IsWin8orLater;

    internal WindowsPrincipal User { get; }

    public string SniHostName { get; private set; }

    public SslProtocols Protocol { get; private set; }

    public CipherAlgorithmType CipherAlgorithm { get; private set; }

    public int CipherStrength { get; private set; }

    public HashAlgorithmType HashAlgorithm { get; private set; }

    public int HashStrength { get; private set; }

    public ExchangeAlgorithmType KeyExchangeAlgorithm { get; private set; }

    public int KeyExchangeStrength { get; private set; }

    private void GetTlsHandshakeResults()
    {
        var handshake = RequestContext.GetTlsHandshake();
        Protocol = (SslProtocols)handshake.Protocol;
        CipherAlgorithm = (CipherAlgorithmType)handshake.CipherType;
        CipherStrength = (int)handshake.CipherStrength;
        HashAlgorithm = (HashAlgorithmType)handshake.HashType;
        HashStrength = (int)handshake.HashStrength;
        KeyExchangeAlgorithm = (ExchangeAlgorithmType)handshake.KeyExchangeType;
        KeyExchangeStrength = (int)handshake.KeyExchangeStrength;

        var sni = RequestContext.GetClientSni();
        SniHostName = sni.Hostname.ToString();
    }

    public X509Certificate2? ClientCertificate
    {
        get
        {
            if (_clientCert == null && SslStatus == SslStatus.ClientCert)
            {
                try
                {
                    _clientCert = RequestContext.GetClientCertificate();
                }
                catch (CryptographicException ce)
                {
                    Log.ErrorInReadingCertificate(RequestContext.Logger, ce);
                }
                catch (SecurityException se)
                {
                    Log.ErrorInReadingCertificate(RequestContext.Logger, se);
                }
            }

            return _clientCert;
        }
    }

    public bool CanDelegate => !(HasRequestBodyStarted || RequestContext.Response.HasStarted);

    // Populates the client certificate.  The result may be null if there is no client cert.
    // TODO: Does it make sense for this to be invoked multiple times (e.g. renegotiate)? Client and server code appear to
    // enable this, but it's unclear what Http.Sys would do.
    public async Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        if (SslStatus == SslStatus.Insecure)
        {
            // Non-SSL
            return null;
        }
        // TODO: Verbose log
        if (_clientCert != null)
        {
            return _clientCert;
        }
        cancellationToken.ThrowIfCancellationRequested();

        var certLoader = new ClientCertLoader(RequestContext, cancellationToken);
        try
        {
            await certLoader.LoadClientCertificateAsync();
            // Populate the environment.
            if (certLoader.ClientCert != null)
            {
                _clientCert = certLoader.ClientCert;
            }
            // TODO: Expose errors and exceptions?
        }
        catch (Exception)
        {
            certLoader?.Dispose();
            throw;
        }
        return _clientCert;
    }
    /* TODO: https://github.com/aspnet/WebListener/issues/231
    private byte[] GetProvidedTokenBindingId()
    {
        return _providedTokenBindingId;
    }

    private byte[] GetReferredTokenBindingId()
    {
        return _referredTokenBindingId;
    }
    */
    // Only call from the constructor so we can directly access the native request blob.
    // This requires Windows 10 and the following reg key:
    // Set Key: HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\HTTP\Parameters to Value: EnableSslTokenBinding = 1 [DWORD]
    // Then for IE to work you need to set these:
    // Key: HKLM\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_ENABLE_TOKEN_BINDING
    // Value: "iexplore.exe"=dword:0x00000001
    // Key: HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_ENABLE_TOKEN_BINDING
    // Value: "iexplore.exe"=dword:00000001
    // TODO: https://github.com/aspnet/WebListener/issues/231
    // TODO: https://github.com/aspnet/WebListener/issues/204 Move to NativeRequestContext
    /*
    private unsafe void GetTlsTokenBindingInfo()
    {
        var nativeRequest = (HttpApi.HTTP_REQUEST_V2*)_nativeRequestContext.RequestBlob;
        for (int i = 0; i < nativeRequest->RequestInfoCount; i++)
        {
            var pThisInfo = &nativeRequest->pRequestInfo[i];
            if (pThisInfo->InfoType == HttpApi.HTTP_REQUEST_INFO_TYPE.HttpRequestInfoTypeSslTokenBinding)
            {
                var pTokenBindingInfo = (HttpApi.HTTP_REQUEST_TOKEN_BINDING_INFO*)pThisInfo->pInfo;
                _providedTokenBindingId = TokenBindingUtil.GetProvidedTokenIdFromBindingInfo(pTokenBindingInfo, out _referredTokenBindingId);
            }
        }
    }
    */
    internal uint GetChunks(ref int dataChunkIndex, ref uint dataChunkOffset, byte[] buffer, int offset, int size)
    {
        return RequestContext.GetChunks(ref dataChunkIndex, ref dataChunkOffset, buffer, offset, size);
    }

    // should only be called from RequestContext
    internal void Dispose()
    {
        if (!_isDisposed)
        {
            // TODO: Verbose log
            _isDisposed = true;
            RequestContext.Dispose();
            (User?.Identity as WindowsIdentity)?.Dispose();
            _nativeStream?.Dispose();
        }
    }

    internal void SwitchToOpaqueMode()
    {
        if (_nativeStream == null)
        {
            _nativeStream = new RequestStream(RequestContext);
        }
        _nativeStream.SwitchToOpaqueMode();
    }

    private static partial class Log
    {
        [LoggerMessage(LoggerEventIds.ErrorInReadingCertificate, LogLevel.Debug, "An error occurred reading the client certificate.", EventName = "ErrorInReadingCertificate")]
        public static partial void ErrorInReadingCertificate(ILogger logger, Exception exception);
    }

    private void RemoveContentLengthIfTransferEncodingContainsChunked()
    {
        if (StringValues.IsNullOrEmpty(Headers.ContentLength)) { return; }

        var transferEncoding = Headers[HeaderNames.TransferEncoding].ToString();
        if (!IsChunked(transferEncoding))
        {
            return;
        }

        // https://datatracker.ietf.org/doc/html/rfc7230#section-3.3.2
        // A sender MUST NOT send a Content-Length header field in any message
        // that contains a Transfer-Encoding header field.
        // https://datatracker.ietf.org/doc/html/rfc7230#section-3.3.3
        // If a message is received with both a Transfer-Encoding and a
        // Content-Length header field, the Transfer-Encoding overrides the
        // Content-Length.  Such a message might indicate an attempt to
        // perform request smuggling (Section 9.5) or response splitting
        // (Section 9.4) and ought to be handled as an error.  A sender MUST
        // remove the received Content-Length field prior to forwarding such
        // a message downstream.
        // We should remove the Content-Length request header in this case, for compatibility
        // reasons, include X-Content-Length so that the original Content-Length is still available.
        IHeaderDictionary headerDictionary = Headers;
        headerDictionary.Add("X-Content-Length", headerDictionary[HeaderNames.ContentLength]);
        Headers.ContentLength = StringValues.Empty;
    }

    private static bool IsChunked(string? transferEncoding)
    {
        if (transferEncoding is null)
        {
            return false;
        }

        var index = transferEncoding.LastIndexOf(',');
        if (transferEncoding.AsSpan().Slice(index + 1).Trim().Equals("chunked", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return false;
    }
}
