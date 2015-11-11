// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Abstractions;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;
#endif

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public class QueryStringValueProviderFactoryTest
    {
        private readonly QueryStringValueProviderFactory _factory = new QueryStringValueProviderFactory();

#if DNX451
        [Fact]
        public async Task GetValueProvider_ReturnsQueryStringValueProviderInstanceWithInvariantCulture()
        {
            // Arrange
            var request = new Mock<HttpRequest>();
            request.SetupGet(f => f.Query).Returns(Mock.Of<IQueryCollection>());
            var context = new Mock<HttpContext>();
            context.SetupGet(c => c.Items).Returns(new Dictionary<object, object>());
            context.SetupGet(c => c.Request).Returns(request.Object);
            var actionContext = new ActionContext(context.Object, new RouteData(), new ActionDescriptor());

            // Act
            var result = await _factory.GetValueProviderAsync(actionContext);

            // Assert
            var valueProvider = Assert.IsType<QueryStringValueProvider>(result);
            Assert.Equal(CultureInfo.InvariantCulture, valueProvider.Culture);
        }
#endif
    }
}
