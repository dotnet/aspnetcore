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
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.Kestrel.Http
{
    public partial class Frame : IFeatureCollection,
                                 IHttpRequestFeature,
                                 IHttpResponseFeature,
                                 IHttpUpgradeFeature,
                                 IHttpConnectionFeature,
                                 IHttpRequestLifetimeFeature
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

        public IFrameControl FrameControl { get; set; }

        string IHttpRequestFeature.Protocol
        {
            get
            {
                return HttpVersion;
            }

            set
            {
                HttpVersion = value;
            }
        }

        string IHttpRequestFeature.Scheme
        {
            get
            {
                return Scheme ?? "http";
            }

            set
            {
                Scheme = value;
            }
        }

        string IHttpRequestFeature.Method
        {
            get
            {
                return Method;
            }

            set
            {
                Method = value;
            }
        }

        string IHttpRequestFeature.PathBase
        {
            get
            {
                return PathBase ?? "";
            }

            set
            {
                PathBase = value;
            }
        }

        string IHttpRequestFeature.Path
        {
            get
            {
                return Path;
            }

            set
            {
                Path = value;
            }
        }

        string IHttpRequestFeature.QueryString
        {
            get
            {
                return QueryString;
            }

            set
            {
                QueryString = value;
            }
        }

        string IHttpRequestFeature.RawTarget
        {
            get
            {
                return RawTarget;
            }
            set
            {
                RawTarget = value;
            }
        }

        IHeaderDictionary IHttpRequestFeature.Headers
        {
            get
            {
                return RequestHeaders;
            }

            set
            {
                RequestHeaders = value;
            }
        }

        Stream IHttpRequestFeature.Body
        {
            get
            {
                return RequestBody;
            }

            set
            {
                RequestBody = value;
            }
        }

        int IHttpResponseFeature.StatusCode
        {
            get
            {
                return StatusCode;
            }

            set
            {
                StatusCode = value;
            }
        }

        string IHttpResponseFeature.ReasonPhrase
        {
            get
            {
                return ReasonPhrase;
            }

            set
            {
                ReasonPhrase = value;
            }
        }

        IHeaderDictionary IHttpResponseFeature.Headers
        {
            get
            {
                return ResponseHeaders;
            }

            set
            {
                ResponseHeaders = value;
            }
        }

        Stream IHttpResponseFeature.Body
        {
            get
            {
                return ResponseBody;
            }

            set
            {
                ResponseBody = value;
            }
        }

        CancellationToken IHttpRequestLifetimeFeature.RequestAborted
        {
            get
            {
                return RequestAborted;
            }
            set
            {
                RequestAborted = value;
            }
        }

        bool IHttpResponseFeature.HasStarted
        {
            get { return HasResponseStarted; }
        }

        bool IHttpUpgradeFeature.IsUpgradableRequest
        {
            get
            {
                StringValues values;
                if (RequestHeaders.TryGetValue("Connection", out values))
                {
                    return values.Any(value => value.IndexOf("upgrade", StringComparison.OrdinalIgnoreCase) != -1);
                }
                return false;
            }
        }

        bool IFeatureCollection.IsReadOnly => false;

        int IFeatureCollection.Revision => _featureRevision;

        IPAddress IHttpConnectionFeature.RemoteIpAddress
        {
            get { return RemoteIpAddress; }
            set { RemoteIpAddress = value; }
        }

        IPAddress IHttpConnectionFeature.LocalIpAddress
        {
            get { return LocalIpAddress; }
            set { LocalIpAddress = value; }
        }

        int IHttpConnectionFeature.RemotePort
        {
            get { return RemotePort; }
            set { RemotePort = value; }
        }

        int IHttpConnectionFeature.LocalPort
        {
            get { return LocalPort; }
            set { LocalPort = value; }
        }

        string IHttpConnectionFeature.ConnectionId
        {
            get { return ConnectionIdFeature; }
            set { ConnectionIdFeature = value; }
        }

        object IFeatureCollection.this[Type key]
        {
            get { return FastFeatureGet(key); }
            set { FastFeatureSet(key, value); }
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
            StatusCode = 101;
            ReasonPhrase = "Switching Protocols";
            ResponseHeaders["Connection"] = "Upgrade";
            if (!ResponseHeaders.ContainsKey("Upgrade"))
            {
                StringValues values;
                if (RequestHeaders.TryGetValue("Upgrade", out values))
                {
                    ResponseHeaders["Upgrade"] = values;
                }
            }

            await FlushAsync(default(CancellationToken));

            return DuplexStream;
        }

        IEnumerator<KeyValuePair<Type, object>> IEnumerable<KeyValuePair<Type, object>>.GetEnumerator() => FastEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => FastEnumerable().GetEnumerator();

        void IHttpRequestLifetimeFeature.Abort()
        {
            Abort();
        }
    }
}
