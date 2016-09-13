// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCaching.Internal;
using Xunit;

namespace Microsoft.AspNetCore.ResponseCaching.Tests
{
    public class HttpContextInternalExtensionTests
    {
        [Fact]
        public void AddingSecondResponseCachingFeature_Throws()
        {
            var httpContext = new DefaultHttpContext();

            // Should not throw
            httpContext.AddResponseCachingFeature();

            // Should throw
            Assert.ThrowsAny<InvalidOperationException>(() => httpContext.AddResponseCachingFeature());
        }
    }
}
