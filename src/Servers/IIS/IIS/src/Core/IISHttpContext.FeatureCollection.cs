// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.AspNetCore.Server.IIS.Core.IO;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal partial class IISHttpContext : IFeatureCollection,
                                        IHttpRequestFeature,
                                        IHttpRequestBodyDetectionFeature,
                                        IHttpResponseFeature,
                                        IHttpResponseBodyFeature,
                                        IHttpUpgradeFeature,
                                        IHttpRequestLifetimeFeature,
                                        IHttpAuthenticationFeature,
                                        IServerVariablesFeature,
                                        ITlsConnectionFeature,
                                        ITlsHandshakeFeature,
                                        IHttpBodyControlFeature,
                                        IHttpMaxRequestBodySizeFeature,
                                        IHttpResponseTrailersFeature,
                                        IHttpResetFeature,
                                        IConnectionLifetimeNotificationFeature,
                                        IHttpSysRequestInfoFeature,
                                        IHttpSysRequestTimingFeature
{
    private int _featureRevision;
    private string? _httpProtocolVersion;
    private X509Certificate2? _certificate;

    private List<KeyValuePair<Type, object>>? MaybeExtra;

    public void ResetFeatureCollection()
    {
        Initialize();
        MaybeExtra?.Clear();
        _featureRevision++;
    }

    private object? ExtraFeatureGet(Type key)
    {
        if (MaybeExtra == null)
        {
            return null;
        }
        for (var i = 0; i < MaybeExtra.Count; i++)
        {
            var kv = MaybeExtra[i];
            if (kv.Key == key)
            {
                return kv.Value;
            }
        }
        return null;
    }

    private void ExtraFeatureSet(Type key, object? value)
    {
        if (value == null)
        {
            if (MaybeExtra == null)
            {
                return;
            }
            for (var i = 0; i < MaybeExtra.Count; i++)
            {
                if (MaybeExtra[i].Key == key)
                {
                    MaybeExtra.RemoveAt(i);
                    return;
                }
            }
        }
        else
        {
            if (MaybeExtra == null)
            {
                MaybeExtra = new List<KeyValuePair<Type, object>>(2);
            }
            for (var i = 0; i < MaybeExtra.Count; i++)
            {
                if (MaybeExtra[i].Key == key)
                {
                    MaybeExtra[i] = new KeyValuePair<Type, object>(key, value);
                    return;
                }
            }
            MaybeExtra.Add(new KeyValuePair<Type, object>(key, value));
        }
    }

    string IHttpRequestFeature.Protocol
    {
        get => _httpProtocolVersion ??= HttpProtocol.GetHttpProtocol(HttpVersion);
        set => _httpProtocolVersion = value;
    }

    string IHttpRequestFeature.Scheme
    {
        get => Scheme;
        set => Scheme = value;
    }

    string IHttpRequestFeature.Method
    {
        get => Method;
        set => Method = value;
    }

    string IHttpRequestFeature.PathBase
    {
        get => PathBase;
        set => PathBase = value;
    }

    string IHttpRequestFeature.Path
    {
        get => Path;
        set => Path = value;
    }

    string IHttpRequestFeature.QueryString
    {
        get => QueryString;
        set => QueryString = value;
    }

    string IHttpRequestFeature.RawTarget
    {
        get => RawTarget;
        set => RawTarget = value;
    }

    IHeaderDictionary IHttpRequestFeature.Headers
    {
        get => RequestHeaders;
        set => RequestHeaders = value;
    }

    Stream IHttpRequestFeature.Body
    {
        get => RequestBody;
        set => RequestBody = value;
    }

    bool IHttpRequestBodyDetectionFeature.CanHaveBody => RequestCanHaveBody;

    int IHttpResponseFeature.StatusCode
    {
        get => StatusCode;
        set => StatusCode = value;
    }

    string? IHttpResponseFeature.ReasonPhrase
    {
        get => ReasonPhrase;
        set => ReasonPhrase = value;
    }

    IHeaderDictionary IHttpResponseFeature.Headers
    {
        get => ResponseHeaders;
        set => ResponseHeaders = value;
    }

    Stream IHttpResponseFeature.Body
    {
        get => ResponseBody;
        set => ResponseBody = value;
    }

    bool IHttpResponseFeature.HasStarted => HasResponseStarted;

    Stream IHttpResponseBodyFeature.Stream => ResponseBody;

    PipeWriter IHttpResponseBodyFeature.Writer
    {
        get
        {
            if (ResponsePipeWrapper == null)
            {
                ResponsePipeWrapper = PipeWriter.Create(ResponseBody, new StreamPipeWriterOptions(leaveOpen: true));
            }

            return ResponsePipeWrapper;
        }
    }

    Task IHttpResponseBodyFeature.StartAsync(CancellationToken cancellationToken)
    {
        if (!HasResponseStarted)
        {
            return InitializeResponse(flushHeaders: false);
        }

        return Task.CompletedTask;
    }

    Task IHttpResponseBodyFeature.SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
        => SendFileFallback.SendFileAsync(ResponseBody, path, offset, count, cancellation);

    // TODO: In the future this could complete the body all the way down to the server. For now it just ensures
    // any unflushed data gets flushed.
    Task IHttpResponseBodyFeature.CompleteAsync()
    {
        if (ResponsePipeWrapper != null)
        {
            var completeAsyncValueTask = ResponsePipeWrapper.CompleteAsync();
            if (!completeAsyncValueTask.IsCompletedSuccessfully)
            {
                return CompleteResponseBodyAwaited(completeAsyncValueTask);
            }
            completeAsyncValueTask.GetAwaiter().GetResult();
        }

        if (!HasResponseStarted)
        {
            var initializeTask = InitializeResponse(flushHeaders: false);
            if (!initializeTask.IsCompletedSuccessfully)
            {
                return CompleteInitializeResponseAwaited(initializeTask);
            }
        }

        // Completing the body output will trigger a final flush to IIS.
        // We'd rather not bypass the bodyoutput to flush, to guarantee we avoid
        // calling flush twice at the same time.
        // awaiting the writeBodyTask guarantees the response has finished the final flush.
        _bodyOutput.Complete();
        return _writeBodyTask!;
    }

    private async Task CompleteResponseBodyAwaited(ValueTask completeAsyncTask)
    {
        await completeAsyncTask;

        if (!HasResponseStarted)
        {
            await InitializeResponse(flushHeaders: false);
        }

        _bodyOutput.Complete();
        await _writeBodyTask!;
    }

    private async Task CompleteInitializeResponseAwaited(Task initializeTask)
    {
        await initializeTask;

        _bodyOutput.Complete();
        await _writeBodyTask!;
    }

    // Http/2 does not support the upgrade mechanic.
    // Http/1.1 upgrade requests may have a request body, but that's not allowed in our main scenario (WebSockets) and much
    // more complicated to support. See https://tools.ietf.org/html/rfc7230#section-6.7, https://tools.ietf.org/html/rfc7540#section-3.2
    bool IHttpUpgradeFeature.IsUpgradableRequest => !RequestCanHaveBody && HttpVersion == System.Net.HttpVersion.Version11;

    bool IFeatureCollection.IsReadOnly => false;

    int IFeatureCollection.Revision => _featureRevision;

    ClaimsPrincipal? IHttpAuthenticationFeature.User
    {
        get => User;
        set => User = value;
    }

    string? IServerVariablesFeature.this[string variableName]
    {
        get
        {
            ArgumentException.ThrowIfNullOrEmpty(variableName);

            // Synchronize access to native methods that might run in parallel with IO loops
            lock (_contextLock)
            {
                return NativeMethods.HttpTryGetServerVariable(_requestNativeHandle, variableName, out var value) ? value : null;
            }
        }
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(variableName);

            ArgumentNullException.ThrowIfNull(value);

            // Synchronize access to native methods that might run in parallel with IO loops
            lock (_contextLock)
            {
                NativeMethods.HttpSetServerVariable(_requestNativeHandle, variableName, value);
            }
        }
    }

    object? IFeatureCollection.this[Type key]
    {
        get => FastFeatureGet(key);
        set => FastFeatureSet(key, value);
    }

    TFeature? IFeatureCollection.Get<TFeature>() where TFeature : default
    {
        return (TFeature?)FastFeatureGet(typeof(TFeature));
    }

    void IFeatureCollection.Set<TFeature>(TFeature? instance) where TFeature : default
    {
        FastFeatureSet(typeof(TFeature), instance);
    }

    void IHttpResponseFeature.OnStarting(Func<object, Task> callback, object state)
    {
        OnStarting(callback, state);
    }

    void IHttpResponseFeature.OnCompleted(Func<object, Task> callback, object state)
    {
        OnCompleted(callback, state);
    }

    async Task<Stream> IHttpUpgradeFeature.UpgradeAsync()
    {
        if (!((IHttpUpgradeFeature)this).IsUpgradableRequest)
        {
            if (HttpVersion != System.Net.HttpVersion.Version11)
            {
                throw new InvalidOperationException(CoreStrings.UpgradeWithWrongProtocolVersion);
            }
            throw new InvalidOperationException(CoreStrings.CannotUpgradeNonUpgradableRequest);
        }

        if (_wasUpgraded)
        {
            throw new InvalidOperationException(CoreStrings.UpgradeCannotBeCalledMultipleTimes);
        }
        if (HasResponseStarted)
        {
            throw new InvalidOperationException(CoreStrings.UpgradeCannotBeCalledMultipleTimes);
        }

        MaxRequestBodySize = null;
        _wasUpgraded = true;

        StatusCode = StatusCodes.Status101SwitchingProtocols;
        ReasonPhrase = ReasonPhrases.GetReasonPhrase(StatusCodes.Status101SwitchingProtocols);

        // If we started reading before calling Upgrade Task should be completed at this point
        // because read would return 0 synchronously
        Debug.Assert(_readBodyTask == null || _readBodyTask.IsCompleted);

        // Reset reading status to allow restarting with new IO
        HasStartedConsumingRequestBody = false;

        // Upgrade async will cause the stream processing to go into duplex mode
        AsyncIO = new WebSocketsAsyncIOEngine(this, _requestNativeHandle);

        await InitializeResponse(flushHeaders: true);

        return _streams.Upgrade();
    }

    Task<X509Certificate2?> ITlsConnectionFeature.GetClientCertificateAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(((ITlsConnectionFeature)this).ClientCertificate);
    }

    unsafe X509Certificate2? ITlsConnectionFeature.ClientCertificate
    {
        get
        {
            if (_certificate == null &&
                NativeRequest->pSslInfo != null &&
                NativeRequest->pSslInfo->pClientCertInfo != null &&
                NativeRequest->pSslInfo->pClientCertInfo->pCertEncoded != null &&
                NativeRequest->pSslInfo->pClientCertInfo->CertEncodedSize != 0)
            {
                // Based off of from https://referencesource.microsoft.com/#system/net/System/Net/HttpListenerRequest.cs,1037c8ec82879ba0,references
                var rawCertificateCopy = new byte[NativeRequest->pSslInfo->pClientCertInfo->CertEncodedSize];
                Marshal.Copy((IntPtr)NativeRequest->pSslInfo->pClientCertInfo->pCertEncoded, rawCertificateCopy, 0, rawCertificateCopy.Length);
                _certificate = new X509Certificate2(rawCertificateCopy);
            }

            return _certificate;
        }
        set
        {
            _certificate = value;
        }
    }

    SslProtocols ITlsHandshakeFeature.Protocol => Protocol;

    TlsCipherSuite? ITlsHandshakeFeature.NegotiatedCipherSuite => NegotiatedCipherSuite;

    string ITlsHandshakeFeature.HostName => SniHostName;

    CipherAlgorithmType ITlsHandshakeFeature.CipherAlgorithm => CipherAlgorithm;

    int ITlsHandshakeFeature.CipherStrength => CipherStrength;

    HashAlgorithmType ITlsHandshakeFeature.HashAlgorithm => HashAlgorithm;

    int ITlsHandshakeFeature.HashStrength => HashStrength;

    ExchangeAlgorithmType ITlsHandshakeFeature.KeyExchangeAlgorithm => KeyExchangeAlgorithm;

    int ITlsHandshakeFeature.KeyExchangeStrength => KeyExchangeStrength;

    IEnumerator<KeyValuePair<Type, object>> IEnumerable<KeyValuePair<Type, object>>.GetEnumerator() => FastEnumerable().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => FastEnumerable().GetEnumerator();

    bool IHttpBodyControlFeature.AllowSynchronousIO { get; set; }

    bool IHttpMaxRequestBodySizeFeature.IsReadOnly => HasStartedConsumingRequestBody || _wasUpgraded;

    long? IHttpMaxRequestBodySizeFeature.MaxRequestBodySize
    {
        get => MaxRequestBodySize;
        set
        {
            if (HasStartedConsumingRequestBody)
            {
                throw new InvalidOperationException(CoreStrings.MaxRequestBodySizeCannotBeModifiedAfterRead);
            }
            if (_wasUpgraded)
            {
                throw new InvalidOperationException(CoreStrings.MaxRequestBodySizeCannotBeModifiedForUpgradedRequests);
            }
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.NonNegativeNumberOrNullRequired);
            }

            if (value > _options.IisMaxRequestSizeLimit)
            {
                _logger.LogWarning(CoreStrings.MaxRequestLimitWarning);
            }

            MaxRequestBodySize = value;
        }
    }

    internal IHttpResponseTrailersFeature? GetResponseTrailersFeature()
    {
        return AdvancedHttp2FeaturesSupported() ? this : null;
    }

    internal ITlsHandshakeFeature? GetTlsHandshakeFeature()
    {
        return IsHttps ? this : null;
    }

    IHeaderDictionary IHttpResponseTrailersFeature.Trailers
    {
        get => ResponseTrailers ??= HttpResponseTrailers;
        set => ResponseTrailers = value;
    }

    CancellationToken IConnectionLifetimeNotificationFeature.ConnectionClosedRequested { get; set; }

    internal IHttpResetFeature? GetResetFeature()
    {
        return AdvancedHttp2FeaturesSupported() ? this : null;
    }

    void IHttpResetFeature.Reset(int errorCode)
    {
        if (errorCode < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(errorCode), "'errorCode' cannot be negative");
        }

        SetResetCode(errorCode);
        AbortIO(clientDisconnect: false);
    }

    internal unsafe void SetResetCode(int errorCode)
    {
        NativeMethods.HttpResetStream(_requestNativeHandle, (ulong)errorCode);
    }

    void IHttpResponseBodyFeature.DisableBuffering()
    {
        NativeMethods.HttpDisableBuffering(_requestNativeHandle);
        DisableCompression();
    }

    private void DisableCompression()
    {
        var serverVariableFeature = (IServerVariablesFeature)this;
        serverVariableFeature["IIS_EnableDynamicCompression"] = "0";
    }

    void IConnectionLifetimeNotificationFeature.RequestClose()
    {
        // Set the connection close feature if the response hasn't sent headers as yet
        if (!HasResponseStarted)
        {
            ResponseHeaders.Connection = ConnectionClose;
        }
    }
}
