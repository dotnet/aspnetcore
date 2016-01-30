// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Test
{
    public class QueryStringValueProviderFactoryTest
    {
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
            var factoryContext = new ValueProviderFactoryContext(actionContext);
            var factory = new QueryStringValueProviderFactory();

            // Act
            await factory.CreateValueProviderAsync(factoryContext);

            // Assert
            var valueProvider = Assert.IsType<QueryStringValueProvider>(Assert.Single(factoryContext.ValueProviders));
            Assert.Equal(CultureInfo.InvariantCulture, valueProvider.Culture);
        }
    }
}
