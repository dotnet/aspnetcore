// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

#if NET45
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class FormValueProviderFactoryTests
    {
        [Fact]
        public async Task GetValueProvider_ReturnsNull_WhenContentTypeIsNotFormUrlEncoded()
        {
            // Arrange
            var requestContext = CreateRequestContext("some-content-type");
            var factory = new FormValueProviderFactory();
            
            // Act
            var result = await factory.GetValueProviderAsync(requestContext);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("application/x-www-form-urlencoded")]
        [InlineData("application/x-www-form-urlencoded;charset=utf-8")]
        public async Task GetValueProvider_ReturnsValueProviderInstaceWithInvariantCulture(string contentType)
        {
            // Arrange
            var requestContext = CreateRequestContext(contentType);
            var factory = new FormValueProviderFactory();

            // Act
            var result = await factory.GetValueProviderAsync(requestContext);

            // Assert
            var valueProvider = Assert.IsType<ReadableStringCollectionValueProvider>(result);
            Assert.Equal(CultureInfo.CurrentCulture, valueProvider.Culture);
        }

        private static RequestContext CreateRequestContext(string contentType)
        {
            var collection = Mock.Of<IReadableStringCollection>();
            var request = new Mock<HttpRequest>();
            request.Setup(f => f.GetFormAsync()).Returns(Task.FromResult(collection));
            
            var mockHeader = new Mock<IHeaderDictionary>();
            mockHeader.Setup(h => h["Content-Type"]).Returns(contentType);
            request.SetupGet(r => r.Headers).Returns(mockHeader.Object);

            var context = new Mock<HttpContext>();
            context.SetupGet(c => c.Request).Returns(request.Object);
            
            var requestContext = new RequestContext(context.Object, new Dictionary<string, object>());
            return requestContext;
        }
    }
}
#endif
