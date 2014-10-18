// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.AspNet.Testing;
using Xunit;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class HttpResponseExceptionTest
    {
        [Fact]
        [ReplaceCulture]
        public void Constructor_SetsResponseProperty()
        {
            // Arrange and Act
            var response = new HttpResponseMessage();
            var exception = new HttpResponseException(response);

            // Assert
            Assert.Same(response, exception.Response);
            Assert.Equal("Processing of the HTTP request resulted in an exception."+
                         " Please see the HTTP response returned by the 'Response' "+
                         "property of this exception for details.",
                         exception.Message);
        }

        [Fact]
        [ReplaceCulture]
        public void Constructor_SetsResponsePropertyWithGivenStatusCode()
        {
            // Arrange and Act
            var exception = new HttpResponseException(HttpStatusCode.BadGateway);

            // Assert
            Assert.Equal(HttpStatusCode.BadGateway, exception.Response.StatusCode);
            Assert.Equal("Processing of the HTTP request resulted in an exception."+
                         " Please see the HTTP response returned by the 'Response' "+
                         "property of this exception for details.",
                         exception.Message);
        }
    }
}