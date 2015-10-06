// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Server.Kestrel.Http
{
    public partial class Frame : IFeatureCollection, IHttpRequestFeature, IHttpResponseFeature, IHttpUpgradeFeature
    {
        // NOTE: When feature interfaces are added to or removed from this Frame class implementation,
        // then the list of `implementedFeatures` in the generated code project MUST also be updated.
        // See also: tools/Microsoft.AspNet.Server.Kestrel.GeneratedCode/FrameFeatureCollection.cs

        private string _scheme;
        private string _pathBase;
        private int _featureRevision;

        private Dictionary<Type, object> Extra => MaybeExtra ?? Interlocked.CompareExchange(ref MaybeExtra, new Dictionary<Type, object>(), null);
        private Dictionary<Type, object> MaybeExtra;

        public void ResetFeatureCollection()
        {
            FastReset();
            MaybeExtra?.Clear();
            Interlocked.Increment(ref _featureRevision);
        }

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
                return _scheme ?? "http";
            }

            set
            {
                _scheme = value;
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
                return _pathBase ?? "";
            }

            set
            {
                _pathBase = value;
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

        IDictionary<string, StringValues> IHttpRequestFeature.Headers
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

        IDictionary<string, StringValues> IHttpResponseFeature.Headers
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

        object IFeatureCollection.this[Type key]
        {
            get { return FastFeatureGet(key); }
            set { FastFeatureSet(key, value); }
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
            ProduceStart();
            return Task.FromResult(DuplexStream);
        }

        IEnumerator<KeyValuePair<Type, object>> IEnumerable<KeyValuePair<Type, object>>.GetEnumerator() => FastEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => FastEnumerable().GetEnumerator();
    }
}
