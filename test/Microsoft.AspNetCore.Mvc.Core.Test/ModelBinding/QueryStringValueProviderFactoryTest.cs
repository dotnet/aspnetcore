// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Test
{
    public class QueryStringValueProviderFactoryTest
    {
        [Fact]
        public async Task DoesNotCreateValueProvider_WhenQueryStringIsEmpty()
        {
            // Arrange
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            var factoryContext = new ValueProviderFactoryContext(actionContext);
            var factory = new QueryStringValueProviderFactory();

            // Act
            await factory.CreateValueProviderAsync(factoryContext);

            // Assert
            Assert.Empty(factoryContext.ValueProviders);
        }

        [Fact]
        public async Task GetValueProvider_ReturnsQueryStringValueProviderInstanceWithInvariantCulture()
        {
            // Arrange
            var queryValues = new Dictionary<string, StringValues>();
            queryValues.Add("foo", "bar");
            var context = new DefaultHttpContext();
            context.Request.Query = new QueryCollection(queryValues);
            var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());
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
