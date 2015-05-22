// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http.Internal;
using Microsoft.Framework.Caching.Memory;
using Xunit;

namespace Microsoft.AspNet.ResponseCaching
{
    public class CachingContextTests
    {
        [Fact]
        public void CheckRequestAllowsCaching_Method_GET_Allowed()
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            var context = new CachingContext(httpContext, new MemoryCache(new MemoryCacheOptions()));

            Assert.True(context.CheckRequestAllowsCaching());
        }

        [Theory]
        [InlineData("POST")]
        public void CheckRequestAllowsCaching_Method_Unsafe_NotAllowed(string method)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = method;
            var context = new CachingContext(httpContext, new MemoryCache(new MemoryCacheOptions()));

            Assert.False(context.CheckRequestAllowsCaching());
        }
    }
}
