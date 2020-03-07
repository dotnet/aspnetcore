// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    internal partial class HttpProtocol : IHttpRequestFeature,
                                          IHttpResponseFeature,
                                          IHttpResponseBodyFeature,
                                          IRequestBodyPipeFeature,
                                          IHttpUpgradeFeature,
                                          IHttpConnectionFeature,
                                          IHttpRequestLifetimeFeature,
                                          IHttpRequestIdentifierFeature,
                                          IHttpRequestTrailersFeature,
                                          IHttpBodyControlFeature,
                                          IHttpMaxRequestBodySizeFeature,
                                          IEndpointFeature,
                                          IRouteValuesFeature
    {
        // NOTE: When feature interfaces are added to or removed from this HttpProtocol class implementation,
        // then the list of `implementedFeatures` in the generated code project MUST also be updated.
        // See also: tools/CodeGenerator/HttpProtocolFeatureCollection.cs

        string IHttpRequestFeature.Protocol
        {
            get => _httpProtocol ??= HttpVersion;
            set => _httpProtocol = value;
        }

        string IHttpRequestFeature.Scheme
        {
            get => Scheme ?? "http";
            set => Scheme = value;
        }

        string IHttpRequestFeature.Method
        {
            get
            {
                if (_methodText != null)
                {
                    return _methodText;
                }

                _methodText = HttpUtilities.MethodToString(Method) ?? string.Empty;
                return _methodText;
            }
            set
            {
                _methodText = value;
            }
        }

        string IHttpRequestFeature.PathBase
        {
            get => PathBase ?? "";
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

        PipeReader IRequestBodyPipeFeature.Reader
        {
            get
            {
                if (!ReferenceEquals(_requestStreamInternal, RequestBody))
                {
                    _requestStreamInternal = RequestBody;
                    RequestBodyPipeReader = PipeReader.Create(RequestBody, new StreamPipeReaderOptions(_context.MemoryPool, _context.MemoryPool.GetMinimumSegmentSize(), _context.MemoryPool.GetMinimumAllocSize()));

                    OnCompleted((self) =>
                    {
                        ((PipeReader)self).Complete();
                        return Task.CompletedTask;
                    }, RequestBodyPipeReader);
                }

                return RequestBodyPipeReader;
            }
        }

        bool IHttpRequestTrailersFeature.Available => RequestTrailersAvailable;

        IHeaderDictionary IHttpRequestTrailersFeature.Trailers
        {
            get
            {
                if (!RequestTrailersAvailable)
                {
                    throw new InvalidOperationException(CoreStrings.RequestTrailersNotAvailable);
                }
                return RequestTrailers;
            }
        }

        int IHttpResponseFeature.StatusCode
        {
            get => StatusCode;
            set => StatusCode = value;
        }

        string IHttpResponseFeature.ReasonPhrase
        {
            get => ReasonPhrase;
            set => ReasonPhrase = value;
        }

        IHeaderDictionary IHttpResponseFeature.Headers
        {
            get => ResponseHeaders;
            set => ResponseHeaders = value;
        }

        CancellationToken IHttpRequestLifetimeFeature.RequestAborted
        {
            get => RequestAborted;
            set => RequestAborted = value;
        }

        bool IHttpResponseFeature.HasStarted => HasResponseStarted;

        bool IHttpUpgradeFeature.IsUpgradableRequest => IsUpgradableRequest;

        IPAddress IHttpConnectionFeature.RemoteIpAddress
        {
            get => RemoteIpAddress;
            set => RemoteIpAddress = value;
        }

        IPAddress IHttpConnectionFeature.LocalIpAddress
        {
            get => LocalIpAddress;
            set => LocalIpAddress = value;
        }

        int IHttpConnectionFeature.RemotePort
        {
            get => RemotePort;
            set => RemotePort = value;
        }

        int IHttpConnectionFeature.LocalPort
        {
            get => LocalPort;
            set => LocalPort = value;
        }

        string IHttpConnectionFeature.ConnectionId
        {
            get => ConnectionIdFeature;
            set => ConnectionIdFeature = value;
        }

        string IHttpRequestIdentifierFeature.TraceIdentifier
        {
            get => TraceIdentifier;
            set => TraceIdentifier = value;
        }

        bool IHttpBodyControlFeature.AllowSynchronousIO
        {
            get => AllowSynchronousIO;
            set => AllowSynchronousIO = value;
        }

        bool IHttpMaxRequestBodySizeFeature.IsReadOnly => HasStartedConsumingRequestBody || IsUpgraded;

        long? IHttpMaxRequestBodySizeFeature.MaxRequestBodySize
        {
            get => MaxRequestBodySize;
            set
            {
                if (HasStartedConsumingRequestBody)
                {
                    throw new InvalidOperationException(CoreStrings.MaxRequestBodySizeCannotBeModifiedAfterRead);
                }
                if (IsUpgraded)
                {
                    throw new InvalidOperationException(CoreStrings.MaxRequestBodySizeCannotBeModifiedForUpgradedRequests);
                }
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), CoreStrings.NonNegativeNumberOrNullRequired);
                }

                MaxRequestBodySize = value;
            }
        }

        Stream IHttpResponseFeature.Body
        {
            get => ResponseBody;
            set => ResponseBody = value;
        }

        PipeWriter IHttpResponseBodyFeature.Writer => ResponseBodyPipeWriter;

        Endpoint IEndpointFeature.Endpoint
        {
            get => _endpoint;
            set => _endpoint = value;
        }

        RouteValueDictionary IRouteValuesFeature.RouteValues
        {
            get => _routeValues ??= new RouteValueDictionary();
            set => _routeValues = value;
        }

        Stream IHttpResponseBodyFeature.Stream => ResponseBody;

        protected void ResetHttp1Features()
        {
            _currentIHttpMinRequestBodyDataRateFeature = this;
            _currentIHttpMinResponseDataRateFeature = this;
        }

        protected void ResetHttp2Features()
        {
            _currentIHttp2StreamIdFeature = this;
            _currentIHttpResponseTrailersFeature = this;
            _currentIHttpResetFeature = this;
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
            if (!IsUpgradableRequest)
            {
                throw new InvalidOperationException(CoreStrings.CannotUpgradeNonUpgradableRequest);
            }

            if (IsUpgraded)
            {
                throw new InvalidOperationException(CoreStrings.UpgradeCannotBeCalledMultipleTimes);
            }

            if (!ServiceContext.ConnectionManager.UpgradedConnectionCount.TryLockOne())
            {
                throw new InvalidOperationException(CoreStrings.UpgradedConnectionLimitReached);
            }

            IsUpgraded = true;

            ConnectionFeatures.Get<IDecrementConcurrentConnectionCountFeature>()?.ReleaseConnection();

            StatusCode = StatusCodes.Status101SwitchingProtocols;
            ReasonPhrase = "Switching Protocols";
            ResponseHeaders[HeaderNames.Connection] = "Upgrade";

            await FlushAsync();

            return _bodyControl.Upgrade();
        }

        void IHttpRequestLifetimeFeature.Abort()
        {
            ApplicationAbort();
        }

        Task IHttpResponseBodyFeature.StartAsync(CancellationToken cancellationToken)
        {
            if (HasResponseStarted)
            {
                return Task.CompletedTask;
            }

            cancellationToken.ThrowIfCancellationRequested();

            return InitializeResponseAsync(0);
        }

        void IHttpResponseBodyFeature.DisableBuffering()
        {
        }

        Task IHttpResponseBodyFeature.SendFileAsync(string path, long offset, long? count, CancellationToken cancellation)
        {
            return SendFileFallback.SendFileAsync(ResponseBody, path, offset, count, cancellation);
        }

        Task IHttpResponseBodyFeature.CompleteAsync()
        {
            return CompleteAsync();
        }
    }
}
