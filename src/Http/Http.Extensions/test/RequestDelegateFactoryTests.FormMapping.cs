// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing.Internal;

public partial class RequestDelegateFactoryTests : LoggedTest
{
    [Fact]
    public async Task SupportsFormMappingOptionsInMetadata()
    {
        // Arrange
        static void TestAction([FromForm] Dictionary<string, string> args) { }
        var options = new RequestDelegateFactoryOptions
        {
            EndpointBuilder = CreateEndpointBuilder(new List<object>()
            {
                new FormMappingOptionsMetadata(maxCollectionSize: 2)
            }),
        };
        var metadataResult = new RequestDelegateMetadataResult { EndpointMetadata = new List<object>() };
        var httpContext = CreateHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            {
                "[name1]", "value1"
            },
            {
                "[name2]", "value2"
            },
            {
                "[name3]", "value3"
            },
            {
                "[name4]", "value4"
            },
            {
                "[name5]", "value5"
            },
            {
                "[name6]", "value6"
            }
        });

        var factoryResult = RequestDelegateFactory.Create(TestAction, options, metadataResult);
        var requestDelegate = factoryResult.RequestDelegate;

        // Act
        var exception = await Assert.ThrowsAsync<FormDataMappingException>(async () => await requestDelegate(httpContext));

        // Assert
        Assert.Equal("The number of elements in the dictionary exceeded the maximum number of '2' elements allowed.", exception.Error.Message.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task SupportsFormMappingOptionsInMetadataFormFormWithAttributeName()
    {
        // Arrange
        static void TestAction([FromForm(Name = "shouldSetKeyCorrectly")] Dictionary<string, string> args) { }
        var options = new RequestDelegateFactoryOptions
        {
            EndpointBuilder = CreateEndpointBuilder(new List<object>()
            {
                new FormMappingOptionsMetadata(maxCollectionSize: 2)
            }),
        };
        var metadataResult = new RequestDelegateMetadataResult { EndpointMetadata = new List<object>() };
        var httpContext = CreateHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            {
                "[name1]", "value1"
            },
            {
                "[name2]", "value2"
            },
            {
                "[name3]", "value3"
            },
            {
                "[name4]", "value4"
            },
            {
                "[name5]", "value5"
            },
            {
                "[name6]", "value6"
            }
        });

        var factoryResult = RequestDelegateFactory.Create(TestAction, options, metadataResult);
        var requestDelegate = factoryResult.RequestDelegate;

        // Act
        var exception = await Assert.ThrowsAsync<FormDataMappingException>(async () => await requestDelegate(httpContext));

        // Assert
        Assert.Equal("The number of elements in the dictionary exceeded the maximum number of '2' elements allowed.", exception.Error.Message.ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task SupportsMergingFormMappingOptionsInMetadata()
    {
        // Arrange
        static void TestAction([FromForm] Dictionary<string, string> args) { }
        var options = new RequestDelegateFactoryOptions
        {
            EndpointBuilder = CreateEndpointBuilder(new List<object>()
            {
                new FormMappingOptionsMetadata(maxCollectionSize: 2),
                new FormMappingOptionsMetadata(maxKeySize: 23)
            }),
        };
        var metadataResult = new RequestDelegateMetadataResult { EndpointMetadata = new List<object>() };
        var httpContext = CreateHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            {
                "[name1]", "value1"
            },
            {
                "[name2]", "value2"
            },
            {
                "[name3]", "value3"
            },
            {
                "[name4]", "value4"
            },
            {
                "[name5]", "value5"
            },
            {
                "[name6]", "value6"
            }
        });

        var factoryResult = RequestDelegateFactory.Create(TestAction, options, metadataResult);
        var requestDelegate = factoryResult.RequestDelegate;

        // Act
        var exception = await Assert.ThrowsAsync<FormDataMappingException>(async () => await requestDelegate(httpContext));

        // Assert
        Assert.Equal("The number of elements in the dictionary exceeded the maximum number of '2' elements allowed.", exception.Error.Message.ToString(CultureInfo.InvariantCulture));

        // Arrange - 2
        httpContext = CreateHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            {
                "[name1name1name1name1name1name1name1]", "value1"
            },
            {
                "[name2name2name2name2name2name2name2]", "value2"
            },
        });

        // Act - 2
        var anotherException = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await requestDelegate(httpContext));

        // Assert - 2
        Assert.Equal("Specified argument was out of the range of valid values.", anotherException.Message);

    }
}
