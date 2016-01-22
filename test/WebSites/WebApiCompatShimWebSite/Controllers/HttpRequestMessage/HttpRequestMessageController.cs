// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApiCompatShimWebSite
{
    public class HttpRequestMessageController : ApiController
    {
        public async Task<IActionResult> EchoProperty()
        {
            await Echo(Request);
            return new EmptyResult();
        }

        public async Task<IActionResult> EchoParameter(HttpRequestMessage request)
        {
            if (!object.ReferenceEquals(request, Request))
            {
                throw new InvalidOperationException();
            }

            await Echo(request);
            return new EmptyResult();
        }

        public async Task<HttpResponseMessage> EchoWithResponseMessage(HttpRequestMessage request)
        {
            var message = string.Format(
                "{0} {1}",
                request.Method.ToString(),
                await request.Content.ReadAsStringAsync());

            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(message);
            response.Headers.TryAddWithoutValidation("X-Test", "Hello!");
            return response;
        }

        public async Task<HttpResponseMessage> EchoWithResponseMessageChunked(HttpRequestMessage request)
        {
            var message = string.Format(
                "{0} {1}",
                request.Method.ToString(),
                await request.Content.ReadAsStringAsync());

            var response = request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(message);
            response.Headers.TransferEncodingChunked = true;
            response.Headers.TryAddWithoutValidation("X-Test", "Hello!");
            return response;
        }

        public HttpResponseMessage GetUser(string mediaType = null)
        {
            var user = new User()
            {
                Name = "Test User",
            };

            if (mediaType == null)
            {
                // This will perform content negotiation
                return Request.CreateResponse<User>(HttpStatusCode.OK, user);
            }
            else
            {
                // This will use the provided media type
                return Request.CreateResponse<User>(HttpStatusCode.OK, user, mediaType);
            }
        }

        public HttpResponseMessage GetUserJson()
        {
            var user = new User()
            {
                Name = "Test User",
            };

            return Request.CreateResponse<User>(HttpStatusCode.OK, user, new JsonMediaTypeFormatter(), "text/json");
        }

        [HttpGet]
        public HttpResponseMessage Fail()
        {
            // This will perform content negotiation
            return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "It failed.");
        }

        [HttpGet]
        public HttpResponseMessage ReturnByteArrayContent()
        {
            var response = new HttpResponseMessage();
            response.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("Hello from ByteArrayContent!!"));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

            return response;
        }

        [HttpGet]
        public HttpResponseMessage ReturnStreamContent()
        {
            var response = new HttpResponseMessage();
            response.Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("This content is from a file")));
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");

            return response;
        }

        // NOTE: PushStreamContent's contract is to close the stream in order to signal
        // that the user has done writing to it. However, the stream that is provided here is
        // a wrapper delegating stream which actually doesn't close the actual response stream.

        [HttpGet]
        public HttpResponseMessage ReturnPushStreamContentSync()
        {
            var response = new HttpResponseMessage();
            // Here we are using a non-Task returning action delegate
            response.Content = new PushStreamContent((responseStream, httpContent, transportContext) =>
            {
                using (var streamWriter = new StreamWriter(responseStream))
                {
                    streamWriter.Write("Hello from PushStreamContent Sync!!");
                }
            });
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            return response;
        }

        [HttpGet]
        public HttpResponseMessage ReturnPushStreamContent()
        {
            var response = new HttpResponseMessage();
            response.Content = new PushStreamContent(async (responseStream, httpContent, transportContext) =>
            {
                using (var streamWriter = new StreamWriter(responseStream))
                {
                    await streamWriter.WriteAsync("Hello from PushStreamContent!!");
                }
            });
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

            return response;
        }

        [HttpGet]
        public HttpResponseMessage ReturnPushStreamContentWithCustomHeaders()
        {
            var response = new HttpResponseMessage();
            response.Headers.Add("Multiple", new[] { "value1", "value2" });
            response.Content = new PushStreamContent(async (responseStream, httpContent, transportContext) =>
            {
                using (var streamWriter = new StreamWriter(responseStream))
                {
                    await streamWriter.WriteAsync("Hello from PushStreamContent with custom headers!!");
                }
            });

            return response;
        }

        private async Task Echo(HttpRequestMessage request)
        {
            var message = string.Format(
                "{0} {1} {2} {3} {4}",
                request.Method,
                request.RequestUri.AbsoluteUri,
                request.Headers.Host,
                request.Content.Headers.ContentLength,
                await request.Content.ReadAsStringAsync());

            await Context.Response.WriteAsync(message);
        }
    }
}