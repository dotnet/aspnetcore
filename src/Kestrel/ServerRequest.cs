// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.FeatureModel;
using Microsoft.AspNet.Http.Features;
using Microsoft.AspNet.Server.Kestrel.Http;

namespace Kestrel
{
    public class ServerRequest : IHttpRequestFeature, IHttpResponseFeature, IHttpUpgradeFeature
    {
        Frame _frame;
        string _scheme;
        string _pathBase;
        private FeatureCollection _features;

        public ServerRequest(Frame frame)
        {
            _frame = frame;
            _features = new FeatureCollection();
            PopulateFeatures();
        }

        private void PopulateFeatures()
        {
            _features.Add(typeof(IHttpRequestFeature), this);
            _features.Add(typeof(IHttpResponseFeature), this);
            _features.Add(typeof(IHttpUpgradeFeature), this);
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

        IDictionary<string, string[]> IHttpRequestFeature.Headers
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

        IDictionary<string, string[]> IHttpResponseFeature.Headers
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

        void IHttpResponseFeature.OnStarting(Func<object, Task> callback, object state)
        {
            _frame.OnStarting(callback, state);
        }

        void IHttpResponseFeature.OnCompleted(Func<object, Task> callback, object state)
        {
            _frame.OnCompleted(callback, state);
        }

        bool IHttpUpgradeFeature.IsUpgradableRequest
        {
            get
            {
                string[] values;
                if (_frame.RequestHeaders.TryGetValue("Connection", out values))
                {
                    return values.Any(value => value.IndexOf("upgrade", StringComparison.OrdinalIgnoreCase) != -1);
                }
                return false;
            }
        }

        async Task<Stream> IHttpUpgradeFeature.UpgradeAsync()
        {
            _frame.StatusCode = 101;
            _frame.ReasonPhrase = "Switching Protocols";
            _frame.ResponseHeaders["Connection"] = new string[] { "Upgrade" };
            if (!_frame.ResponseHeaders.ContainsKey("Upgrade"))
            {
                string[] values;
                if (_frame.RequestHeaders.TryGetValue("Upgrade", out values))
                {
                    _frame.ResponseHeaders["Upgrade"] = values;
                }
            }
            _frame.ProduceStart();
            return _frame.DuplexStream;
        }
    }
}
