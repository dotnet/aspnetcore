// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http.Internal;
using Xunit;

namespace Microsoft.AspNet.Builder.Internal
{
    public class ApplicationBuilderTests
    {
        [Fact]
        public void BuildReturnsCallableDelegate()
        {
            var builder = new ApplicationBuilder(null);
            var app = builder.Build();

            var httpContext = new DefaultHttpContext();

            app.Invoke(httpContext);
            Assert.Equal(httpContext.Response.StatusCode, 404);
        }
    }
}