// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2
{
    public partial class Http2Stream : IFeatureCollection,
                                       IHttpRequestFeature,
                                       IHttpResponseFeature,
                                       IHttpUpgradeFeature,
                                       IHttpConnectionFeature,
                                       IHttpRequestLifetimeFeature,
                                       IHttpRequestIdentifierFeature,
                                       IHttpBodyControlFeature,
                                       IHttpMaxRequestBodySizeFeature,
                                       IHttpMinRequestBodyDataRateFeature,
                                       IHttpMinResponseDataRateFeature,
                                       IHttp2StreamIdFeature
    {
        // NOTE: When feature interfaces are added to or removed from this Frame class implementation,
        // then the list of `implementedFeatures` in the generated code project MUST also be updated.
        // See also: tools/Microsoft.AspNetCore.Server.Kestrel.GeneratedCode/FrameFeatureCollection.cs

        private int _featureRevision;

        private List<KeyValuePair<Type, object>> MaybeExtra;

        public void ResetFeatureCollection()
        {
            FastReset();
            MaybeExtra?.Clear();
            _featureRevision++;
        }

        private object ExtraFeatureGet(Type key)
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

        private void ExtraFeatureSet(Type key, object value)
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

        string IHttpRequestFeature.Protocol
        {
            get => HttpVersion;
            set => throw new InvalidOperationException();
        }

        string IHttpRequestFeature.Scheme
        {
            get => Scheme ?? "http";
            set => Scheme = value;
        }

        string IHttpRequestFeature.Method
        {
            get => Method;
            set => Method = value;
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

        bool IHttpUpgradeFeature.IsUpgradableRequest => false;

        bool IFeatureCollection.IsReadOnly => false;

        int IFeatureCollection.Revision => _featureRevision;

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

        bool IHttpMaxRequestBodySizeFeature.IsReadOnly => HasStartedConsumingRequestBody;

        long? IHttpMaxRequestBodySizeFeature.MaxRequestBodySize
        {
            get => MaxRequestBodySize;
            set
            {
                if (HasStartedConsumingRequestBody)
                {
                    throw new InvalidOperationException(CoreStrings.MaxRequestBodySizeCannotBeModifiedAfterRead);
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
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        MinDataRate IHttpMinResponseDataRateFeature.MinDataRate
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        object IFeatureCollection.this[Type key]
        {
            get => FastFeatureGet(key) ?? ConnectionFeatures?[key];
            set => FastFeatureSet(key, value);
        }

        TFeature IFeatureCollection.Get<TFeature>()
        {
            return (TFeature)(FastFeatureGet(typeof(TFeature)) ?? ConnectionFeatures?[typeof(TFeature)]);
        }

        void IFeatureCollection.Set<TFeature>(TFeature instance)
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

        Task<Stream> IHttpUpgradeFeature.UpgradeAsync()
        {
            throw new NotImplementedException();
        }

        IEnumerator<KeyValuePair<Type, object>> IEnumerable<KeyValuePair<Type, object>>.GetEnumerator() => FastEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => FastEnumerable().GetEnumerator();

        void IHttpRequestLifetimeFeature.Abort()
        {
            Abort(error: null);
        }

        int IHttp2StreamIdFeature.StreamId => StreamId;
    }
}
