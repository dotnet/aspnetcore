// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Builder.Internal
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
            Assert.Equal(404, httpContext.Response.StatusCode);
        }

        [Fact]
        public void PropertiesDictionaryIsDistinctAfterNew()
        {
            var builder1 = new ApplicationBuilder(null);
            builder1.Properties["test"] = "value1";

            var builder2 = builder1.New();
            builder2.Properties["test"] = "value2";

            Assert.Equal("value1", builder1.Properties["test"]);
        }
    }
}