// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.HttpFeature;

namespace Microsoft.AspNet.Owin
{
    using SendFileFunc = Func<string, long, long?, CancellationToken, Task>;

    public class OwinEnvironment : IDictionary<string, object>
    {
        private HttpContext _context;
        private IDictionary<string, FeatureMap> _entries;

        public OwinEnvironment(HttpContext context)
        {
            _context = context;
            _entries = new Dictionary<string, FeatureMap>()
            {
                { OwinConstants.RequestProtocol, new FeatureMap<IHttpRequestFeature>(feature => feature.Protocol, (feature, value) => feature.Protocol = Convert.ToString(value)) },
                { OwinConstants.RequestScheme, new FeatureMap<IHttpRequestFeature>(feature => feature.Scheme, (feature, value) => feature.Scheme = Convert.ToString(value)) },
                { OwinConstants.RequestMethod, new FeatureMap<IHttpRequestFeature>(feature => feature.Method, (feature, value) => feature.Method = Convert.ToString(value)) },
                { OwinConstants.RequestPathBase, new FeatureMap<IHttpRequestFeature>(feature => feature.PathBase, (feature, value) => feature.PathBase = Convert.ToString(value)) },
                { OwinConstants.RequestPath, new FeatureMap<IHttpRequestFeature>(feature => feature.Path, (feature, value) => feature.Path = Convert.ToString(value)) },
                { OwinConstants.RequestQueryString, new FeatureMap<IHttpRequestFeature>(feature => feature.QueryString, (feature, value) => feature.QueryString = Convert.ToString(value)) },
                { OwinConstants.RequestHeaders, new FeatureMap<IHttpRequestFeature>(feature => feature.Headers, (feature, value) => feature.Headers = (IDictionary<string, string[]>)value) },
                { OwinConstants.RequestBody, new FeatureMap<IHttpRequestFeature>(feature => feature.Body, (feature, value) => feature.Body = (Stream)value) },

                { OwinConstants.ResponseStatusCode, new FeatureMap<IHttpResponseFeature>(feature => feature.StatusCode, (feature, value) => feature.StatusCode = Convert.ToInt32(value)) },
                { OwinConstants.ResponseReasonPhrase, new FeatureMap<IHttpResponseFeature>(feature => feature.ReasonPhrase, (feature, value) => feature.ReasonPhrase = Convert.ToString(value)) },
                { OwinConstants.ResponseHeaders, new FeatureMap<IHttpResponseFeature>(feature => feature.Headers, (feature, value) => feature.Headers = (IDictionary<string, string[]>)value) },
                { OwinConstants.ResponseBody, new FeatureMap<IHttpResponseFeature>(feature => feature.Body, (feature, value) => feature.Body = (Stream)value) },
                { OwinConstants.CommonKeys.OnSendingHeaders, new FeatureMap<IHttpResponseFeature>(feature => new Action<Action<object>, object>(feature.OnSendingHeaders)) },

                { OwinConstants.CommonKeys.LocalPort, new FeatureMap<IHttpConnectionFeature>(feature => feature.LocalPort.ToString(CultureInfo.InvariantCulture),
                    (feature, value) => feature.LocalPort = Convert.ToInt32(value, CultureInfo.InvariantCulture)) },
                { OwinConstants.CommonKeys.RemotePort, new FeatureMap<IHttpConnectionFeature>(feature => feature.RemotePort.ToString(CultureInfo.InvariantCulture),
                    (feature, value) => feature.RemotePort = Convert.ToInt32(value, CultureInfo.InvariantCulture)) },

                { OwinConstants.CommonKeys.LocalIpAddress, new FeatureMap<IHttpConnectionFeature>(feature => feature.LocalIpAddress.ToString(),
                    (feature, value) => feature.LocalIpAddress = IPAddress.Parse(Convert.ToString(value))) },
                { OwinConstants.CommonKeys.RemoteIpAddress, new FeatureMap<IHttpConnectionFeature>(feature => feature.RemoteIpAddress.ToString(),
                    (feature, value) => feature.RemoteIpAddress = IPAddress.Parse(Convert.ToString(value))) },

                { OwinConstants.CommonKeys.IsLocal, new FeatureMap<IHttpConnectionFeature>(feature => feature.IsLocal, (feature, value) => feature.IsLocal = Convert.ToBoolean(value)) },

                { OwinConstants.SendFiles.SendAsync, new FeatureMap<IHttpSendFileFeature>(feature => new SendFileFunc(feature.SendFileAsync)) },
            };

            if (context.Request.IsSecure)
            {
                _entries.Add(OwinConstants.CommonKeys.ClientCertificate, new FeatureMap<IHttpTransportLayerSecurityFeature>(feature => feature.ClientCertificate,
                    (feature, value) => feature.ClientCertificate = (X509Certificate)value));
                _entries.Add(OwinConstants.CommonKeys.LoadClientCertAsync, new FeatureMap<IHttpTransportLayerSecurityFeature>(feature => new Func<Task>(feature.LoadAsync)));
            }

            _context.Items[typeof(HttpContext).FullName] = _context; // Store for lookup when we transition back out of OWIN
        }

        // Public in case there's a new/custom feature interface that needs to be added.
        public IDictionary<string, FeatureMap> FeatureMaps
        {
            get { return _entries; }
        }

        void IDictionary<string, object>.Add(string key, object value)
        {
            if (_entries.ContainsKey(key))
            {
                throw new InvalidOperationException("Key already present");
            }
            _context.Items.Add(key, value);
        }

        bool IDictionary<string, object>.ContainsKey(string key)
        {
            return _entries.ContainsKey(key) || _context.Items.ContainsKey(key);
        }

        ICollection<string> IDictionary<string, object>.Keys
        {
            get
            {
                return _entries.Keys.Concat(_context.Items.Keys.Select(key => Convert.ToString(key))).ToList();
            }
        }

        bool IDictionary<string, object>.Remove(string key)
        {
            if (_entries.Remove(key))
            {
                return true;
            }
            return _context.Items.Remove(key);
        }

        bool IDictionary<string, object>.TryGetValue(string key, out object value)
        {
            FeatureMap entry;
            if (_entries.TryGetValue(key, out entry))
            {
                value = entry.Get(_context);
                return true;
            }
            return _context.Items.TryGetValue(key, out value);
        }

        ICollection<object> IDictionary<string, object>.Values
        {
            get { throw new NotImplementedException(); }
        }

        object IDictionary<string, object>.this[string key]
        {
            get
            {
                FeatureMap entry;
                if (_entries.TryGetValue(key, out entry))
                {
                    return entry.Get(_context);
                }
                object value;
                if (_context.Items.TryGetValue(key, out value))
                {
                    return value;
                }
                throw new KeyNotFoundException(key);
            }
            set
            {
                FeatureMap entry;
                if (_entries.TryGetValue(key, out entry))
                {
                    if (entry.Setter == null)
                    {
                        _entries.Remove(key);
                        if (value != null)
                        {
                            _context.Items[key] = value;
                        }
                    }
                    else
                    {
                        entry.Setter(_context.GetFeature(entry.FeatureInterface), value);
                    }
                }
                else
                {
                    if (value == null)
                    {
                        _context.Items.Remove(key);
                    }
                    else
                    {
                        _context.Items[key] = value;
                    }
                }
            }
        }

        void ICollection<KeyValuePair<string, object>>.Add(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<string, object>>.Clear()
        {
            _entries.Clear();
            _context.Items.Clear();
        }

        bool ICollection<KeyValuePair<string, object>>.Contains(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<string, object>>.CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        int ICollection<KeyValuePair<string, object>>.Count
        {
            get { return _entries.Count + _context.Items.Count; }
        }

        bool ICollection<KeyValuePair<string, object>>.IsReadOnly
        {
            get { return false; }
        }

        bool ICollection<KeyValuePair<string, object>>.Remove(KeyValuePair<string, object> item)
        {
            throw new NotImplementedException();
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            foreach (var entryPair in _entries)
            {
                yield return new KeyValuePair<string, object>(entryPair.Key, entryPair.Value.Get(_context));
            }
            foreach (var entryPair in _context.Items)
            {
                yield return new KeyValuePair<string, object>(Convert.ToString(entryPair.Key), entryPair.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public class FeatureMap
        {
            internal FeatureMap(Type featureInterface, Func<object, object> getter)
                : this(featureInterface, getter, null)
            {
            }

            internal FeatureMap(Type featureInterface, Func<object, object> getter, Action<object, object> setter)
            {
                FeatureInterface = featureInterface;
                Getter = getter;
                Setter = setter;
            }

            internal Type FeatureInterface { get; set; }
            internal Func<object, object> Getter { get; set; }
            internal Action<object, object> Setter { get; set; }

            internal object Get(HttpContext context)
            {
                object featureInstance = context.GetFeature(FeatureInterface);
                if (featureInstance == null)
                {
                    return null;
                }
                return Getter(featureInstance);
            }

            internal void Set(HttpContext context, object value)
            {
                Setter(context.GetFeature(FeatureInterface), value);
            }
        }

        public class FeatureMap<T> : FeatureMap
        {
            internal FeatureMap(Func<T, object> getter)
                : base(typeof(T), feature => getter((T)feature))
            {
            }

            internal FeatureMap(Func<T, object> getter, Action<T, object> setter)
                : base(typeof(T), feature => getter((T)feature), (feature, value) => setter((T)feature, value))
            {
            }
        }
    }
}
