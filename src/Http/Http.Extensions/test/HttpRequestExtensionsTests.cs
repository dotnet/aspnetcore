// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

#nullable enable

namespace Microsoft.AspNetCore.Http.Extensions.Tests
{
    public class HttpRequestExtensionsTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("application/xml", false)]
        [InlineData("text/json", false)]
        [InlineData("text/json; charset=utf-8", false)]
        [InlineData("application/json", true)]
        [InlineData("application/json; charset=utf-8", true)]
        [InlineData("application/ld+json", true)]
        [InlineData("APPLICATION/JSON", true)]
        [InlineData("APPLICATION/JSON; CHARSET=UTF-8", true)]
        [InlineData("APPLICATION/LD+JSON", true)]
        public void HasJsonContentType(string contentType, bool hasJsonContentType)
        {
            var request = new DefaultHttpContext().Request;
            request.ContentType = contentType;

            Assert.Equal(hasJsonContentType, request.HasJsonContentType());
        }
    }
}
