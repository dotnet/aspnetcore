// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
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
