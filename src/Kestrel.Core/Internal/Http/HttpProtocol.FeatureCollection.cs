// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public partial class HttpProtocol : IHttpRequestFeature,
                                        IHttpResponseFeature,
                                        IHttpConnectionFeature,
                                        IHttpRequestLifetimeFeature,
                                        IHttpRequestIdentifierFeature,
                                        IHttpBodyControlFeature,
                                        IHttpMaxRequestBodySizeFeature,
                                        IHttpMinRequestBodyDataRateFeature,
                                        IHttpMinResponseDataRateFeature
    {
        // NOTE: When feature interfaces are added to or removed from this HttpProtocol class implementation,
        // then the list of `implementedFeatures` in the generated code project MUST also be updated.
        // See also: tools/CodeGenerator/HttpProtocolFeatureCollection.cs

        string IHttpRequestFeature.Protocol
        {
            get => HttpVersion;
            set => HttpVersion = value;
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

        Stream IHttpResponseFeature.Body
        {
            get => ResponseBody;
            set => ResponseBody = value;
        }

        CancellationToken IHttpRequestLifetimeFeature.RequestAborted
        {
            get => RequestAborted;
            set => RequestAborted = value;
        }

        bool IHttpResponseFeature.HasStarted => HasResponseStarted;


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

        MinDataRate IHttpMinRequestBodyDataRateFeature.MinDataRate
        {
            get => MinRequestBodyDataRate;
            set => MinRequestBodyDataRate = value;
        }

        MinDataRate IHttpMinResponseDataRateFeature.MinDataRate
        {
            get => MinResponseDataRate;
            set => MinResponseDataRate = value;
        }

        protected void ResetIHttpUpgradeFeature()
        {
            _currentIHttpUpgradeFeature = this;
        }

        protected void ResetIHttp2StreamIdFeature()
        {
            _currentIHttp2StreamIdFeature = this;
        }

        void IHttpResponseFeature.OnStarting(Func<object, Task> callback, object state)
        {
            OnStarting(callback, state);
        }

        void IHttpResponseFeature.OnCompleted(Func<object, Task> callback, object state)
        {
            OnCompleted(callback, state);
        }

        void IHttpRequestLifetimeFeature.Abort()
        {
            ApplicationAbort();
        }

        protected virtual void ApplicationAbort()
        {
            Log.ApplicationAbortedConnection(ConnectionId, TraceIdentifier);
            Abort(new ConnectionAbortedException(CoreStrings.ConnectionAbortedByApplication));
        }
    }
}
