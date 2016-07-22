// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class ResponseCachingContextTests
    {
        [Fact]
        public void CheckRequestAllowsCaching_Method_GET_Allowed()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            var context = new ResponseCachingContext(httpContext, new TestResponseCache());

            Assert.True(context.CheckRequestAllowsCaching());
        }

        [Theory]
        [InlineData("POST")]
        public void CheckRequestAllowsCaching_Method_Unsafe_NotAllowed(string method)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = method;
            var context = new ResponseCachingContext(httpContext, new TestResponseCache());

            Assert.False(context.CheckRequestAllowsCaching());
        }

        private class TestResponseCache : IResponseCache
        {
            public object Get(string key)
            {
                return null;
            }

            public void Remove(string key)
            {
            }

            public void Set(string key, object entry)
            {
            }
        }
    }
}
