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
        private Func<Task> _responseStartingAsync = () => Task.FromResult(true);
        private Func<Task> _responseCompletedAsync = () => Task.FromResult(true);

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
            if (HasStarted)
            {
                throw new InvalidOperationException();
            }

            var prior = _responseStartingAsync;
            _responseStartingAsync = async () =>
            {
                await callback(state);
                await prior();
            };
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            var prior = _responseCompletedAsync;
            _responseCompletedAsync = async () =>
            {
                try
                {
                    await callback(state);
                }
                finally
                {
                    await prior();
                }
            };
        }

        public async Task FireOnSendingHeadersAsync()
        {
            await _responseStartingAsync();
            HasStarted = true;
        }

        public Task FireOnResponseCompletedAsync()
        {
            return _responseCompletedAsync();
        }
    }
}
