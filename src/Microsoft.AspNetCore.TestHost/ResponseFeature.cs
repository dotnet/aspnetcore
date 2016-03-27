// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.TestHost
{
    internal class ResponseFeature : IHttpResponseFeature
    {
        private Action _responseStarting = () => { };
        private Action _responseCompleted = () => { };

        public ResponseFeature()
        {
            Headers = new HeaderDictionary();
            Body = new MemoryStream();

            // 200 is the default status code all the way down to the host, so we set it
            // here to be consistent with the rest of the hosts when writing tests.
            StatusCode = 200;
        }

        public int StatusCode { get; set; }

        public string ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; }

        public Stream Body { get; set; }

        public bool HasStarted { get; set; }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            var prior = _responseStarting;
            _responseStarting = () =>
            {
                callback(state);
                prior();
            };
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            var prior = _responseCompleted;
            _responseCompleted = () =>
            {
                callback(state);
                prior();
            };
        }

        public void FireOnSendingHeaders()
        {
            _responseStarting();
            HasStarted = true;
        }

        public void FireOnResponseCompleted()
        {
            _responseCompleted();
        }
    }
}
