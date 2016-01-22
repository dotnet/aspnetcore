// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.TestHost
{
    /// <summary>
    /// Used to construct a HttpRequestMessage object.
    /// </summary>
    public class RequestBuilder
    {
        private readonly TestServer _server;
        private readonly HttpRequestMessage _req;

        /// <summary>
        /// Construct a new HttpRequestMessage with the given path.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="path"></param>
        public RequestBuilder(TestServer server, string path)
        {
            if (server == null)
            {
                throw new ArgumentNullException(nameof(server));
            }

            _server = server;
            _req = new HttpRequestMessage(HttpMethod.Get, path);
        }

        /// <summary>
        /// Configure any HttpRequestMessage properties.
        /// </summary>
        /// <param name="configure"></param>
        /// <returns></returns>
        public RequestBuilder And(Action<HttpRequestMessage> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            configure(_req);
            return this;
        }

        /// <summary>
        /// Add the given header and value to the request or request content.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public RequestBuilder AddHeader(string name, string value)
        {
            if (!_req.Headers.TryAddWithoutValidation(name, value))
            {
                if (_req.Content == null)
                {
                    _req.Content = new StreamContent(Stream.Null);
                }
                if (!_req.Content.Headers.TryAddWithoutValidation(name, value))
                {
                    // TODO: throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.InvalidHeaderName, name), "name");
                    throw new ArgumentException("Invalid header name: " + name, "name");
                }
            }
            return this;
        }

        /// <summary>
        /// Set the request method and start processing the request.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public Task<HttpResponseMessage> SendAsync(string method)
        {
            _req.Method = new HttpMethod(method);
            return _server.CreateClient().SendAsync(_req);
        }

        /// <summary>
        /// Set the request method to GET and start processing the request.
        /// </summary>
        /// <returns></returns>
        public Task<HttpResponseMessage> GetAsync()
        {
            _req.Method = HttpMethod.Get;
            return _server.CreateClient().SendAsync(_req);
        }

        /// <summary>
        /// Set the request method to POST and start processing the request.
        /// </summary>
        /// <returns></returns>
        public Task<HttpResponseMessage> PostAsync()
        {
            _req.Method = HttpMethod.Post;
            return _server.CreateClient().SendAsync(_req);
        }
    }
}
