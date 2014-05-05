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

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
#if NET45
using Moq;
#endif
using Xunit;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class QueryStringValueProviderFactoryTest
    {
        private readonly QueryStringValueProviderFactory _factory = new QueryStringValueProviderFactory();

#if NET45
        [Fact]
        public async Task GetValueProvider_ReturnsQueryStringValueProviderInstaceWithInvariantCulture()
        {
            // Arrange
            var request = new Mock<HttpRequest>();
            request.SetupGet(f => f.Query).Returns(Mock.Of<IReadableStringCollection>());
            var context = new Mock<HttpContext>();
            context.SetupGet(c => c.Items).Returns(new Dictionary<object, object>());
            context.SetupGet(c => c.Request).Returns(request.Object);
            var requestContext = new RequestContext(context.Object, new Dictionary<string, object>());

            // Act
            var result = await _factory.GetValueProviderAsync(requestContext);

            // Assert
            var valueProvider = Assert.IsType<ReadableStringCollectionValueProvider>(result);
            Assert.Equal(CultureInfo.InvariantCulture, valueProvider.Culture);
        }
#endif
    }
}
