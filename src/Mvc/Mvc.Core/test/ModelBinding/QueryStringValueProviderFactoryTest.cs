// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Test;

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
