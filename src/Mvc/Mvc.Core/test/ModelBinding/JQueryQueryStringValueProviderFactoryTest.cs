// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Test;

public class JQueryQueryStringValueProviderFactoryTest
{
    private static readonly Dictionary<string, StringValues> _backingStore = new Dictionary<string, StringValues>
        {
            { "[]", new[] { "found" } },
            { "[]property1", new[] { "found" } },
            { "property2[]", new[] { "found" } },
            { "[]property3[]", new[] { "found" } },
            { "property[]Value", new[] { "found" } },
            { "[10]", new[] { "found" } },
            { "[11]property", new[] { "found" } },
            { "property4[10]", new[] { "found" } },
            { "[12]property[][13]", new[] { "found" } },
            { "[14][]property1[15]property2", new[] { "found" } },
            { "prefix[11]property1", new[] { "found" } },
            { "prefix[12][][property2]", new[] { "found" } },
            { "prefix[property1][13]", new[] { "found" } },
            { "prefix[14][][15]", new[] { "found" } },
            { "[property5][]", new[] { "found" } },
            { "[][property6]Value", new[] { "found" } },
            { "prefix[property2]", new[] { "found" } },
            { "prefix[][property]Value", new[] { "found" } },
            { "[property7][property8]", new[] { "found" } },
            { "[property9][][property10]Value", new[] { "found" } },
        };

    public static TheoryData<string> SuccessDataSet
    {
        get
        {
            return new TheoryData<string>
                {
                    string.Empty,
                    "property1",
                    "property2",
                    "property3",
                    "propertyValue",
                    "[10]",
                    "[11]property",
                    "property4[10]",
                    "[12]property[13]",
                    "[14]property1[15]property2",
                    "prefix.property1[13]",
                    "prefix[14][15]",
                    "property5",
                    "property6Value",
                    "prefix.property2",
                    "prefix.propertyValue",
                    "property7.property8",
                    "property9.property10Value",
                };
        }
    }

    [Theory]
    [MemberData(nameof(SuccessDataSet))]
    public async Task GetValueProvider_ReturnsValueProvider_ContainingExpectedKeys(string key)
    {
        // Arrange
        var context = CreateContext(_backingStore);
        var factory = new JQueryQueryStringValueProviderFactory();

        // Act
        await factory.CreateValueProviderAsync(context);

        // Assert
        var valueProvider = Assert.Single(context.ValueProviders);
        var result = valueProvider.GetValue(key);
        Assert.Equal("found", (string)result);
    }

    [Fact]
    public async Task DoesNotCreateValueProvider_WhenQueryIsEmpty()
    {
        // Arrange
        var context = CreateContext(new Dictionary<string, StringValues>());
        var factory = new JQueryQueryStringValueProviderFactory();

        // Act
        await factory.CreateValueProviderAsync(context);

        // Assert
        Assert.Empty(context.ValueProviders);
    }

    [Fact]
    public async Task CreatesValueProvider_WithInvariantCulture()
    {
        // Arrange
        var context = CreateContext(_backingStore);
        var factory = new JQueryQueryStringValueProviderFactory();

        // Act
        await factory.CreateValueProviderAsync(context);

        // Assert
        var valueProvider = Assert.Single(context.ValueProviders);
        var jqueryQueryStringValueProvider = Assert.IsType<JQueryQueryStringValueProvider>(valueProvider);
        Assert.Equal(CultureInfo.InvariantCulture, jqueryQueryStringValueProvider.Culture);
    }

    private static ValueProviderFactoryContext CreateContext(Dictionary<string, StringValues> queryStringValues)
    {
        var context = new DefaultHttpContext();

        context.Request.Query = new QueryCollection(queryStringValues);

        var actionContext = new ActionContext(context, new RouteData(), new ActionDescriptor());
        return new ValueProviderFactoryContext(actionContext);
    }
}
