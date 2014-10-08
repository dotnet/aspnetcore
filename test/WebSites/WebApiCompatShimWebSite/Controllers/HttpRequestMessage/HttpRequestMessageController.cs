// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;

namespace WebApiCompatShimWebSite
{
    public class HttpRequestMessageController : ApiController
    {
        public async Task<IActionResult> EchoProperty()
        {
            var request = Request;

            var message = string.Format(
                "{0} {1} {2} {3} {4}",
                request.Method, 
                request.RequestUri.AbsoluteUri,
                request.Headers.Host,
                request.Content.Headers.ContentLength,
                await request.Content.ReadAsStringAsync());

            await Context.Response.WriteAsync(message);
            return new EmptyResult();
        }
    }
}