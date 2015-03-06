// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.TestHost
{
    internal class ResponseFeature : IHttpResponseFeature
    {
        private Action _sendingHeaders = () => { };

        public ResponseFeature()
        {
            Headers = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            Body = new MemoryStream();

            // 200 is the default status code all the way down to the host, so we set it
            // here to be consistent with the rest of the hosts when writing tests.
            StatusCode = 200;
        }

        public int StatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public IDictionary<string, string[]> Headers { get; set; }

        public Stream Body { get; set; }

        public bool HeadersSent { get; set; }

        public void OnSendingHeaders(Action<object> callback, object state)
        {
            var prior = _sendingHeaders;
            _sendingHeaders = () =>
            {
                callback(state);
                prior();
            };
        }

        public void FireOnSendingHeaders()
        {
            _sendingHeaders();
            HeadersSent = true;
        }
    }
}
