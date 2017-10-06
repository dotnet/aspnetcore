// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal partial class HttpProtocol : IFeatureCollection,
                                          IHttpRequestFeature,
                                          IHttpResponseFeature,
                                          IHttpUpgradeFeature,
                                          IHttpConnectionFeature,
                                          IHttpRequestLifetimeFeature,
                                          IHttpRequestIdentifierFeature
    {
        // NOTE: When feature interfaces are added to or removed from this HttpProtocol implementation,
        // then the list of `implementedFeatures` in the generated code project MUST also be updated.

        private int _featureRevision;
        private string _httpProtocolVersion = null;

        private List<KeyValuePair<Type, object>> MaybeExtra;
        public void ResetFeatureCollection()
        {
            Initialize();
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
            get
            {
                if (_httpProtocolVersion == null)
                {
                    var protocol = HttpVersion;
                    if (protocol.Major == 1 && protocol.Minor == 1)
                    {
                        _httpProtocolVersion = "HTTP/1.1";
                    }
                    else if (protocol.Major == 1 && protocol.Minor == 0)
                    {
                        _httpProtocolVersion = "HTTP/1.0";
                    }
                    else
                    {
                        _httpProtocolVersion = "HTTP/" + protocol.ToString(2);
                    }
                }
                return _httpProtocolVersion;
            }
            set
            {
                _httpProtocolVersion = value;
            }
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

        bool IHttpUpgradeFeature.IsUpgradableRequest => UpgradeAvailable;

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
            get => RequestConnectionId;
            set => RequestConnectionId = value;
        }

        string IHttpRequestIdentifierFeature.TraceIdentifier
        {
            get => TraceIdentifier;
            set => TraceIdentifier = value;
        }

        object IFeatureCollection.this[Type key]
        {
            get => FastFeatureGet(key);
            set => FastFeatureSet(key, value);
        }

        TFeature IFeatureCollection.Get<TFeature>()
        {
            return (TFeature)FastFeatureGet(typeof(TFeature));
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

        async Task<Stream> IHttpUpgradeFeature.UpgradeAsync()
        {
            // TODO fix these exceptions strings
            if (!((IHttpUpgradeFeature)this).IsUpgradableRequest)
            {
                throw new InvalidOperationException("CoreStrings.CannotUpgradeNonUpgradableRequest");
            }

            if (_wasUpgraded)
            {
                throw new InvalidOperationException("CoreStrings.UpgradeCannotBeCalledMultipleTimes");
            }
            if (HasResponseStarted)
            {
                throw new InvalidOperationException("CoreStrings.UpgradeCannotBeCalledMultipleTimes");
            }

            _wasUpgraded = true;

            StatusCode = StatusCodes.Status101SwitchingProtocols;
            ReasonPhrase = ReasonPhrases.GetReasonPhrase(StatusCodes.Status101SwitchingProtocols);
            await UpgradeAsync();

            return new DuplexStream(RequestBody, ResponseBody);
        }

        IEnumerator<KeyValuePair<Type, object>> IEnumerable<KeyValuePair<Type, object>>.GetEnumerator() => FastEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => FastEnumerable().GetEnumerator();

        void IHttpRequestLifetimeFeature.Abort()
        {
            Abort();
        }
    }
}
