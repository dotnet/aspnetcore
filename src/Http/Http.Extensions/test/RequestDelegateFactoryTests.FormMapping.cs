// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            ThrowOnBadRequest = true
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
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await requestDelegate(httpContext));

        // Assert
        Assert.Equal("The number of elements in the dictionary exceeded the maximum number of '2' elements allowed.", exception.Message);
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
            ThrowOnBadRequest = true
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
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await requestDelegate(httpContext));

        // Assert
        Assert.Equal("The number of elements in the dictionary exceeded the maximum number of '2' elements allowed.", exception.Message);
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
            ThrowOnBadRequest = true
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
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await requestDelegate(httpContext));

        // Assert
        Assert.Equal("The number of elements in the dictionary exceeded the maximum number of '2' elements allowed.", exception.Message);

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

    [Fact]
    public async Task SupportsFormMappingWithRecordTypes()
    {
        TodoRecord capturedTodo = default;
        void TestAction([FromForm] TodoRecord args) { capturedTodo = args; };
        var httpContext = CreateHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            {
                "id", "1"
            },
            {
                "name", "Write tests"
            },
            {
                "isCompleted", "false"
            }
        });

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
        Assert.Equal(1, capturedTodo.Id);
        Assert.Equal("Write tests", capturedTodo.Name);
        Assert.False(capturedTodo.IsCompleted);
    }

    [Fact]
    public async Task SupportsRecursiveProperties()
    {
        Employee capturedEmployee = default;
        void TestAction([FromForm] Employee args) { capturedEmployee = args; };
        var httpContext = CreateHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            {
                "Name", "A"
            },
            {
                "Manager.Name", "B"
            },
            {
                "Manager.Manager.Name", "C"
            },
            {
                "Manager.Manager.Manager.Name", "D"
            }
        });

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);

        Assert.Equal(StatusCodes.Status200OK, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task SupportsRecursivePropertiesWithRecursionLimit()
    {
        Employee capturedEmployee = default;
        var options = new RequestDelegateFactoryOptions
        {
            EndpointBuilder = CreateEndpointBuilder(new List<object>()
            {
                new FormMappingOptionsMetadata(maxRecursionDepth: 3)
            }),
            ThrowOnBadRequest = true
        };
        var metadataResult = new RequestDelegateMetadataResult { EndpointMetadata = new List<object>() };
        void TestAction([FromForm] Employee args) { capturedEmployee = args; };
        var httpContext = CreateHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            {
                "Name", "A"
            },
            {
                "Manager.Name", "B"
            },
            {
                "Manager.Manager.Name", "C"
            },
            {
                "Manager.Manager.Manager.Name", "D"
            },
            {
                "Manager.Manager.Manager.Manager.Name", "E"
            },
            {
                "Manager.Manager.Manager.Manager.Manager.Name", "F"
            },
            {
                "Manager.Manager.Manager.Manager.Manager.Manager.Name", "G"
            }
        });

        var factoryResult = RequestDelegateFactory.Create(TestAction, options, metadataResult);
        var requestDelegate = factoryResult.RequestDelegate;

        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(async () => await requestDelegate(httpContext));

        Assert.Equal("The maximum recursion depth of '3' was exceeded for 'Manager.Manager.Manager.Name'.", exception.Message);
    }

    [Fact]
    public async Task SupportsFormFileSourcesInDto()
    {
        FormFileDto capturedArgument = default;
        void TestAction([FromForm] FormFileDto args) { capturedArgument = args; };
        var httpContext = CreateHttpContext();
        var formFiles = new FormFileCollection
        {
            new FormFile(Stream.Null, 0, 10, "file", "file.txt"),
            new FormFile(Stream.Null, 0, 10, "formFiles", "file-1.txt"),
            new FormFile(Stream.Null, 0, 10, "formFiles", "file-2.txt"),
        };
        httpContext.Request.Form = new FormCollection(new() { { "Description", "A test file" } }, formFiles);

        var factoryResult = RequestDelegateFactory.Create(TestAction);
        var requestDelegate = factoryResult.RequestDelegate;

        await requestDelegate(httpContext);
        Assert.Equal("A test file", capturedArgument.Description);
        Assert.Equal(formFiles["file"], capturedArgument.File);
        Assert.Equal(formFiles.GetFiles("formFiles"), capturedArgument.FormFiles);
        Assert.Equal(formFiles, capturedArgument.FormFileCollection);
    }

    private record TodoRecord(int Id, string Name, bool IsCompleted);

    private class Employee
    {
        public string Name { get; set; }
        public Employee Manager { get; set; }
    }
#nullable enable

    private class FormFileDto
    {
        public string Description { get; set; } = String.Empty;
        public IFormFile? File { get; set; }
        public IReadOnlyList<IFormFile>? FormFiles { get; set; }
        public IFormFileCollection? FormFileCollection { get; set; }
    }
}
