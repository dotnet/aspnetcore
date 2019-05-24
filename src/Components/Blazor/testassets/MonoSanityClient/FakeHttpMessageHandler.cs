// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace MonoSanityClient
{
    class FakeHttpMessageHandler : HttpMessageHandler
    {
        public static void Attach()
        {
            var getHttpMessageHandlerField = typeof(HttpClient).GetField(
                "GetHttpMessageHandler",
                BindingFlags.Static | BindingFlags.NonPublic);
            Func<HttpMessageHandler> handlerFactory = () => new FakeHttpMessageHandler();
            getHttpMessageHandlerField.SetValue(null, handlerFactory);
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new NotImplementedException($"{nameof(FakeHttpMessageHandler)} cannot {nameof(SendAsync)}.");
    }
}
