// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Kestrel.Http;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Server.Kestrel
{
    public class ServerRequest : IHttpRequestFeature, IHttpResponseFeature, IHttpUpgradeFeature
    {
        private Frame _frame;
        private string _scheme;
        private string _pathBase;
        private FeatureCollection _features;

        public ServerRequest(Frame frame)
        {
            _frame = frame;
            _features = new FeatureCollection();
            PopulateFeatures();
        }

        internal IFeatureCollection Features
        {
            get { return _features; }
        }

        string IHttpRequestFeature.Protocol
        {
            get
            {
                return _frame.HttpVersion;
            }

            set
            {
                _frame.HttpVersion = value;
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
                return _frame.Method;
            }

            set
            {
                _frame.Method = value;
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
                return _frame.Path;
            }

            set
            {
                _frame.Path = value;
            }
        }

        string IHttpRequestFeature.QueryString
        {
            get
            {
                return _frame.QueryString;
            }

            set
            {
                _frame.QueryString = value;
            }
        }

        IDictionary<string, StringValues> IHttpRequestFeature.Headers
        {
            get
            {
                return _frame.RequestHeaders;
            }

            set
            {
                _frame.RequestHeaders = value;
            }
        }

        Stream IHttpRequestFeature.Body
        {
            get
            {
                return _frame.RequestBody;
            }

            set
            {
                _frame.RequestBody = value;
            }
        }

        int IHttpResponseFeature.StatusCode
        {
            get
            {
                return _frame.StatusCode;
            }

            set
            {
                _frame.StatusCode = value;
            }
        }

        string IHttpResponseFeature.ReasonPhrase
        {
            get
            {
                return _frame.ReasonPhrase;
            }

            set
            {
                _frame.ReasonPhrase = value;
            }
        }

        IDictionary<string, StringValues> IHttpResponseFeature.Headers
        {
            get
            {
                return _frame.ResponseHeaders;
            }

            set
            {
                _frame.ResponseHeaders = value;
            }
        }

        Stream IHttpResponseFeature.Body
        {
            get
            {
                return _frame.ResponseBody;
            }

            set
            {
                _frame.ResponseBody = value;
            }
        }

        bool IHttpResponseFeature.HasStarted
        {
            get { return _frame.HasResponseStarted; }
        }

        bool IHttpUpgradeFeature.IsUpgradableRequest
        {
            get
            {
                StringValues values;
                if (_frame.RequestHeaders.TryGetValue("Connection", out values))
                {
                    return values.Any(value => value.IndexOf("upgrade", StringComparison.OrdinalIgnoreCase) != -1);
                }
                return false;
            }
        }

        private void PopulateFeatures()
        {
            _features[typeof(IHttpRequestFeature)] = this;
            _features[typeof(IHttpResponseFeature)] = this;
            _features[typeof(IHttpUpgradeFeature)] = this;
        }

        void IHttpResponseFeature.OnStarting(Func<object, Task> callback, object state)
        {
            _frame.OnStarting(callback, state);
        }

        void IHttpResponseFeature.OnCompleted(Func<object, Task> callback, object state)
        {
            _frame.OnCompleted(callback, state);
        }

        Task<Stream> IHttpUpgradeFeature.UpgradeAsync()
        {
            _frame.StatusCode = 101;
            _frame.ReasonPhrase = "Switching Protocols";
            _frame.ResponseHeaders["Connection"] = "Upgrade";
            if (!_frame.ResponseHeaders.ContainsKey("Upgrade"))
            {
                StringValues values;
                if (_frame.RequestHeaders.TryGetValue("Upgrade", out values))
                {
                    _frame.ResponseHeaders["Upgrade"] = values;
                }
            }
            _frame.ProduceStart();
            return Task.FromResult(_frame.DuplexStream);
        }
    }
}
