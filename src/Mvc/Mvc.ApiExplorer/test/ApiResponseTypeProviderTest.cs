// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

public class ApiResponseTypeProviderTest
{
    [Fact]
    public void GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresent()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(
            typeof(GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresentController),
            nameof(GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresentController.Get));
        actionDescriptor.Properties[typeof(ApiConventionResult)] = new ApiConventionResult(new[]
        {
                new ProducesResponseTypeAttribute(201),
                new ProducesResponseTypeAttribute(404),
            });

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(BaseModel), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format =>
                    {
                        Assert.Equal("application/json", format.MediaType);
                        Assert.IsType<TestOutputFormatter>(format.Formatter);
                    });
            },
            responseType =>
            {
                Assert.Equal(301, responseType.StatusCode);
                Assert.Equal(typeof(void), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Empty(responseType.ApiResponseFormats);
            },
            responseType =>
            {
                Assert.Equal(404, responseType.StatusCode);
                Assert.Equal(typeof(void), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Empty(responseType.ApiResponseFormats);
            });
    }

    [ApiConventionType(typeof(DefaultApiConventions))]
    public class GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresentController : ControllerBase
    {
        [Produces(typeof(BaseModel))]
        [ProducesResponseType(301)]
        [ProducesResponseType(404)]
        public Task<ActionResult<BaseModel>> Get(int id) => null;
    }

    [Fact]
    public void GetApiResponseTypes_CombinesFilters()
    {
        // Arrange
        var filterDescriptors = new[]
        {
                new FilterDescriptor(new ProducesResponseTypeAttribute(400), FilterScope.Global),
                new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(object), 201), FilterScope.Controller),
                new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(ProblemDetails), 400), FilterScope.Controller),
                new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(BaseModel), 201), FilterScope.Action),
                new FilterDescriptor(new ProducesResponseTypeAttribute(404), FilterScope.Action),
            };

        // Global:
        //  400 => void (ignored, overriden by 400 status code in controller scope)
        // --
        // Controller:
        //  201 => object (ignored, overriden by 201 status code in action scope)
        //  400 => ProblemDetails
        // --
        // Action:
        // 201 => BaseModel
        // 404 => void

        var actionDescriptor = new ControllerActionDescriptor
        {
            FilterDescriptors = filterDescriptors,
            MethodInfo = typeof(GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresentController).GetMethod(nameof(GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresentController.Get)),
        };

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            // BaseModel; 201 => scope=Action
            responseType =>
            {
                Assert.Equal(201, responseType.StatusCode);
                Assert.Equal(typeof(BaseModel), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format =>
                    {
                        Assert.Equal("application/json", format.MediaType);
                        Assert.IsType<TestOutputFormatter>(format.Formatter);
                    });
            },
            // ProblemDetails; 400 => scope=Controller
            responseType =>
            {
                Assert.Equal(400, responseType.StatusCode);
                Assert.Equal(typeof(ProblemDetails), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format =>
                    {
                        Assert.Equal("application/json", format.MediaType);
                        Assert.IsType<TestOutputFormatter>(format.Formatter);
                    });
            },
            // 404; void => scope=Action
            responseType =>
            {
                Assert.Equal(404, responseType.StatusCode);
                Assert.Equal(typeof(void), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Empty(responseType.ApiResponseFormats);
            });
    }

    [Fact]
    public void GetApiResponseTypes_ReturnsResponseTypesFromApiConventionItem()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(
            typeof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController),
            nameof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController.DeleteBase));

        actionDescriptor.Properties[typeof(ApiConventionResult)] = new ApiConventionResult(new[]
        {
                new ProducesResponseTypeAttribute(200),
                new ProducesResponseTypeAttribute(400),
                new ProducesResponseTypeAttribute(404),
            });

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
           result,
           responseType =>
           {
               Assert.Equal(200, responseType.StatusCode);
               Assert.Equal(typeof(BaseModel), responseType.Type);
               Assert.False(responseType.IsDefaultResponse);
               Assert.Collection(
                    responseType.ApiResponseFormats,
                    format =>
                    {
                        Assert.Equal("application/json", format.MediaType);
                        Assert.IsType<TestOutputFormatter>(format.Formatter);
                    });
           },
           responseType =>
           {
               Assert.Equal(400, responseType.StatusCode);
               Assert.Equal(typeof(void), responseType.Type);
               Assert.False(responseType.IsDefaultResponse);
               Assert.Empty(responseType.ApiResponseFormats);
           },
           responseType =>
           {
               Assert.Equal(404, responseType.StatusCode);
               Assert.Equal(typeof(void), responseType.Type);
               Assert.False(responseType.IsDefaultResponse);
               Assert.Empty(responseType.ApiResponseFormats);
           });
    }

    [Fact]
    public void GetApiResponseTypes_ReturnsDescriptionFromProducesResponseType()
    {
        // Arrange

        const string expectedOkDescription = "All is well";
        const string expectedBadRequestDescription = "Invalid request";
        const string expectedNotFoundDescription = "Something was not found";

        var actionDescriptor = GetControllerActionDescriptor(
            typeof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController),
            nameof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController.DeleteBase));

        actionDescriptor.Properties[typeof(ApiConventionResult)] = new ApiConventionResult(new[]
        {
                new ProducesResponseTypeAttribute(200) { Description = expectedOkDescription},
                new ProducesResponseTypeAttribute(400) { Description = expectedBadRequestDescription },
                new ProducesResponseTypeAttribute(404) { Description = expectedNotFoundDescription },
            });

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
           result,
           responseType =>
           {
               Assert.Equal(200, responseType.StatusCode);
               Assert.Equal(typeof(BaseModel), responseType.Type);
               Assert.False(responseType.IsDefaultResponse);
               Assert.Equal(expectedOkDescription, responseType.Description);
               Assert.Collection(
                    responseType.ApiResponseFormats,
                    format =>
                    {
                        Assert.Equal("application/json", format.MediaType);
                        Assert.IsType<TestOutputFormatter>(format.Formatter);
                    });
           },
           responseType =>
           {
               Assert.Equal(400, responseType.StatusCode);
               Assert.Equal(typeof(void), responseType.Type);
               Assert.False(responseType.IsDefaultResponse);
               Assert.Empty(responseType.ApiResponseFormats);
               Assert.Equal(expectedBadRequestDescription, responseType.Description);
           },
           responseType =>
           {
               Assert.Equal(404, responseType.StatusCode);
               Assert.Equal(typeof(void), responseType.Type);
               Assert.False(responseType.IsDefaultResponse);
               Assert.Empty(responseType.ApiResponseFormats);
               Assert.Equal(expectedNotFoundDescription, responseType.Description);
           });
    }

    [ApiConventionType(typeof(DefaultApiConventions))]
    public class GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController : ControllerBase
    {
        public Task<ActionResult<BaseModel>> DeleteBase(int id) => null;
    }

    [Fact]
    public void GetApiResponseTypes_ReturnsDefaultResultsIfNoConventionsMatch()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(
            typeof(GetApiResponseTypes_ReturnsDefaultResultsIfNoConventionsMatchController),
            nameof(GetApiResponseTypes_ReturnsDefaultResultsIfNoConventionsMatchController.PostModel));

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
           result,
           responseType =>
           {
               Assert.Equal(200, responseType.StatusCode);
               Assert.Equal(typeof(BaseModel), responseType.Type);
               Assert.False(responseType.IsDefaultResponse);
               Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
           });
    }

    [ApiConventionType(typeof(DefaultApiConventions))]
    public class GetApiResponseTypes_ReturnsDefaultResultsIfNoConventionsMatchController : ControllerBase
    {
        public Task<ActionResult<BaseModel>> PostModel(int id, BaseModel model) => null;
    }

    [Fact]
    public void GetApiResponseTypes_ReturnsDefaultProblemResponse()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(
            typeof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController),
            nameof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController.DeleteBase));
        actionDescriptor.Properties[typeof(ApiConventionResult)] = new ApiConventionResult(new IApiResponseMetadataProvider[]
        {
                new ProducesResponseTypeAttribute(201),
                new ProducesResponseTypeAttribute(404),
                new ProducesDefaultResponseTypeAttribute(typeof(SerializableError)),
        });

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.True(responseType.IsDefaultResponse);
                Assert.Equal(typeof(SerializableError), responseType.Type);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            },
            responseType =>
            {
                Assert.Equal(201, responseType.StatusCode);
                Assert.Equal(typeof(BaseModel), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            },
            responseType =>
            {
                Assert.Equal(404, responseType.StatusCode);
                Assert.Equal(typeof(void), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Empty(responseType.ApiResponseFormats);
            });
    }

    public class GetApiResponseTypes_WithApiConventionMethodAndProducesResponseType : ControllerBase
    {
        [ApiConventionMethod(typeof(DefaultApiConventions), nameof(DefaultApiConventions.Get))]
        [ProducesResponseType(201)]
        [ProducesResponseType(404)]
        public Task<ActionResult<BaseModel>> Put(int id, BaseModel model) => null;
    }

    [Fact]
    public void GetApiResponseTypes_ReturnsValuesFromProducesResponseType_IfApiConventionMethodAndAttributesAreSpecified()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(
            typeof(GetApiResponseTypes_WithApiConventionMethodAndProducesResponseType),
            nameof(GetApiResponseTypes_WithApiConventionMethodAndProducesResponseType.Put));
        actionDescriptor.Properties[typeof(ApiConventionResult)] = new ApiConventionResult(new IApiResponseMetadataProvider[]
        {
                new ProducesResponseTypeAttribute(200),
                new ProducesResponseTypeAttribute(404),
                new ProducesDefaultResponseTypeAttribute(),
        });

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.Equal(201, responseType.StatusCode);
                Assert.Equal(typeof(BaseModel), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            },
            responseType =>
            {
                Assert.Equal(404, responseType.StatusCode);
                Assert.Equal(typeof(void), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Empty(responseType.ApiResponseFormats);
            });
    }

    [Fact]
    public void GetApiResponseTypes_UsesErrorType_ForClientErrors()
    {
        // Arrange
        var errorType = typeof(InvalidTimeZoneException);
        var actionDescriptor = GetControllerActionDescriptor(
             typeof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController),
             nameof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController.DeleteBase));
        actionDescriptor.Properties[typeof(ApiConventionResult)] = new ApiConventionResult(new IApiResponseMetadataProvider[]
        {
                new ProducesResponseTypeAttribute(200),
                new ProducesResponseTypeAttribute(404),
                new ProducesResponseTypeAttribute(415),
        });

        actionDescriptor.Properties[typeof(ProducesErrorResponseTypeAttribute)] = new ProducesErrorResponseTypeAttribute(errorType);

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(BaseModel), responseType.Type);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            },
            responseType =>
            {
                Assert.Equal(404, responseType.StatusCode);
                Assert.Equal(errorType, responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            },
            responseType =>
            {
                Assert.Equal(415, responseType.StatusCode);
                Assert.Equal(errorType, responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            });
    }

    [Fact]
    public void GetApiResponseTypes_UsesErrorType_ForDefaultResponse()
    {
        // Arrange
        var errorType = typeof(ProblemDetails);
        var actionDescriptor = GetControllerActionDescriptor(
             typeof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController),
             nameof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController.DeleteBase));
        actionDescriptor.Properties[typeof(ApiConventionResult)] = new ApiConventionResult(new IApiResponseMetadataProvider[]
        {
                new ProducesResponseTypeAttribute(200),
                new ProducesDefaultResponseTypeAttribute(),
        });

        actionDescriptor.Properties[typeof(ProducesErrorResponseTypeAttribute)] = new ProducesErrorResponseTypeAttribute(errorType);

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.Equal(errorType, responseType.Type);
                Assert.True(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            },
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(BaseModel), responseType.Type);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            });
    }

    [Fact]
    public void GetApiResponseTypes_DoesNotUseErrorType_IfSpecified()
    {
        // Arrange
        var errorType = typeof(InvalidTimeZoneException);
        var actionDescriptor = GetControllerActionDescriptor(
             typeof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController),
             nameof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController.DeleteBase));
        actionDescriptor.Properties[typeof(ApiConventionResult)] = new ApiConventionResult(new IApiResponseMetadataProvider[]
        {
                new ProducesResponseTypeAttribute(200),
                new ProducesResponseTypeAttribute(typeof(DivideByZeroException), 415),
                new ProducesDefaultResponseTypeAttribute(typeof(DivideByZeroException)),
        });

        actionDescriptor.Properties[typeof(ProducesErrorResponseTypeAttribute)] = new ProducesErrorResponseTypeAttribute(errorType);

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.Equal(typeof(DivideByZeroException), responseType.Type);
                Assert.True(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            },
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(BaseModel), responseType.Type);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            },
            responseType =>
            {
                Assert.Equal(415, responseType.StatusCode);
                Assert.Equal(typeof(DivideByZeroException), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            });
    }

    [Fact]
    public void GetApiResponseTypes_DoesNotUseErrorType_ForNonClientErrors()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(
             typeof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController),
             nameof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController.DeleteBase));
        actionDescriptor.Properties[typeof(ApiConventionResult)] = new ApiConventionResult(new IApiResponseMetadataProvider[]
        {
                new ProducesResponseTypeAttribute(201),
                new ProducesResponseTypeAttribute(300),
                new ProducesResponseTypeAttribute(500),
        });

        actionDescriptor.Properties[typeof(ProducesErrorResponseTypeAttribute)] = new ProducesErrorResponseTypeAttribute(typeof(InvalidTimeZoneException));

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.Equal(201, responseType.StatusCode);
                Assert.Equal(typeof(BaseModel), responseType.Type);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            },
            responseType =>
            {
                Assert.Equal(300, responseType.StatusCode);
                Assert.Equal(typeof(void), responseType.Type);
                Assert.Empty(responseType.ApiResponseFormats);
            },
            responseType =>
            {
                Assert.Equal(500, responseType.StatusCode);
                Assert.Equal(typeof(void), responseType.Type);
                Assert.Empty(responseType.ApiResponseFormats);
            });
    }

    [Fact]
    public void GetApiResponseTypes_AllowsUsingVoid()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(
             typeof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController),
             nameof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController.DeleteBase));
        actionDescriptor.Properties[typeof(ApiConventionResult)] = new ApiConventionResult(new IApiResponseMetadataProvider[]
        {
                new ProducesResponseTypeAttribute(typeof(InvalidCastException), 400),
                new ProducesResponseTypeAttribute(415),
                new ProducesDefaultResponseTypeAttribute(),
        });

        actionDescriptor.Properties[typeof(ProducesErrorResponseTypeAttribute)] = new ProducesErrorResponseTypeAttribute(typeof(void));

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.True(responseType.IsDefaultResponse);
                Assert.Equal(typeof(void), responseType.Type);
                Assert.Empty(responseType.ApiResponseFormats);
            },
            responseType =>
            {
                Assert.Equal(400, responseType.StatusCode);
                Assert.Equal(typeof(InvalidCastException), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            },
            responseType =>
            {
                Assert.Equal(415, responseType.StatusCode);
                Assert.Equal(typeof(void), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Empty(responseType.ApiResponseFormats);
            });
    }

    [Fact]
    public void GetApiResponseTypes_CombinesProducesAttributeAndConventions()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(typeof(TestController), nameof(TestController.PutModel));
        actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new ProducesAttribute("application/json"), FilterScope.Controller));
        actionDescriptor.Properties[typeof(ApiConventionResult)] = new ApiConventionResult(new IApiResponseMetadataProvider[]
        {
                new ProducesResponseTypeAttribute(200),
                new ProducesResponseTypeAttribute(400),
                new ProducesDefaultResponseTypeAttribute(),
        });
        actionDescriptor.Properties[typeof(ProducesErrorResponseTypeAttribute)] = new ProducesErrorResponseTypeAttribute(typeof(ProblemDetails));

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.True(responseType.IsDefaultResponse);
                Assert.Equal(typeof(ProblemDetails), responseType.Type);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            },
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(DerivedModel), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            },
            responseType =>
            {
                Assert.Equal(400, responseType.StatusCode);
                Assert.Equal(typeof(ProblemDetails), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            });
    }

    [Fact]
    public void GetApiResponseTypes_DoesNotCombineProducesAttributeThatSpecifiesType()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(typeof(TestController), nameof(TestController.PutModel));
        actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new ProducesAttribute("application/json") { Type = typeof(string) }, FilterScope.Controller));
        actionDescriptor.Properties[typeof(ApiConventionResult)] = new ApiConventionResult(new IApiResponseMetadataProvider[]
        {
                new ProducesResponseTypeAttribute(200),
                new ProducesResponseTypeAttribute(400),
                new ProducesDefaultResponseTypeAttribute(),
        });
        actionDescriptor.Properties[typeof(ProducesErrorResponseTypeAttribute)] = new ProducesErrorResponseTypeAttribute(typeof(ProblemDetails));

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(string), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            });
    }

    [Fact]
    public void GetApiResponseTypes_DoesNotCombineProducesResponseTypeAttributeThatSpecifiesStatusCode()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(typeof(TestController), nameof(TestController.PutModel));
        actionDescriptor.Properties[typeof(ApiConventionResult)] = new ApiConventionResult(new IApiResponseMetadataProvider[]
        {
                new ProducesResponseTypeAttribute(200),
        });
        actionDescriptor.Properties[typeof(ProducesErrorResponseTypeAttribute)] = new ProducesErrorResponseTypeAttribute(typeof(ProblemDetails));

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(DerivedModel), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format => Assert.Equal("application/json", format.MediaType));
            });
    }

    [Fact]
    public void GetApiResponseTypes_UsesContentTypeWithoutWildCard_WhenNoFormatterSupportsIt()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(typeof(TestController), nameof(TestController.GetUser));
        actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new ProducesAttribute("application/pdf"), FilterScope.Action));

        var provider = GetProvider();

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(DerivedModel), responseType.Type);
                Assert.False(responseType.IsDefaultResponse);
                Assert.Collection(
                    responseType.ApiResponseFormats,
                    format =>
                    {
                        Assert.Equal("application/pdf", format.MediaType);
                        Assert.Null(format.Formatter);
                    });
            });
    }

    [Fact]
    public void GetApiResponseTypes_HandlesActionWithMultipleContentTypesAndProduces()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(typeof(TestController), nameof(TestController.GetUser));
        actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new ProducesAttribute("text/xml") { Type = typeof(BaseModel) }, FilterScope.Action));
        actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(ValidationProblemDetails), 400, "application/problem+json"), FilterScope.Action));
        actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(ProblemDetails), 404, "application/problem+json"), FilterScope.Action));
        actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new ProducesResponseTypeAttribute(409), FilterScope.Action));

        var provider = new ApiResponseTypeProvider(new EmptyModelMetadataProvider(), new ActionResultTypeMapper(), new MvcOptions());

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.Equal(typeof(BaseModel), responseType.Type);
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(new[] { "text/xml" }, GetSortedMediaTypes(responseType));

            },
            responseType =>
            {
                Assert.Equal(typeof(ValidationProblemDetails), responseType.Type);
                Assert.Equal(400, responseType.StatusCode);
                Assert.Equal(new[] { "application/problem+json" }, GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(typeof(ProblemDetails), responseType.Type);
                Assert.Equal(404, responseType.StatusCode);
                Assert.Equal(new[] { "application/problem+json" }, GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(typeof(void), responseType.Type);
                Assert.Equal(409, responseType.StatusCode);
                Assert.Empty(GetSortedMediaTypes(responseType));
            });
    }

    [Fact]
    public void GetApiResponseTypes_PreservesMultipleProducesResponseTypeWithSameStatusCodeButDifferentTypesWithoutContentTypes()
    {
        var actionDescriptor = GetControllerActionDescriptor(typeof(TestController), nameof(TestController.GetMultipleTypes));
        var provider = new ApiResponseTypeProvider(new EmptyModelMetadataProvider(), new ActionResultTypeMapper(), new MvcOptions());

        var result = provider.GetApiResponseTypes(actionDescriptor);

        Assert.Collection(
            result,
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(BaseModel), responseType.Type);
            },
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(string), responseType.Type);
            });
    }

    [Fact]
    public void GetApiResponseTypes_ReturnNoResponseTypes_IfActionWithBuiltIResultReturnType()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(typeof(TestController), nameof(TestController.GetIResult));
        var provider = new ApiResponseTypeProvider(new EmptyModelMetadataProvider(), new ActionResultTypeMapper(), new MvcOptions());

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.False(result.Any());
    }

    [Fact]
    public void GetApiResponseTypes_ReturnResponseType_IfActionHasCustomIResultReturnTypeInMetadata()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(typeof(TestController), nameof(TestController.GetCustomIResult));
        actionDescriptor.EndpointMetadata = [new ProducesResponseTypeMetadata(200, typeof(MyResponse))];
        var provider = new ApiResponseTypeProvider(new EmptyModelMetadataProvider(), new ActionResultTypeMapper(), new MvcOptions());

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        var response = Assert.Single(result);
        Assert.Equal(typeof(MyResponse), response.Type);
        Assert.Equal(200, response.StatusCode);
    }

    private static ApiResponseTypeProvider GetProvider()
    {
        var mvcOptions = new MvcOptions
        {
            OutputFormatters = { new TestOutputFormatter() },
        };
        var provider = new ApiResponseTypeProvider(new EmptyModelMetadataProvider(), new ActionResultTypeMapper(), mvcOptions);
        return provider;
    }

    private static IEnumerable<string> GetSortedMediaTypes(ApiResponseType apiResponseType)
    {
        return apiResponseType.ApiResponseFormats
            .OrderBy(format => format.MediaType)
            .Select(format => format.MediaType);
    }

    private static ControllerActionDescriptor GetControllerActionDescriptor(Type type, string name)
    {
        var method = type.GetMethod(name);
        var actionDescriptor = new ControllerActionDescriptor
        {
            MethodInfo = method,
            FilterDescriptors = new List<FilterDescriptor>(),
        };

        foreach (var filterAttribute in method.GetCustomAttributes().OfType<IFilterMetadata>())
        {
            actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(filterAttribute, FilterScope.Action));
        }

        return actionDescriptor;
    }

    public class BaseModel { }

    public class DerivedModel : BaseModel { }

    public class TestController
    {
        public ActionResult<DerivedModel> GetUser(int id) => null;

        public ActionResult<DerivedModel> GetUserLocation(int a, int b) => null;

        public ActionResult<DerivedModel> PutModel(string userId, DerivedModel model) => null;

        public IResult GetIResult(int id) => null;

        [ProducesResponseType(typeof(BaseModel), 200)]
        [ProducesResponseType(typeof(string), 200)]
        public IResult GetMultipleTypes() => Results.Ok();

        public MyResponse GetCustomIResult() => new MyResponse { Content = "Test Content" };
    }

    public class MyResponse : IResult
    {
        public required string Content { get; set; }

        public Task ExecuteAsync(HttpContext httpContext)
        {
            return httpContext.Response.WriteAsJsonAsync(this);
        }
    }

    private class TestOutputFormatter : OutputFormatter
    {
        public TestOutputFormatter()
        {
            SupportedMediaTypes.Add(new Net.Http.Headers.MediaTypeHeaderValue("application/json"));
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context) => Task.CompletedTask;
    }

    [Fact]
    public void GetApiResponseTypes_PreservesMultipleProducesResponseTypeWithSameStatusCodeButDifferentContentTypes()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(
            typeof(MultipleProducesForSameStatusCodeController),
            nameof(MultipleProducesForSameStatusCodeController.Get));

        var provider = new ApiResponseTypeProvider(new EmptyModelMetadataProvider(), new ActionResultTypeMapper(), new MvcOptions());

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(BaseModel), responseType.Type);
                Assert.Equal(new[] { "application/json" }, GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(string), responseType.Type);
                Assert.Equal(new[] { "text/html" }, GetSortedMediaTypes(responseType));
            });
    }

    public class MultipleProducesForSameStatusCodeController : ControllerBase
    {
        [ProducesResponseType(typeof(BaseModel), 200, "application/json")]
        [ProducesResponseType(typeof(string), 200, "text/html")]
        public IActionResult Get() => null;
    }

    [Fact]
    public void GetApiResponseTypes_PreservesMultipleProducesResponseTypeFromEndpointMetadata()
    {
        // Arrange
        var actionDescriptor = GetControllerActionDescriptor(
            typeof(MultipleProducesForSameStatusCodeController),
            nameof(MultipleProducesForSameStatusCodeController.Get));
        actionDescriptor.EndpointMetadata =
        [
            new ProducesResponseTypeMetadata(200, typeof(BaseModel), ["application/json"]),
            new ProducesResponseTypeMetadata(200, typeof(string), ["text/html"]),
        ];

        var provider = new ApiResponseTypeProvider(new EmptyModelMetadataProvider(), new ActionResultTypeMapper(), new MvcOptions());

        // Act
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(BaseModel), responseType.Type);
                Assert.Equal(new[] { "application/json" }, GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(string), responseType.Type);
                Assert.Equal(new[] { "text/html" }, GetSortedMediaTypes(responseType));
            });
    }

    [Fact]
    public void GetApiResponseTypes_MergesContentTypesForSameStatusCodeAndTypeAtSameScope()
    {
        // Arrange — two [ProducesResponseType] for the same (200, BaseModel) at action scope with different content types.
        var actionDescriptor = GetControllerActionDescriptor(typeof(TestController), nameof(TestController.GetUser));
        actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(BaseModel), 200, "application/json"), FilterScope.Action));
        actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(BaseModel), 200, "text/xml"), FilterScope.Action));

        // Act
        var provider = new ApiResponseTypeProvider(new EmptyModelMetadataProvider(), new ActionResultTypeMapper(), new MvcOptions());
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert — single (200, BaseModel) with merged [json, xml]
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(BaseModel), responseType.Type);
                Assert.Equal(["application/json", "text/xml"], GetSortedMediaTypes(responseType));
            });
    }

    [Fact]
    public void GetApiResponseTypes_ProducesAttribute_HighestScopeWins()
    {
        // Arrange — [Produces("text/xml")] at controller scope and [Produces("application/json")] at action scope.
        // Action scope is higher, so "application/json" should be the shared content type.
        var actionDescriptor = GetControllerActionDescriptor(typeof(TestController), nameof(TestController.GetUser));
        actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new ProducesAttribute("text/xml"), FilterScope.Controller));
        actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new ProducesAttribute("application/json"), FilterScope.Action));

        // Act
        var provider = GetProvider();
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert — action-level "application/json" wins, controller-level "text/xml" is ignored
        Assert.Collection(
            result,
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(DerivedModel), responseType.Type);
                Assert.Equal(["application/json"], GetSortedMediaTypes(responseType));
            });
    }

    [Fact]
    public void GetApiResponseTypes_AttributesTakePrecedenceOverEndpointMetadata_ForOverlappingStatusCodes()
    {
        // Arrange — endpoint metadata provides (200, string, "text/html") and (404, ProblemDetails, "application/json").
        // Filter attribute claims status code 200 with (200, BaseModel, "application/json").
        // Attribute wins for 200, endpoint metadata 404 passes through.
        var actionDescriptor = GetControllerActionDescriptor(typeof(TestController), nameof(TestController.GetUser));
        actionDescriptor.EndpointMetadata =
        [
            new ProducesResponseTypeMetadata(200, typeof(string), ["text/html"]),
            new ProducesResponseTypeMetadata(404, typeof(ProblemDetails), ["application/json"]),
        ];
        actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(BaseModel), 200, "application/json"), FilterScope.Action));

        // Act
        var provider = new ApiResponseTypeProvider(new EmptyModelMetadataProvider(), new ActionResultTypeMapper(), new MvcOptions());
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert
        Assert.Collection(
            result,
            responseType =>
            {
                // Attribute wins for 200
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(BaseModel), responseType.Type);
                Assert.Equal(["application/json"], GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                // Endpoint metadata 404 passes through (not claimed by attributes)
                Assert.Equal(404, responseType.StatusCode);
                Assert.Equal(typeof(ProblemDetails), responseType.Type);
                Assert.Equal(["application/json"], GetSortedMediaTypes(responseType));
            });
    }

    [Fact]
    public void GetApiResponseTypes_DeterministicOrdering_ComplexScenario()
    {
        // [Produces("application/json")]                                   // controller, scope=20
        // [ProducesResponseType(typeof(Error), 404)]                       // controller, scope=20
        // public class MyController
        // {
        //     [ProducesResponseType(typeof(Foo), 200, "application/json")] // action, scope=10
        //     [ProducesResponseType(typeof(Bar), 200, "text/xml")]         // action, scope=10
        //     [ProducesResponseType(typeof(Foo), 200, "text/plain")]       // action, scope=10
        //     [ProducesResponseType(404)]                                  // action, scope=10
        //     public IActionResult Get() { ... }
        // }
        //
        // Expected output:
        //   200 BaseModel [application/json, text/plain] → Foo merged
        //   200 Bar [text/xml]
        //   404 void [] → action scope wins (void), controller 404 Error ignored

        // Arrange
        var filterDescriptors = new[]
        {
            // controller scope
            new FilterDescriptor(new ProducesAttribute("application/json"), FilterScope.Controller),
            new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(DerivedModel), 404), FilterScope.Controller),
            // action scope
            new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(BaseModel), 200, "application/json"), FilterScope.Action),
            new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(DerivedModel), 200, "text/xml"), FilterScope.Action),
            new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(BaseModel), 200, "text/plain"), FilterScope.Action),
            new FilterDescriptor(new ProducesResponseTypeAttribute(404), FilterScope.Action),
        };

        var actionDescriptor = new ControllerActionDescriptor
        {
            FilterDescriptors = filterDescriptors,
            MethodInfo = typeof(GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresentController)
                .GetMethod(nameof(GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresentController.Get)),
        };

        // Act
        var provider = new ApiResponseTypeProvider(new EmptyModelMetadataProvider(), new ActionResultTypeMapper(), new MvcOptions());
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert — ordered by StatusCode → Type.Name → first ContentType
        Assert.Collection(
            result,
            responseType =>
            {
                // 200 BaseModel [application/json, text/plain] — merged from two action-scope entries
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(BaseModel), responseType.Type);
                Assert.Equal(["application/json", "text/plain"], GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                // 200 DerivedModel [text/xml] — different type, added alongside
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(DerivedModel), responseType.Type);
                Assert.Equal(["text/xml"], GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                // 404 void — action scope wins over controller's (404, DerivedModel).
                // [Produces("application/json")] shared content type applied since no own formats.
                Assert.Equal(404, responseType.StatusCode);
                Assert.Equal(typeof(void), responseType.Type);
                Assert.Empty(responseType.ApiResponseFormats);
            });
    }

    [Fact]
    public void GetApiResponseTypes_DefaultFallback_VoidReturnType_Produces200WithNoFormats()
    {
        // Arrange — action returns void (Task), no attributes, no conventions.
        var actionDescriptor = new ControllerActionDescriptor
        {
            MethodInfo = typeof(VoidController).GetMethod(nameof(VoidController.Delete)),
            FilterDescriptors = [],
        };

        // Act
        var provider = GetProvider();
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert — default fallback produces (200, void) with empty formats
        var responseType = Assert.Single(result);
        Assert.Equal(200, responseType.StatusCode);
        Assert.Null(responseType.ModelMetadata);
        Assert.Empty(responseType.ApiResponseFormats);
    }

    [Fact]
    public void GetApiResponseTypes_HigherScopeProviderWithNullType_DoesNotBlockLowerScope()
    {
        // Arrange — a custom IApiResponseMetadataProvider at action scope returns Type=null for
        // status 404. Because the entry is dropped (Type stays null and the `if (Type != null)`
        // guard skips it), the action scope does NOT register a claim on status 404 in
        // statusCodeScopes. A controller-level [ProducesResponseType(typeof(DerivedModel), 404)]
        // is therefore allowed to fill in the entry. This locks down behavior for the corner case
        // where a custom provider intentionally yields no type information.
        var actionDescriptor = GetControllerActionDescriptor(typeof(TestController), nameof(TestController.GetUser));
        actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new NullTypeMetadataProvider(404), FilterScope.Action));
        actionDescriptor.FilterDescriptors.Add(new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(DerivedModel), 404), FilterScope.Controller));

        // Act
        var provider = GetProvider();
        var result = provider.GetApiResponseTypes(actionDescriptor);

        // Assert — controller-level (404, DerivedModel) survives because the higher-scope
        // null-Type provider produced no entry.
        Assert.Contains(result, r => r.StatusCode == 404 && r.Type == typeof(DerivedModel));
    }

    private sealed class NullTypeMetadataProvider : IApiResponseMetadataProvider
    {
        public NullTypeMetadataProvider(int statusCode) { StatusCode = statusCode; }
        public Type Type => null;
        public int StatusCode { get; }
        public string Description => null;
        public void SetContentTypes(MediaTypeCollection contentTypes) { }
    }

    public class VoidController : ControllerBase
    {
        public Task Delete() => Task.CompletedTask;
    }

    public static class SearchApiConventions
    {
        [ProducesResponseType(206)]
        [ProducesResponseType(406)]
        public static void Search(object searchTerm, int page) { }
    }
}
