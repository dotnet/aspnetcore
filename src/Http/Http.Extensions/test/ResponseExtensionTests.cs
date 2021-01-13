// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Http.Extensions
{
    public class ResponseExtensionTests
    {
        [Fact]
        public void Clear_ResetsResponse()
        {
            var context = new DefaultHttpContext();
            context.Response.StatusCode = 201;
            context.Response.Headers["custom"] = "value";
            context.Response.Body.Write(new byte[100], 0, 100);

            context.Response.Clear();

            Assert.Equal(200, context.Response.StatusCode);
            Assert.Equal(string.Empty, context.Response.Headers["custom"].ToString());
            Assert.Equal(0, context.Response.Body.Length);
        }

        [Fact]
        public void Clear_AlreadyStarted_Throws()
        {
            var context = new DefaultHttpContext();
            context.Features.Set<IHttpResponseFeature>(new StartedResponseFeature());

            Assert.Throws<InvalidOperationException>(() => context.Response.Clear());
        }

        [Theory]
        [InlineData(true, false, 301)]
        [InlineData(false, false, 302)]
        [InlineData(true, true, 308)]
        [InlineData(false, true, 307)]
        public void Redirect_SetsResponseCorrectly(bool permanent, bool preserveMethod, int expectedStatusCode)
        {
            var location = "http://localhost/redirect";
            var context = new DefaultHttpContext();
            context.Response.StatusCode = StatusCodes.Status200OK;

            context.Response.Redirect(location, permanent, preserveMethod);

            Assert.Equal(location, context.Response.Headers[HeaderNames.Location].First());
            Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        }

        private class StartedResponseFeature : IHttpResponseFeature
        {
            public Stream Body { get; set; }

            public bool HasStarted { get { return true; } }

            public IHeaderDictionary Headers { get; set; }

            public string ReasonPhrase { get; set; }

            public int StatusCode { get; set; }

            public void OnCompleted(Func<object, Task> callback, object state)
            {
                throw new NotImplementedException();
            }

            public void OnStarting(Func<object, Task> callback, object state)
            {
                throw new NotImplementedException();
            }
        }
    }
}
