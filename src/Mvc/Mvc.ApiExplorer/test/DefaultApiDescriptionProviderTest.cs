// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Constraints;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Description;

public class DefaultApiDescriptionProviderTest
{
    [Fact]
    public void GetApiDescription_IgnoresNonControllerActionDescriptor()
    {
        // Arrange
        var action = new ActionDescriptor();
        action.SetProperty(new ApiDescriptionActionData());

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        Assert.Empty(descriptions);
    }

    [Fact]
    public void GetApiDescription_IgnoresActionWithoutApiExplorerData()
    {
        // Arrange
        var action = new ControllerActionDescriptor();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        Assert.Empty(descriptions);
    }

    [Fact]
    public void GetApiDescription_PopulatesActionDescriptor()
    {
        // Arrange
        var action = CreateActionDescriptor();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Same(action, description.ActionDescriptor);
    }

    [Fact]
    public void GetApiDescription_PopulatesGroupName()
    {
        // Arrange
        var action = CreateActionDescriptor();
        action.GetProperty<ApiDescriptionActionData>().GroupName = "Customers";

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal("Customers", description.GroupName);
    }

    [Fact]
    public void GetApiDescription_PopulatesGroupName_FromMetadata()
    {
        // Arrange
        var action = CreateActionDescriptor();
        action.EndpointMetadata = new List<object>() { new EndpointGroupNameAttribute("Customers") };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal("Customers", description.GroupName);
    }

    [Fact]
    public void GetApiDescription_PopulatesGroupName_FromMetadataOrExtensionData()
    {
        // Arrange
        var action = CreateActionDescriptor();
        action.EndpointMetadata = new List<object>() { new EndpointGroupNameAttribute("Customers") };
        action.GetProperty<ApiDescriptionActionData>().GroupName = "NotUsedCustomers";

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal("Customers", description.GroupName);
    }

    [Fact]
    public void GetApiDescription_HttpMethodIsNullWithoutConstraint()
    {
        // Arrange
        var action = CreateActionDescriptor();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Null(description.HttpMethod);
    }

    [Fact]
    public void GetApiDescription_CreatesMultipleDescriptionsForMultipleHttpMethods()
    {
        // Arrange
        var action = CreateActionDescriptor();
        action.ActionConstraints = new List<IActionConstraintMetadata>()
            {
                new HttpMethodActionConstraint(new string[] { "PUT", "POST" }),
                new HttpMethodActionConstraint(new string[] { "GET" }),
            };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        Assert.Equal(3, descriptions.Count);

        Assert.Single(descriptions, d => d.HttpMethod == "PUT");
        Assert.Single(descriptions, d => d.HttpMethod == "POST");
        Assert.Single(descriptions, d => d.HttpMethod == "GET");
    }

    [Theory]
    [InlineData("api/products/{id}", false, null, null)]
    [InlineData("api/products/{id?}", true, null, null)]
    [InlineData("api/products/{id=5}", true, null, "5")]
    [InlineData("api/products/{id:int}", false, typeof(IntRouteConstraint), null)]
    [InlineData("api/products/{id:int?}", true, typeof(IntRouteConstraint), null)]
    [InlineData("api/products/{id:int=5}", true, null, "5")]
    [InlineData("api/products/{*id}", false, null, null)]
    [InlineData("api/products/{*id:int}", false, typeof(IntRouteConstraint), null)]
    [InlineData("api/products/{*id:int=5}", true, typeof(IntRouteConstraint), "5")]
    public void GetApiDescription_PopulatesParameters_ThatAppearOnlyOnRouteTemplate(
        string template,
        bool isOptional,
        Type constraintType,
        object defaultValue)
    {
        // Arrange
        var action = CreateActionDescriptor();
        action.AttributeRouteInfo = new AttributeRouteInfo { Template = template };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var parameter = Assert.Single(description.ParameterDescriptions);
        Assert.Equal(BindingSource.Path, parameter.Source);
        Assert.Equal(isOptional, parameter.RouteInfo.IsOptional);
        Assert.Equal("id", parameter.Name);

        if (constraintType != null)
        {
            Assert.IsType(constraintType, Assert.Single(parameter.RouteInfo.Constraints));
        }

        if (defaultValue != null)
        {
            Assert.Equal(defaultValue, parameter.RouteInfo.DefaultValue);
        }
        else
        {
            Assert.Null(parameter.RouteInfo.DefaultValue);
        }
    }

    [Theory]
    [InlineData("api/products/{id}", false, null, null)]
    [InlineData("api/products/{id?}", true, null, null)]
    [InlineData("api/products/{id=5}", true, null, "5")]
    [InlineData("api/products/{id:int}", false, typeof(IntRouteConstraint), null)]
    [InlineData("api/products/{id:int?}", true, typeof(IntRouteConstraint), null)]
    [InlineData("api/products/{id:int=5}", true, typeof(IntRouteConstraint), "5")]
    [InlineData("api/products/{*id}", false, null, null)]
    [InlineData("api/products/{*id:int}", false, typeof(IntRouteConstraint), null)]
    [InlineData("api/products/{*id:int=5}", true, typeof(IntRouteConstraint), "5")]
    public void GetApiDescription_PopulatesParametersThatAppearOnRouteTemplate_AndHaveAssociatedParameterDescriptor(
        string template,
        bool isOptional,
        Type constraintType,
        object defaultValue)
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(FromRouting));
        action.AttributeRouteInfo = new AttributeRouteInfo { Template = template };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var parameter = Assert.Single(description.ParameterDescriptions);
        Assert.Equal(BindingSource.Path, parameter.Source);
        Assert.Equal(isOptional, parameter.RouteInfo.IsOptional);
        Assert.Equal("id", parameter.Name);

        if (constraintType != null)
        {
            Assert.IsType(constraintType, Assert.Single(parameter.RouteInfo.Constraints));
        }

        if (defaultValue != null)
        {
            Assert.Equal(defaultValue, parameter.RouteInfo.DefaultValue);
        }
        else
        {
            Assert.Null(parameter.RouteInfo.DefaultValue);
        }
    }

    [Fact]
    public void GetApiDescription_WithInferredBindingSource_ExcludesPathParametersWhenNotPresentInRoute()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(FromModelBinding));
        action.AttributeRouteInfo = new AttributeRouteInfo { Template = "api/products" };

        action.Parameters[0].BindingInfo = new BindingInfo()
        {
            BindingSource = BindingSource.Path
        };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Empty(description.ParameterDescriptions);
    }

    [Theory]
    [InlineData("api/products/{id}")]
    [InlineData("api/products/{id?}")]
    [InlineData("api/products/{id=5}")]
    [InlineData("api/products/{id:int}")]
    [InlineData("api/products/{id:int?}")]
    [InlineData("api/products/{id:int=5}")]
    [InlineData("api/products/{*id}")]
    [InlineData("api/products/{*id:int}")]
    [InlineData("api/products/{*id:int=5}")]
    public void GetApiDescription_WithInferredBindingSource_IncludesPathParametersWhenPresentInRoute(string template)
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(FromModelBinding));
        action.AttributeRouteInfo = new AttributeRouteInfo { Template = template };

        action.Parameters[0].BindingInfo = new BindingInfo()
        {
            BindingSource = BindingSource.Path
        };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var parameter = Assert.Single(description.ParameterDescriptions);
        Assert.Equal(BindingSource.Path, parameter.Source);
        Assert.Equal("id", parameter.Name);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_IncludesParameterDescriptor()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(FromBody));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var parameterDescription = Assert.Single(description.ParameterDescriptions);
        var actionParameterDescriptor = Assert.Single(action.Parameters);
        Assert.Equal(actionParameterDescriptor, parameterDescription.ParameterDescriptor);
    }

    // Only a parameter which comes from a route or model binding or unknown should
    // include route info.
    [Theory]
    [InlineData("api/products/{id}", nameof(FromBody), "Body")]
    [InlineData("api/products/{id}", nameof(FromHeader), "Header")]
    public void GetApiDescription_ParameterDescription_DoesNotIncludeRouteInfo(
        string template,
        string methodName,
        string source)
    {
        // Arrange
        var action = CreateActionDescriptor(methodName);
        action.AttributeRouteInfo = new AttributeRouteInfo { Template = template };

        var expected = new BindingSource(source, displayName: null, isGreedy: false, isFromRequest: false);

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var parameters = description.ParameterDescriptions;

        var id = Assert.Single(parameters, p => p.Source == expected);
        Assert.Null(id.RouteInfo);
    }

    // Only a parameter which comes from a route or model binding or unknown should
    // include route info. If the source is model binding, we also check if it's an optional
    // parameter, and only change the source if it's a match.
    [Theory]
    [InlineData("api/products/{id}", nameof(FromRouting), "Path")]
    [InlineData("api/products/{id}", nameof(FromModelBinding), "Path")]
    [InlineData("api/products/{id?}", nameof(FromModelBinding), "ModelBinding")]
    [InlineData("api/products/{id=5}", nameof(FromModelBinding), "ModelBinding")]
    [InlineData("api/products/{id}", nameof(FromCustom), "Custom")]
    public void GetApiDescription_ParameterDescription_IncludesRouteInfo(
        string template,
        string methodName,
        string source)
    {
        // Arrange
        var action = CreateActionDescriptor(methodName);
        action.AttributeRouteInfo = new AttributeRouteInfo { Template = template };

        var expected = new BindingSource(source, displayName: null, isGreedy: false, isFromRequest: false);

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var parameters = description.ParameterDescriptions;

        var id = Assert.Single(parameters, p => p.Source == expected);
        Assert.NotNull(id.RouteInfo);
    }

    [Theory]
    [InlineData("api/products/{id}", false)]
    [InlineData("api/products/{id?}", true)]
    [InlineData("api/products/{id=5}", true)]
    public void GetApiDescription_ParameterFromPathAndDescriptor_IsOptionalIfRouteParameterIsOptional(
        string template,
        bool expectedOptional)
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(FromRouting));
        action.AttributeRouteInfo = new AttributeRouteInfo { Template = template };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var parameter = Assert.Single(description.ParameterDescriptions);
        Assert.Equal(expectedOptional, parameter.RouteInfo.IsOptional);
    }

    [Theory]
    [InlineData("api/Products/{id}", "api/Products/{id}")]
    [InlineData("api/Products/{id?}", "api/Products/{id}")]
    [InlineData("api/Products/{id:int}", "api/Products/{id}")]
    [InlineData("api/Products/{id:int?}", "api/Products/{id}")]
    [InlineData("api/Products/{*id}", "api/Products/{id}")]
    [InlineData("api/Products/{*id:int}", "api/Products/{id}")]
    [InlineData("api/Products/{id1}-{id2:int}", "api/Products/{id1}-{id2}")]
    [InlineData("api/{id1}/{id2?}/{id3:int}/{id4:int?}/{*id5:int}", "api/{id1}/{id2}/{id3}/{id4}/{id5}")]
    public void GetApiDescription_PopulatesRelativePath(string template, string relativePath)
    {
        // Arrange
        var action = CreateActionDescriptor();
        action.AttributeRouteInfo = new AttributeRouteInfo
        {
            Template = template
        };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(relativePath, description.RelativePath);
    }

    [Fact]
    public void GetApiDescription_DetectsMultipleParameters_OnTheSameSegment()
    {
        // Arrange
        var action = CreateActionDescriptor();
        action.AttributeRouteInfo = new AttributeRouteInfo
        {
            Template = "api/Products/{id1}-{id2:int}"
        };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var id1 = Assert.Single(description.ParameterDescriptions, p => p.Name == "id1");
        Assert.Equal(BindingSource.Path, id1.Source);
        Assert.Empty(id1.RouteInfo.Constraints);

        var id2 = Assert.Single(description.ParameterDescriptions, p => p.Name == "id2");
        Assert.Equal(BindingSource.Path, id2.Source);
        Assert.IsType<IntRouteConstraint>(Assert.Single(id2.RouteInfo.Constraints));
    }

    [Fact]
    public void GetApiDescription_DetectsMultipleParameters_OnDifferentSegments()
    {
        // Arrange
        var action = CreateActionDescriptor();
        action.AttributeRouteInfo = new AttributeRouteInfo
        {
            Template = "api/Products/{id1}-{id2}/{id3:int}/{id4:int?}/{*id5:int}"
        };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        Assert.Single(description.ParameterDescriptions, p => p.Name == "id1");
        Assert.Single(description.ParameterDescriptions, p => p.Name == "id2");
        Assert.Single(description.ParameterDescriptions, p => p.Name == "id3");
        Assert.Single(description.ParameterDescriptions, p => p.Name == "id4");
        Assert.Single(description.ParameterDescriptions, p => p.Name == "id5");
    }

    [Fact]
    public void GetApiDescription_ProducesLowerCaseRelativePaths()
    {
        // Arrange
        var action = CreateActionDescriptor();
        action.AttributeRouteInfo = new AttributeRouteInfo
        {
            Template = "api/Products/UpdateProduct/{productId}"
        };
        var routeOptions = new RouteOptions { LowercaseUrls = true };

        // Act
        var descriptions = GetApiDescriptions(action, routeOptions: routeOptions);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal("api/products/updateproduct/{productId}", description.RelativePath);
    }

    [Fact]
    public void GetApiDescription_PopulatesResponseType_WithProduct()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(ReturnsProduct));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var responseType = Assert.Single(description.SupportedResponseTypes);
        Assert.Equal(typeof(Product), responseType.Type);
        Assert.NotNull(responseType.ModelMetadata);
    }

    [Theory]
    [InlineData(nameof(ReturnsActionResultOfProduct))]
    [InlineData(nameof(ReturnsTaskOfActionResultOfProduct))]
    public void GetApiDescription_PopulatesResponseType_ForActionResultOfT(string methodName)
    {
        // Arrange
        var action = CreateActionDescriptor(methodName);

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var responseType = Assert.Single(description.SupportedResponseTypes);
        Assert.Equal(typeof(Product), responseType.Type);
        Assert.NotNull(responseType.ModelMetadata);
    }

    [Theory]
    [InlineData(nameof(ReturnsResultOfProductWithEndpointMetadata))]
    [InlineData(nameof(ReturnsTaskOfResultOfProductWithEndpointMetadata))]
    public void GetApiDescription_PopulatesResponseType_ForResultOfT_WithEndpointMetadata(string methodName)
    {
        // Arrange
        var action = CreateActionDescriptor(methodName);
        action.EndpointMetadata = new List<object>() { new ProducesResponseTypeMetadata(200, typeof(Product)) };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var responseType = Assert.Single(description.SupportedResponseTypes);
        Assert.Equal(typeof(Product), responseType.Type);
        Assert.NotNull(responseType.ModelMetadata);
    }

    [Theory]
    [InlineData(nameof(ReturnsResultOfProductWithEndpointMetadata))]
    [InlineData(nameof(ReturnsTaskOfResultOfProductWithEndpointMetadata))]
    public void GetApiDescription_PopulatesResponseType_ForResultOfT_WithEndpointMetadata_PreferProducesAttribute(string methodName)
    {
        // Arrange
        var action = CreateActionDescriptor(methodName);
        action.EndpointMetadata = new List<object>() { new ProducesResponseTypeMetadata(200, typeof(Product)) };
        action.FilterDescriptors = new List<FilterDescriptor>{
            new FilterDescriptor(new ProducesResponseTypeAttribute(typeof(Customer), 200), FilterScope.Action)
        };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var responseType = Assert.Single(description.SupportedResponseTypes);
        Assert.Equal(typeof(Customer), responseType.Type);
        Assert.NotNull(responseType.ModelMetadata);
    }

    [Theory]
    [InlineData(nameof(ReturnsActionResultOfSequenceOfProducts))]
    [InlineData(nameof(ReturnsTaskOfActionResultOfSequenceOfProducts))]
    public void GetApiDescription_PopulatesResponseType_ForActionResultOfSequenceOfT(string methodName)
    {
        // Arrange
        var action = CreateActionDescriptor(methodName);

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var responseType = Assert.Single(description.SupportedResponseTypes);
        Assert.Equal(typeof(IEnumerable<Product>), responseType.Type);
        Assert.NotNull(responseType.ModelMetadata);
    }

    [Fact]
    public void GetApiDescription_PopulatesResponseType_WithTaskOfProduct()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(ReturnsTaskOfProduct));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var responseType = Assert.Single(description.SupportedResponseTypes);
        Assert.Equal(typeof(Product), responseType.Type);
        Assert.NotNull(responseType.ModelMetadata);
    }

    [Fact]
    public void GetApiDescription_PopulatesResponseType_WithValueTaskOfProduct()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(ReturnsValueTaskOfProduct));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var responseType = Assert.Single(description.SupportedResponseTypes);
        Assert.Equal(typeof(Product), responseType.Type);
        Assert.NotNull(responseType.ModelMetadata);
    }

    [Theory]
    [InlineData(nameof(ReturnsObject))]
    [InlineData(nameof(ReturnsActionResult))]
    [InlineData(nameof(ReturnsJsonResult))]
    [InlineData(nameof(ReturnsTaskOfObject))]
    [InlineData(nameof(ReturnsTaskOfActionResult))]
    [InlineData(nameof(ReturnsTaskOfJsonResult))]
    [InlineData(nameof(ReturnsValueTaskOfObject))]
    [InlineData(nameof(ReturnsValueTaskOfActionResult))]
    [InlineData(nameof(ReturnsValueTaskOfJsonResult))]
    public void GetApiDescription_DoesNotPopulatesResponseInformation_WhenUnknown(string methodName)
    {
        // Arrange
        var action = CreateActionDescriptor(methodName);

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Empty(description.SupportedResponseTypes);
    }

    public static TheoryData ReturnsActionResultWithProducesAndProducesContentTypeData
    {
        get
        {
            var filterDescriptors = new List<FilterDescriptor>()
                {
                    new FilterDescriptor(
                        new ProducesAttribute("text/json", "application/json") { Type = typeof(Customer) },
                        FilterScope.Action),
                    new FilterDescriptor(
                        new ProducesResponseTypeAttribute(304),
                        FilterScope.Action),
                    new FilterDescriptor(
                        new ProducesResponseTypeAttribute(typeof(BadData), 400),
                        FilterScope.Action),
                    new FilterDescriptor(
                        new ProducesResponseTypeAttribute(typeof(ErrorDetails), 500),
                        FilterScope.Action),
                };

            return new TheoryData<Type, string, List<FilterDescriptor>>
                {
                    {
                        typeof(DefaultApiDescriptionProviderTest),
                        nameof(DefaultApiDescriptionProviderTest.ReturnsTaskOfActionResult),
                        filterDescriptors
                    },
                    {
                        typeof(DefaultApiDescriptionProviderTest),
                        nameof(DefaultApiDescriptionProviderTest.ReturnsValueTaskOfActionResult),
                        filterDescriptors
                    },
                    {
                        typeof(DefaultApiDescriptionProviderTest),
                        nameof(DefaultApiDescriptionProviderTest.ReturnsActionResult),
                        filterDescriptors
                    },
                    {
                        typeof(DerivedProducesController),
                        nameof(DerivedProducesController.ReturnsActionResult),
                        filterDescriptors
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(ReturnsActionResultWithProducesAndProducesContentTypeData))]
    public void GetApiDescription_ReturnsActionResultWithProduces_And_ProducesContentType(
        Type controllerType,
        string methodName,
        List<FilterDescriptor> filterDescriptors)
    {
        // Arrange
        var action = CreateActionDescriptor(methodName, controllerType);
        action.FilterDescriptors = filterDescriptors;
        var expectedMediaTypes = new[] { "application/json", "text/json" };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(4, description.SupportedResponseTypes.Count);

        Assert.Collection(
            description.SupportedResponseTypes.OrderBy(responseType => responseType.StatusCode),
            responseType =>
            {
                Assert.Equal(200, responseType.StatusCode);
                Assert.Equal(typeof(Customer), responseType.Type);
                Assert.NotNull(responseType.ModelMetadata);
                Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(304, responseType.StatusCode);
                Assert.Equal(typeof(void), responseType.Type);
                Assert.Null(responseType.ModelMetadata);
                Assert.Empty(responseType.ApiResponseFormats);
            },
            responseType =>
            {
                Assert.Equal(400, responseType.StatusCode);
                Assert.Equal(typeof(BadData), responseType.Type);
                Assert.NotNull(responseType.ModelMetadata);
                Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(500, responseType.StatusCode);
                Assert.Equal(typeof(ErrorDetails), responseType.Type);
                Assert.NotNull(responseType.ModelMetadata);
                Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
            });
    }

    public static TheoryData<Type, string, List<FilterDescriptor>> ReturnsVoidOrTaskWithProducesContentTypeData
    {
        get
        {
            var filterDescriptors = new List<FilterDescriptor>()
                {
                    // Since action is returning Void or Task, it does not make sense to provide a value for the
                    // 'Type' property to ProducesAttribute. But the same action could return other types of data
                    // based on runtime conditions.
                    new FilterDescriptor(
                        new ProducesAttribute("text/json", "application/json"),
                        FilterScope.Action),
                    new FilterDescriptor(
                        new ProducesResponseTypeAttribute(200),
                        FilterScope.Action),
                    new FilterDescriptor(
                        new ProducesResponseTypeAttribute(typeof(BadData), 400),
                        FilterScope.Action),
                    new FilterDescriptor(
                        new ProducesResponseTypeAttribute(typeof(ErrorDetails), 500),
                        FilterScope.Action)
                };

            return new TheoryData<Type, string, List<FilterDescriptor>>
                {
                    {
                        typeof(DefaultApiDescriptionProviderTest),
                        nameof(DefaultApiDescriptionProviderTest.ReturnsVoid),
                        filterDescriptors
                    },
                    {
                        typeof(DefaultApiDescriptionProviderTest),
                        nameof(DefaultApiDescriptionProviderTest.ReturnsTask),
                        filterDescriptors
                    },
                    {
                        typeof(DefaultApiDescriptionProviderTest),
                        nameof(DefaultApiDescriptionProviderTest.ReturnsValueTask),
                        filterDescriptors
                    },
                    {
                        typeof(DerivedProducesController),
                        nameof(DerivedProducesController.ReturnsVoid),
                        filterDescriptors
                    },
                    {
                        typeof(DerivedProducesController),
                        nameof(DerivedProducesController.ReturnsTask),
                        filterDescriptors
                    },
                    {
                        typeof(DerivedProducesController),
                        nameof(DerivedProducesController.ReturnsValueTask),
                        filterDescriptors
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(ReturnsVoidOrTaskWithProducesContentTypeData))]
    public void GetApiDescription_ReturnsVoidWithProducesContentType(
        Type controllerType,
        string methodName,
        List<FilterDescriptor> filterDescriptors)
    {
        // Arrange
        var action = CreateActionDescriptor(methodName, controllerType);
        action.FilterDescriptors = filterDescriptors;
        var expectedMediaTypes = new[] { "application/json", "text/json" };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(3, description.SupportedResponseTypes.Count);

        Assert.Collection(
            description.SupportedResponseTypes.OrderBy(responseType => responseType.StatusCode),
            responseType =>
            {
                Assert.Equal(typeof(void), responseType.Type);
                Assert.Equal(200, responseType.StatusCode);
                Assert.Null(responseType.ModelMetadata);
                Assert.Empty(responseType.ApiResponseFormats);
            },
            responseType =>
            {
                Assert.Equal(typeof(BadData), responseType.Type);
                Assert.Equal(400, responseType.StatusCode);
                Assert.NotNull(responseType.ModelMetadata);
                Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(typeof(ErrorDetails), responseType.Type);
                Assert.Equal(500, responseType.StatusCode);
                Assert.NotNull(responseType.ModelMetadata);
                Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
            });
    }

    [Theory]
    [InlineData(nameof(ReturnsActionResultOfProduct))]
    [InlineData(nameof(ReturnsTaskOfActionResultOfProduct))]
    public void GetApiDescription_ReturnsActionResultOfTWithProducesContentType(
        string methodName)
    {
        // Arrange
        var action = CreateActionDescriptor(methodName);
        action.FilterDescriptors = new List<FilterDescriptor>()
            {
                // Since action is returning Void or Task, it does not make sense to provide a value for the
                // 'Type' property to ProducesAttribute. But the same action could return other types of data
                // based on runtime conditions.
                new FilterDescriptor(
                    new ProducesAttribute("text/json", "application/json"),
                    FilterScope.Action),
                new FilterDescriptor(
                    new ProducesResponseTypeAttribute(200),
                    FilterScope.Action),
                new FilterDescriptor(
                    new ProducesResponseTypeAttribute(202),
                    FilterScope.Action),
                new FilterDescriptor(
                    new ProducesResponseTypeAttribute(typeof(BadData), 400),
                    FilterScope.Action),
                new FilterDescriptor(
                    new ProducesResponseTypeAttribute(typeof(ErrorDetails), 500),
                    FilterScope.Action)
            };
        var expectedMediaTypes = new[] { "application/json", "text/json" };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(4, description.SupportedResponseTypes.Count);

        Assert.Collection(
            description.SupportedResponseTypes.OrderBy(responseType => responseType.StatusCode),
            responseType =>
            {
                Assert.Equal(typeof(Product), responseType.Type);
                Assert.Equal(200, responseType.StatusCode);
                Assert.NotNull(responseType.ModelMetadata);
                Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(typeof(void), responseType.Type);
                Assert.Equal(202, responseType.StatusCode);
                Assert.Null(responseType.ModelMetadata);
                Assert.Empty(GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(typeof(BadData), responseType.Type);
                Assert.Equal(400, responseType.StatusCode);
                Assert.NotNull(responseType.ModelMetadata);
                Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(typeof(ErrorDetails), responseType.Type);
                Assert.Equal(500, responseType.StatusCode);
                Assert.NotNull(responseType.ModelMetadata);
                Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
            });
    }

    [Theory]
    [InlineData(nameof(ReturnsActionResultOfProduct))]
    [InlineData(nameof(ReturnsTaskOfActionResultOfProduct))]
    public void GetApiDescription_ReturnsActionResultOfTWithProducesContentType_ForStatusCode201(
        string methodName)
    {
        // Arrange
        var action = CreateActionDescriptor(methodName);
        action.FilterDescriptors = new List<FilterDescriptor>()
            {
                // Since action is returning Void or Task, it does not make sense to provide a value for the
                // 'Type' property to ProducesAttribute. But the same action could return other types of data
                // based on runtime conditions.
                new FilterDescriptor(
                    new ProducesAttribute("text/json", "application/json"),
                    FilterScope.Action),
                new FilterDescriptor(
                    new ProducesResponseTypeAttribute(201),
                    FilterScope.Action),
                new FilterDescriptor(
                    new ProducesResponseTypeAttribute(204),
                    FilterScope.Action),
                new FilterDescriptor(
                    new ProducesResponseTypeAttribute(typeof(BadData), 400),
                    FilterScope.Action),
                new FilterDescriptor(
                    new ProducesResponseTypeAttribute(typeof(ErrorDetails), 500),
                    FilterScope.Action)
            };
        var expectedMediaTypes = new[] { "application/json", "text/json" };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(4, description.SupportedResponseTypes.Count);

        Assert.Collection(
            description.SupportedResponseTypes.OrderBy(responseType => responseType.StatusCode),
            responseType =>
            {
                Assert.Equal(typeof(Product), responseType.Type);
                Assert.Equal(201, responseType.StatusCode);
                Assert.NotNull(responseType.ModelMetadata);
                Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(typeof(void), responseType.Type);
                Assert.Equal(204, responseType.StatusCode);
                Assert.Null(responseType.ModelMetadata);
                Assert.Empty(GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(typeof(BadData), responseType.Type);
                Assert.Equal(400, responseType.StatusCode);
                Assert.NotNull(responseType.ModelMetadata);
                Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(typeof(ErrorDetails), responseType.Type);
                Assert.Equal(500, responseType.StatusCode);
                Assert.NotNull(responseType.ModelMetadata);
                Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
            });
    }

    [Theory]
    [InlineData(nameof(ReturnsActionResultOfSequenceOfProducts))]
    [InlineData(nameof(ReturnsTaskOfActionResultOfSequenceOfProducts))]
    public void GetApiDescription_ReturnsActionResultOfSequenceOfTWithProducesContentType(
        string methodName)
    {
        // Arrange
        var action = CreateActionDescriptor(methodName);
        action.FilterDescriptors = new List<FilterDescriptor>()
            {
                // Since action is returning Void or Task, it does not make sense to provide a value for the
                // 'Type' property to ProducesAttribute. But the same action could return other types of data
                // based on runtime conditions.
                new FilterDescriptor(
                    new ProducesAttribute("text/json", "application/json"),
                    FilterScope.Action),
                new FilterDescriptor(
                    new ProducesResponseTypeAttribute(200),
                    FilterScope.Action),
                new FilterDescriptor(
                    new ProducesResponseTypeAttribute(201),
                    FilterScope.Action),
                new FilterDescriptor(
                    new ProducesResponseTypeAttribute(typeof(BadData), 400),
                    FilterScope.Action),
                new FilterDescriptor(
                    new ProducesResponseTypeAttribute(typeof(ErrorDetails), 500),
                    FilterScope.Action)
            };
        var expectedMediaTypes = new[] { "application/json", "text/json" };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(4, description.SupportedResponseTypes.Count);

        Assert.Collection(
            description.SupportedResponseTypes.OrderBy(responseType => responseType.StatusCode),
            responseType =>
            {
                Assert.Equal(typeof(IEnumerable<Product>), responseType.Type);
                Assert.Equal(200, responseType.StatusCode);
                Assert.NotNull(responseType.ModelMetadata);
                Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(typeof(IEnumerable<Product>), responseType.Type);
                Assert.Equal(201, responseType.StatusCode);
                Assert.NotNull(responseType.ModelMetadata);
                Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(typeof(BadData), responseType.Type);
                Assert.Equal(400, responseType.StatusCode);
                Assert.NotNull(responseType.ModelMetadata);
                Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
            },
            responseType =>
            {
                Assert.Equal(typeof(ErrorDetails), responseType.Type);
                Assert.Equal(500, responseType.StatusCode);
                Assert.NotNull(responseType.ModelMetadata);
                Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
            });
    }

    [Theory]
    [InlineData(nameof(ReturnsVoid))]
    [InlineData(nameof(ReturnsTask))]
    [InlineData(nameof(ReturnsValueTask))]
    public void GetApiDescription_DefaultVoidStatus(string methodName)
    {
        // Arrange
        var action = CreateActionDescriptor(methodName);

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var responseType = Assert.Single(description.SupportedResponseTypes);
        Assert.Equal(typeof(void), responseType.Type);
        Assert.Equal(200, responseType.StatusCode);
        Assert.Null(responseType.ModelMetadata);
    }

    [Theory]
    [InlineData(nameof(ReturnsVoid))]
    [InlineData(nameof(ReturnsTask))]
    [InlineData(nameof(ReturnsValueTask))]
    public void GetApiDescription_VoidWithResponseTypeAttributeStatus(string methodName)
    {
        // Arrange
        var action = CreateActionDescriptor(methodName);
        var filter = new ProducesResponseTypeAttribute(typeof(void), statusCode: 204);
        action.FilterDescriptors = new List<FilterDescriptor>
            {
                new FilterDescriptor(filter, FilterScope.Action)
            };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var responseType = Assert.Single(description.SupportedResponseTypes);
        Assert.Equal(typeof(void), responseType.Type);
        Assert.Equal(204, responseType.StatusCode);
        Assert.Null(responseType.ModelMetadata);
    }

    [Theory]
    [InlineData(nameof(ReturnsObject))]
    [InlineData(nameof(ReturnsVoid))]
    [InlineData(nameof(ReturnsActionResult))]
    [InlineData(nameof(ReturnsJsonResult))]
    [InlineData(nameof(ReturnsTaskOfObject))]
    [InlineData(nameof(ReturnsTask))]
    [InlineData(nameof(ReturnsTaskOfActionResult))]
    [InlineData(nameof(ReturnsTaskOfJsonResult))]
    [InlineData(nameof(ReturnsValueTask))]
    [InlineData(nameof(ReturnsValueTaskOfObject))]
    [InlineData(nameof(ReturnsValueTaskOfActionResult))]
    [InlineData(nameof(ReturnsValueTaskOfJsonResult))]
    public void GetApiDescription_PopulatesResponseInformation_WhenSetByFilter(string methodName)
    {
        // Arrange
        var action = CreateActionDescriptor(methodName);
        var filter = new ContentTypeAttribute("text/*")
        {
            Type = typeof(Order)
        };

        action.FilterDescriptors = new List<FilterDescriptor>
            {
                new FilterDescriptor(filter, FilterScope.Action)
            };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var responseTypes = Assert.Single(description.SupportedResponseTypes);
        Assert.NotNull(responseTypes.ModelMetadata);
        Assert.Equal(200, responseTypes.StatusCode);
        Assert.Equal(typeof(Order), responseTypes.Type);

        foreach (var responseFormat in responseTypes.ApiResponseFormats)
        {
            Assert.StartsWith("text/", responseFormat.MediaType);
        }
    }

    [Fact]
    public void GetApiDescription_IncludesResponseFormats()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(ReturnsProduct));
        var expectedMediaTypes = new[] { "application/json", "application/xml", "text/json", "text/xml" };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var responseType = Assert.Single(description.SupportedResponseTypes);
        Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
    }

    [Fact]
    public void GetApiDescription_IncludesResponseFormats_FilteredByAttribute()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(ReturnsProduct));
        var expectedMediaTypes = new[] { "text/json", "text/xml" };
        action.FilterDescriptors = new List<FilterDescriptor>
            {
                new FilterDescriptor(new ContentTypeAttribute("text/*"), FilterScope.Action)
            };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var responseType = Assert.Single(description.SupportedResponseTypes);
        Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
    }

    [Fact]
    public void GetApiDescription_IncludesResponseFormats_FilteredByType()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(ReturnsObject));
        var filter = new ContentTypeAttribute("text/*")
        {
            Type = typeof(Order)
        };

        action.FilterDescriptors = new List<FilterDescriptor>
            {
                new FilterDescriptor(filter, FilterScope.Action)
            };

        var formatters = CreateOutputFormatters();

        // This will just format Order
        formatters[0].SupportedTypes.Add(typeof(Order));

        // This will just format Product
        formatters[1].SupportedTypes.Add(typeof(Product));

        // Act
        var descriptions = GetApiDescriptions(action, outputFormatters: formatters);

        // Assert
        var description = Assert.Single(descriptions);
        var responseType = Assert.Single(description.SupportedResponseTypes);
        Assert.Equal(typeof(Order), responseType.Type);
        Assert.NotNull(responseType.ModelMetadata);
        var apiResponseFormat = Assert.Single(
            responseType.ApiResponseFormats.Where(responseFormat => responseFormat.MediaType == "text/json"));
        Assert.Same(formatters[0], apiResponseFormat.Formatter);
    }

    [Fact]
    public void GetApiDescription_RequestFormatsEmpty_WithNoBodyParameter()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsProduct));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Empty(description.SupportedRequestFormats);
    }

    [Fact]
    public void GetApiDescription_IncludesRequestFormats()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsProduct_Body));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Collection(
            description.SupportedRequestFormats.OrderBy(f => f.MediaType.ToString()),
            f => Assert.Equal("application/json", f.MediaType.ToString()),
            f => Assert.Equal("application/xml", f.MediaType.ToString()),
            f => Assert.Equal("text/json", f.MediaType.ToString()),
            f => Assert.Equal("text/xml", f.MediaType.ToString()));
    }

    [Fact]
    public void GetApiDescription_IncludesRequestFormats_FilteredByAttribute()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsProduct_Body));

        action.FilterDescriptors = new List<FilterDescriptor>
            {
                new FilterDescriptor(new ContentTypeAttribute("text/*"), FilterScope.Action)
            };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Collection(
            description.SupportedRequestFormats.OrderBy(f => f.MediaType.ToString()),
            f => Assert.Equal("text/json", f.MediaType.ToString()),
            f => Assert.Equal("text/xml", f.MediaType.ToString()));
    }

    [Fact]
    public void GetApiDescription_IncludesRequestFormats_FilteredByAcceptsMetadata()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsProduct_Body));
        action.EndpointMetadata = new List<object>() { new XmlOnlyMetadata() };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Collection(
            description.SupportedRequestFormats.OrderBy(f => f.MediaType.ToString()),
            f => Assert.Equal("application/xml", f.MediaType.ToString()),
            f => Assert.Equal("text/xml", f.MediaType.ToString()));
    }

    [Fact]
    public void GetApiDescription_IncludesRequestFormats_FilteredByType()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsProduct_Body));

        action.FilterDescriptors = new List<FilterDescriptor>
            {
                new FilterDescriptor(new ContentTypeAttribute("text/*"), FilterScope.Action)
            };

        var formatters = CreateInputFormatters();

        // This will just format Order
        formatters[0].SupportedTypes.Add(typeof(Order));

        // This will just format Product
        formatters[1].SupportedTypes.Add(typeof(Product));

        // Act
        var descriptions = GetApiDescriptions(action, inputFormatters: formatters);

        // Assert
        var description = Assert.Single(descriptions);

        var format = Assert.Single(description.SupportedRequestFormats);
        Assert.Equal("text/xml", format.MediaType.ToString());
        Assert.Same(formatters[1], format.Formatter);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_ModelBoundParameter()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsProduct));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var parameters = description.ParameterDescriptions;
        Assert.Equal(3, parameters.Count);

        var parameter = Assert.Single(parameters, p => p.Name == "ProductId");
        Assert.Same(BindingSource.ModelBinding, parameter.Source);
        Assert.Equal(typeof(int), parameter.Type);

        parameter = Assert.Single(parameters, p => p.Name == "Name");
        Assert.Same(BindingSource.ModelBinding, parameter.Source);
        Assert.Equal(typeof(string), parameter.Type);

        parameter = Assert.Single(parameters, p => p.Name == "Description");
        Assert.Same(BindingSource.ModelBinding, parameter.Source);
        Assert.Equal(typeof(string), parameter.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_IsRequiredSet()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(RequiredParameter));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        var parameter = Assert.Single(description.ParameterDescriptions);
        Assert.Equal("name", parameter.Name);
        Assert.Same(BindingSource.ModelBinding, parameter.Source);
        Assert.Equal(typeof(string), parameter.Type);
        Assert.True(parameter.ModelMetadata.IsRequired);
        Assert.True(parameter.ModelMetadata.IsBindingRequired);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_SourceFromRouteData()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsId_Route));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var parameter = Assert.Single(description.ParameterDescriptions);
        Assert.Equal("id", parameter.Name);
        Assert.Same(BindingSource.Path, parameter.Source);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_SourceFromQueryString()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsId_Query));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var parameter = Assert.Single(description.ParameterDescriptions);
        Assert.Equal("id", parameter.Name);
        Assert.Same(BindingSource.Query, parameter.Source);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_SourceFromBody()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsProduct_Body));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var parameter = Assert.Single(description.ParameterDescriptions);
        Assert.Equal("product", parameter.Name);
        Assert.Same(BindingSource.Body, parameter.Source);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_SourceFromForm()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsProduct_Form));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var parameters = description.ParameterDescriptions;
        Assert.Equal(3, parameters.Count);

        var parameter = Assert.Single(parameters, p => p.Name == "ProductId");
        Assert.Same(BindingSource.Form, parameter.Source);
        Assert.Equal(typeof(int), parameter.Type);

        parameter = Assert.Single(parameters, p => p.Name == "Name");
        Assert.Same(BindingSource.Form, parameter.Source);
        Assert.Equal(typeof(string), parameter.Type);

        parameter = Assert.Single(parameters, p => p.Name == "Description");
        Assert.Same(BindingSource.Form, parameter.Source);
        Assert.Equal(typeof(string), parameter.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_SourceFromFormFile()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsFormFile));
        action.FilterDescriptors = new[]
        {
            new FilterDescriptor(new ConsumesAttribute("multipart/form-data"), FilterScope.Action),
        };

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var parameters = description.ParameterDescriptions;
        var parameter = Assert.Single(parameters);
        Assert.Same(BindingSource.FormFile, parameter.Source);

        var requestFormat = Assert.Single(description.SupportedRequestFormats);
        Assert.Equal("multipart/form-data", requestFormat.MediaType);
        Assert.Null(requestFormat.Formatter);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_SourceFromHeader()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsId_Header));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var parameter = Assert.Single(description.ParameterDescriptions);
        Assert.Equal("id", parameter.Name);
        Assert.Same(BindingSource.Header, parameter.Source);
    }

    // 'Hidden' parameters are hidden (not returned).
    [Fact]
    public void GetApiDescription_ParameterDescription_SourceFromServices()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsFormatters_Services));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Empty(description.ParameterDescriptions);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_SourceFromCustomModelBinder()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsProduct_Custom));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var parameter = Assert.Single(description.ParameterDescriptions);
        Assert.Equal("product", parameter.Name);
        Assert.Same(BindingSource.Custom, parameter.Source);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_SourceFromDefault_ModelBinderAttribute_WithoutBinderType()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsProduct_Default));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var parameters = description.ParameterDescriptions;
        Assert.Equal(3, parameters.Count);

        var parameter = Assert.Single(parameters, p => p.Name == "ProductId");
        Assert.Same(BindingSource.ModelBinding, parameter.Source);
        Assert.Equal(typeof(int), parameter.Type);

        parameter = Assert.Single(parameters, p => p.Name == "Name");
        Assert.Same(BindingSource.ModelBinding, parameter.Source);
        Assert.Equal(typeof(string), parameter.Type);

        parameter = Assert.Single(parameters, p => p.Name == "Description");
        Assert.Same(BindingSource.ModelBinding, parameter.Source);
        Assert.Equal(typeof(string), parameter.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_ComplexDTO()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsProductChangeDTO));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(4, description.ParameterDescriptions.Count);

        var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "Id");
        Assert.Same(BindingSource.Path, id.Source);
        Assert.Equal(typeof(int), id.Type);

        var product = Assert.Single(description.ParameterDescriptions, p => p.Name == "Product");
        Assert.Same(BindingSource.Body, product.Source);
        Assert.Equal(typeof(Product), product.Type);

        var userId = Assert.Single(description.ParameterDescriptions, p => p.Name == "UserId");
        Assert.Same(BindingSource.Header, userId.Source);
        Assert.Equal(typeof(string), userId.Type);

        var comments = Assert.Single(description.ParameterDescriptions, p => p.Name == "Comments");
        Assert.Same(BindingSource.ModelBinding, comments.Source);
        Assert.Equal(typeof(string), comments.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_FromQueryEmployee()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsEmployee));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(1, description.ParameterDescriptions.Count);

        var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "Name");
        Assert.Same(BindingSource.Query, id.Source);
        Assert.Equal(typeof(string), id.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_ParsablePrimitiveType()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsTryParsablePrimitiveType));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(1, description.ParameterDescriptions.Count);

        var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "id");
        Assert.Same(BindingSource.Query, id.Source);
        Assert.Equal(typeof(Guid), id.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_NullableParsablePrimitiveType()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsTryParsableNullablePrimitiveType));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(1, description.ParameterDescriptions.Count);

        var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "id");
        Assert.Same(BindingSource.Query, id.Source);
        Assert.Equal(typeof(Guid?), id.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_ParsableType()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsTryParsableEmployee));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(1, description.ParameterDescriptions.Count);

        var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "employee");
        Assert.Same(BindingSource.Query, id.Source);
        Assert.Equal(typeof(string), id.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_NullableParsableType()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsNullableTryParsableEmployee));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(1, description.ParameterDescriptions.Count);

        var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "employee");
        Assert.Same(BindingSource.Query, id.Source);
        Assert.Equal(typeof(string), id.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_ConvertibleType()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsConvertibleEmployee));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(1, description.ParameterDescriptions.Count);

        var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "employee");
        Assert.Same(BindingSource.Query, id.Source);
        Assert.Equal(typeof(string), id.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_NullableConvertibleType()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsNullableConvertibleEmployee));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(1, description.ParameterDescriptions.Count);

        var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "employee");
        Assert.Same(BindingSource.Query, id.Source);
        Assert.Equal(typeof(string), id.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_FromQueryManager()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsManager));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(2, description.ParameterDescriptions.Count);

        var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "managerid");
        Assert.Same(BindingSource.Query, id.Source);
        Assert.Equal(typeof(string), id.Type);

        var product = Assert.Single(description.ParameterDescriptions, p => p.Name == "name");
        Assert.Same(BindingSource.Query, product.Source);
        Assert.Equal(typeof(string), product.Type);
    }

    // The method under test uses an attribute on the parameter to set a 'default' source
    [Fact]
    public void GetApiDescription_ParameterDescription_ComplexDTO_AmbientValueProviderMetadata()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsProductChangeDTO_Query));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(4, description.ParameterDescriptions.Count);

        var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "Id");
        Assert.Same(BindingSource.Path, id.Source);
        Assert.Equal(typeof(int), id.Type);

        var product = Assert.Single(description.ParameterDescriptions, p => p.Name == "Product");
        Assert.Same(BindingSource.Body, product.Source);
        Assert.Equal(typeof(Product), product.Type);

        var userId = Assert.Single(description.ParameterDescriptions, p => p.Name == "UserId");
        Assert.Same(BindingSource.Header, userId.Source);
        Assert.Equal(typeof(string), userId.Type);

        var comments = Assert.Single(description.ParameterDescriptions, p => p.Name == "Comments");
        Assert.Same(BindingSource.Query, comments.Source);
        Assert.Equal(typeof(string), comments.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_ComplexDTO_AnotherLevel()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsOrderDTO));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(4, description.ParameterDescriptions.Count);

        var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "Id");
        Assert.Same(BindingSource.Path, id.Source);
        Assert.Equal(typeof(int), id.Type);

        var quantity = Assert.Single(description.ParameterDescriptions, p => p.Name == "Quantity");
        Assert.Same(BindingSource.ModelBinding, quantity.Source);
        Assert.Equal(typeof(int), quantity.Type);

        var productId = Assert.Single(description.ParameterDescriptions, p => p.Name == "Product.Id");
        Assert.Same(BindingSource.ModelBinding, productId.Source);
        Assert.Equal(typeof(int), productId.Type);

        var price = Assert.Single(description.ParameterDescriptions, p => p.Name == "Product.Price");
        Assert.Same(BindingSource.Query, price.Source);
        Assert.Equal(typeof(decimal), price.Type);
    }

    // The method under test uses an attribute on the parameter to set a 'default' source
    [Fact]
    public void GetApiDescription_ParameterDescription_ComplexDTO_AnotherLevel_AmbientValueProviderMetadata()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsOrderDTO_Query));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(4, description.ParameterDescriptions.Count);

        var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "Id");
        Assert.Same(BindingSource.Path, id.Source);
        Assert.Equal(typeof(int), id.Type);

        var quantity = Assert.Single(description.ParameterDescriptions, p => p.Name == "Quantity");
        Assert.Same(BindingSource.Query, quantity.Source);
        Assert.Equal(typeof(int), quantity.Type);

        var productId = Assert.Single(description.ParameterDescriptions, p => p.Name == "Product.Id");
        Assert.Same(BindingSource.Query, productId.Source);
        Assert.Equal(typeof(int), productId.Type);

        var productPrice = Assert.Single(description.ParameterDescriptions, p => p.Name == "Product.Price");
        Assert.Same(BindingSource.Query, productPrice.Source);
        Assert.Equal(typeof(decimal), productPrice.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_DuplicatePropertiesWithChildren_ExpandBoth()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsMultipleProperties));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(4, description.ParameterDescriptions.Count);

        var parentNames = new[] { "Parent1", "Parent2" };

        foreach (var parentName in parentNames)
        {
            var id = Assert.Single(description.ParameterDescriptions, p => p.Name == $"{parentName}.Child.Id");
            Assert.Same(BindingSource.Query, id.Source);
            Assert.Equal(typeof(int), id.Type);

            var name = Assert.Single(description.ParameterDescriptions, p => p.Name == $"{parentName}.Child.Name");
            Assert.Same(BindingSource.Query, name.Source);
            Assert.Equal(typeof(string), name.Type);
        }
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_DuplicatePropertiesWithChildren_Nested_ExpandAll()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsMultiplePropertiesNested));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(8, description.ParameterDescriptions.Count);

        var groupNames = new[] { "Group1", "Group2" };
        var parentNames = new[] { "Parent1", "Parent2" };

        foreach (var groupName in groupNames)
        {
            foreach (var parentName in parentNames)
            {
                var id = Assert.Single(description.ParameterDescriptions, p => p.Name == $"{groupName}.{parentName}.Child.Id");
                Assert.Same(BindingSource.Query, id.Source);
                Assert.Equal(typeof(int), id.Type);

                var name = Assert.Single(description.ParameterDescriptions, p => p.Name == $"{groupName}.{parentName}.Child.Name");
                Assert.Same(BindingSource.Query, name.Source);
                Assert.Equal(typeof(string), name.Type);
            }
        }
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_BreaksCycles()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsCycle));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var c = Assert.Single(description.ParameterDescriptions);
        Assert.Same(BindingSource.Query, c.Source);
        Assert.Equal("C.C.C.C", c.Name);
        Assert.Equal(typeof(Cycle1), c.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_DTOWithCollection()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsHasCollection));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var products = Assert.Single(description.ParameterDescriptions);
        Assert.Same(BindingSource.Query, products.Source);
        Assert.Equal("Products", products.Name);
        Assert.Equal(typeof(Product[]), products.Type);
    }

    // If a property/parameter is a collection, we automatically treat it as a leaf-node.
    [Fact]
    public void GetApiDescription_ParameterDescription_DTOWithCollection_ElementsWithBinderMetadataIgnored()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsHasCollection_Complex));

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var items = Assert.Single(description.ParameterDescriptions);
        Assert.Same(BindingSource.ModelBinding, items.Source);
        Assert.Equal("Items", items.Name);
        Assert.Equal(typeof(Child[]), items.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_RedundantMetadata_NotMergedWithParent()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsRedundantMetadata));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var parameters = description.ParameterDescriptions;
        Assert.Equal(2, parameters.Count);

        var id = Assert.Single(parameters, p => p.Name == "Id");
        Assert.Same(BindingSource.Query, id.Source);
        Assert.Equal(typeof(int), id.Type);

        var name = Assert.Single(parameters, p => p.Name == "Name");
        Assert.Same(BindingSource.Query, name.Source);
        Assert.Equal(typeof(string), name.Type);
    }

    [Fact]
    public void GetApiDescription_ParameterDescription_RedundantMetadata_WithParameterMetadata()
    {
        // Arrange
        var action = CreateActionDescriptor(nameof(AcceptsPerson));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);

        var name = Assert.Single(description.ParameterDescriptions, p => p.Name == "Name");
        Assert.Same(BindingSource.Header, name.Source);
        Assert.Equal(typeof(string), name.Type);

        var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "Id");
        Assert.Same(BindingSource.Form, id.Source);
        Assert.Equal(typeof(int), id.Type);
    }

    [Fact]
    public void GetApiDescription_WithControllerProperties_Merges_ParameterDescription()
    {
        // Arrange
        var action = CreateActionDescriptor("FromQueryName", typeof(TestController));
        var parameterDescriptor = action.Parameters.Single();

        // Act
        var descriptions = GetApiDescriptions(action);

        // Assert
        var description = Assert.Single(descriptions);
        Assert.Equal(5, description.ParameterDescriptions.Count);

        var name = Assert.Single(description.ParameterDescriptions, p => p.Name == "name");
        Assert.Same(BindingSource.Query, name.Source);
        Assert.Equal(typeof(string), name.Type);

        var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "Id");
        Assert.Same(BindingSource.Path, id.Source);
        Assert.Equal(typeof(int), id.Type);

        var product = Assert.Single(description.ParameterDescriptions, p => p.Name == "Product");
        Assert.Same(BindingSource.Body, product.Source);
        Assert.Equal(typeof(Product), product.Type);

        var userId = Assert.Single(description.ParameterDescriptions, p => p.Name == "UserId");
        Assert.Same(BindingSource.Header, userId.Source);
        Assert.Equal(typeof(string), userId.Type);

        var comments = Assert.Single(description.ParameterDescriptions, p => p.Name == "Comments");
        Assert.Same(BindingSource.ModelBinding, comments.Source);
        Assert.Equal(typeof(string), comments.Type);
    }

    [Fact]
    public void ProcessIsRequired_SetsTrue_ForFromBodyParameters()
    {
        // Arrange
        var description = new ApiParameterDescription { Source = BindingSource.Body, };
        var context = GetApiParameterContext(description);

        // Act
        DefaultApiDescriptionProvider.ProcessIsRequired(context, new MvcOptions());

        // Assert
        Assert.True(description.IsRequired);
    }

    [Fact]
    public void ProcessIsRequired_SetsFalse_IfAllowEmptyInputInBodyModelBinding_IsSetInMvcOptions()
    {
        // Arrange
        var description = new ApiParameterDescription { Source = BindingSource.Body, };
        var context = GetApiParameterContext(description);

        // Act
        DefaultApiDescriptionProvider.ProcessIsRequired(context, new MvcOptions { AllowEmptyInputInBodyModelBinding = true });

        // Assert
        Assert.False(description.IsRequired);
    }

    [Fact]
    public void ProcessIsRequired_SetsFalse_IfEmptyBodyBehaviorIsAllowedInBindingInfo()
    {
        // Arrange
        var description = new ApiParameterDescription
        {
            Source = BindingSource.Body,
            BindingInfo = new BindingInfo
            {
                EmptyBodyBehavior = EmptyBodyBehavior.Allow,
            }
        };
        var context = GetApiParameterContext(description);

        // Act
        DefaultApiDescriptionProvider.ProcessIsRequired(context, new MvcOptions());

        // Assert
        Assert.False(description.IsRequired);
    }

    [Fact]
    public void ProcessIsRequired_SetsTrue_ForParameterDescriptorsWithBindRequired()
    {
        // Arrange
        var description = new ApiParameterDescription
        {
            Source = BindingSource.Query,
        };
        var context = GetApiParameterContext(description);
        var modelMetadataProvider = new TestModelMetadataProvider();
        modelMetadataProvider
            .ForProperty<Person>(nameof(Person.Name))
            .BindingDetails(d => d.IsBindingRequired = true);
        description.ModelMetadata = modelMetadataProvider.GetMetadataForProperty(typeof(Person), nameof(Person.Name));

        // Act
        DefaultApiDescriptionProvider.ProcessIsRequired(context, new MvcOptions());

        // Assert
        Assert.True(description.IsRequired);
    }

    [Fact]
    public void ProcessIsRequired_SetsTrue_ForRequiredRouteParameterDescriptors()
    {
        // Arrange
        var description = new ApiParameterDescription
        {
            Source = BindingSource.Path,
            RouteInfo = new ApiParameterRouteInfo(),
        };
        var context = GetApiParameterContext(description);

        // Act
        DefaultApiDescriptionProvider.ProcessIsRequired(context, new MvcOptions());

        // Assert
        Assert.True(description.IsRequired);
    }

    [Fact]
    public void ProcessIsRequired_DoesNotSetToTrue_ByDefault()
    {
        // Arrange
        var description = new ApiParameterDescription();
        var context = GetApiParameterContext(description);

        // Act
        DefaultApiDescriptionProvider.ProcessIsRequired(context, new MvcOptions());

        // Assert
        Assert.False(description.IsRequired);
    }

    [Fact]
    public void ProcessIsRequired_DoesNotSetToTrue_ForParameterDescriptorsWithValidationRequired()
    {
        // Arrange
        var description = new ApiParameterDescription();
        var context = GetApiParameterContext(description);
        var modelMetadataProvider = new TestModelMetadataProvider();
        modelMetadataProvider
            .ForProperty<Person>(nameof(Person.Name))
            .ValidationDetails(d => d.IsRequired = true);
        description.ModelMetadata = modelMetadataProvider.GetMetadataForProperty(typeof(Person), nameof(Person.Name));

        // Act
        DefaultApiDescriptionProvider.ProcessIsRequired(context, new MvcOptions());

        // Assert
        Assert.False(description.IsRequired);
    }

    [Fact]
    public void ProcessDefaultValue_SetsDefaultRouteValue()
    {
        // Arrange
        var methodInfo = GetType().GetMethod(nameof(ParameterDefaultValue), BindingFlags.Instance | BindingFlags.NonPublic);
        var parameterInfo = methodInfo.GetParameters()[0];

        var defaultValue = new object();
        var description = new ApiParameterDescription
        {
            Source = BindingSource.Path,
            RouteInfo = new ApiParameterRouteInfo { DefaultValue = defaultValue },
            ParameterDescriptor = new ControllerParameterDescriptor
            {
                ParameterInfo = parameterInfo,
            },
        };
        var context = GetApiParameterContext(description);

        // Act
        DefaultApiDescriptionProvider.ProcessParameterDefaultValue(context);

        // Assert
        Assert.Same(defaultValue, description.DefaultValue);
    }

    [Fact]
    public void ProcessDefaultValue_SetsDefaultValue_FromParameterInfo()
    {
        // Arrange
        var methodInfo = GetType().GetMethod(nameof(ParameterDefaultValue), BindingFlags.Instance | BindingFlags.NonPublic);
        var parameterInfo = methodInfo.GetParameters()[0];
        var description = new ApiParameterDescription
        {
            Source = BindingSource.Query,
            ParameterDescriptor = new ControllerParameterDescriptor
            {
                ParameterInfo = parameterInfo,
            },
        };
        var context = GetApiParameterContext(description);

        // Act
        DefaultApiDescriptionProvider.ProcessParameterDefaultValue(context);

        // Assert
        Assert.Equal(10, description.DefaultValue);
    }

    [Fact]
    public void ProcessDefaultValue_DoesNotSpecifyDefaultValueForValueTypes_WhenNoValueIsSpecified()
    {
        // Arrange
        var methodInfo = GetType().GetMethod(nameof(AcceptsId_Query), BindingFlags.Instance | BindingFlags.NonPublic);
        var parameterInfo = methodInfo.GetParameters()[0];
        var description = new ApiParameterDescription
        {
            Source = BindingSource.Query,
            ParameterDescriptor = new ControllerParameterDescriptor
            {
                ParameterInfo = parameterInfo,
            },
        };
        var context = GetApiParameterContext(description);

        // Act
        DefaultApiDescriptionProvider.ProcessParameterDefaultValue(context);

        // Assert
        Assert.Null(description.DefaultValue);
    }

    private static ApiParameterContext GetApiParameterContext(ApiParameterDescription description)
    {
        var context = new ApiParameterContext(new EmptyModelMetadataProvider(), new ControllerActionDescriptor(), new TemplatePart[0]);
        context.Results.Add(description);
        return context;
    }

    private IReadOnlyList<ApiDescription> GetApiDescriptions(
        ActionDescriptor action,
        List<MockInputFormatter> inputFormatters = null,
        List<MockOutputFormatter> outputFormatters = null,
        RouteOptions routeOptions = null)
    {
        var context = new ApiDescriptionProviderContext(new ActionDescriptor[] { action });

        var options = new MvcOptions();
        foreach (var formatter in inputFormatters ?? CreateInputFormatters())
        {
            options.InputFormatters.Add(formatter);
        }

        foreach (var formatter in outputFormatters ?? CreateOutputFormatters())
        {
            options.OutputFormatters.Add(formatter);
        }

        var optionsAccessor = Options.Create(options);

        var constraintResolver = new Mock<IInlineConstraintResolver>();
        constraintResolver.Setup(c => c.ResolveConstraint("int"))
            .Returns(new IntRouteConstraint());

        var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

        var provider = new DefaultApiDescriptionProvider(
            optionsAccessor,
            constraintResolver.Object,
            modelMetadataProvider,
            new ActionResultTypeMapper(),
            Options.Create(routeOptions ?? new RouteOptions()));

        provider.OnProvidersExecuting(context);
        provider.OnProvidersExecuted(context);

        return new ReadOnlyCollection<ApiDescription>(context.Results);
    }

    private List<MockInputFormatter> CreateInputFormatters()
    {
        // Include some default formatters that look reasonable, some tests will override this.
        var formatters = new List<MockInputFormatter>()
            {
                new MockInputFormatter(),
                new MockInputFormatter(),
            };

        formatters[0].SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        formatters[0].SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/json"));

        formatters[1].SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));
        formatters[1].SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));

        return formatters;
    }

    private List<MockOutputFormatter> CreateOutputFormatters()
    {
        // Include some default formatters that look reasonable, some tests will override this.
        var formatters = new List<MockOutputFormatter>()
            {
                new MockOutputFormatter(),
                new MockOutputFormatter(),
            };

        formatters[0].SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        formatters[0].SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/json"));

        formatters[1].SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/xml"));
        formatters[1].SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/xml"));

        return formatters;
    }

    private ControllerActionDescriptor CreateActionDescriptor(string methodName = null, Type controllerType = null)
    {
        var action = new ControllerActionDescriptor();
        action.SetProperty(new ApiDescriptionActionData());

        if (controllerType != null)
        {
            action.MethodInfo = controllerType.GetMethod(
                methodName ?? "ReturnsObject",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            action.ControllerTypeInfo = controllerType.GetTypeInfo();
            action.BoundProperties = new List<ParameterDescriptor>();

            foreach (var property in controllerType.GetProperties())
            {
                var bindingInfo = BindingInfo.GetBindingInfo(property.GetCustomAttributes().OfType<object>());
                if (bindingInfo != null)
                {
                    action.BoundProperties.Add(new ParameterDescriptor()
                    {
                        BindingInfo = bindingInfo,
                        Name = property.Name,
                        ParameterType = property.PropertyType,
                    });
                }
            }
        }
        else
        {
            action.MethodInfo = GetType().GetMethod(
                methodName ?? "ReturnsObject",
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        action.Parameters = new List<ParameterDescriptor>();
        foreach (var parameter in action.MethodInfo.GetParameters())
        {
            action.Parameters.Add(new ControllerParameterDescriptor()
            {
                Name = parameter.Name,
                ParameterType = parameter.ParameterType,
                BindingInfo = BindingInfo.GetBindingInfo(parameter.GetCustomAttributes().OfType<object>()),
                ParameterInfo = parameter
            });
        }

        return action;
    }

    private IEnumerable<string> GetSortedMediaTypes(ApiResponseType apiResponseType)
    {
        return apiResponseType.ApiResponseFormats
            .OrderBy(responseType => responseType.MediaType)
            .Select(responseType => responseType.MediaType);
    }

    private object ReturnsObject()
    {
        return null;
    }

    private void ReturnsVoid()
    {
    }

    private IActionResult ReturnsActionResult()
    {
        return null;
    }

    private JsonResult ReturnsJsonResult()
    {
        return null;
    }

    private Task<Product> ReturnsTaskOfProduct()
    {
        return null;
    }

    private Task<object> ReturnsTaskOfObject()
    {
        return null;
    }

    private Task ReturnsTask()
    {
        return null;
    }

    private Task<IActionResult> ReturnsTaskOfActionResult()
    {
        return null;
    }

    private Task<JsonResult> ReturnsTaskOfJsonResult()
    {
        return null;
    }

    private ValueTask<Product> ReturnsValueTaskOfProduct()
    {
        return default;
    }

    private ValueTask<object> ReturnsValueTaskOfObject()
    {
        return default;
    }

    private ValueTask ReturnsValueTask()
    {
        return default;
    }

    private ValueTask<IActionResult> ReturnsValueTaskOfActionResult()
    {
        return default;
    }

    private ValueTask<JsonResult> ReturnsValueTaskOfJsonResult()
    {
        return default;
    }

    private Product ReturnsProduct()
    {
        return null;
    }

    private ActionResult<Product> ReturnsActionResultOfProduct() => null;
    private Http.HttpResults.Ok<Product> ReturnsResultOfProductWithEndpointMetadata() => null;

    private ActionResult<IEnumerable<Product>> ReturnsActionResultOfSequenceOfProducts() => null;

    private Task<ActionResult<Product>> ReturnsTaskOfActionResultOfProduct() => null;
    private Task<Http.HttpResults.Ok<Product>> ReturnsTaskOfResultOfProductWithEndpointMetadata() => null;

    private Task<ActionResult<IEnumerable<Product>>> ReturnsTaskOfActionResultOfSequenceOfProducts() => null;

    private void AcceptsProduct(Product product)
    {
    }

    private void RequiredParameter([BindRequired, Required] string name)
    {
    }

    private void AcceptsProduct_Body([FromBody] Product product)
    {
    }

    private void AcceptsProduct_Form([FromForm] Product product)
    {
    }

    private void AcceptsFormFile([FromFormFile] IFormFile formFile)
    {
    }

    // This will show up as source = model binding
    private void AcceptsProduct_Default([ModelBinder] Product product)
    {
    }

    // This will show up as source = unknown
    private void AcceptsProduct_Custom([ModelBinder<BodyModelBinder>] Product product)
    {
    }

    private void AcceptsId_Route([FromRoute] int id)
    {
    }

    private void AcceptsId_Query([FromQuery] int id)
    {
    }

    private void AcceptsId_Header([FromHeader] int id)
    {
    }

    private void AcceptsFormatters_Services([FromServices] ITestService tempDataProvider, [FromKeyedServices("foo")] ITestService keyedTempDataProvider)
    {
    }

    private void AcceptsProductChangeDTO(ProductChangeDTO dto)
    {
    }

    private void AcceptsProductChangeDTO_Query([FromQuery] ProductChangeDTO dto)
    {
    }

    private void AcceptsManager([ModelBinder] Manager dto)
    {
    }

    private void AcceptsEmployee([FromQuery(Name = "employee")] Employee dto)
    {
    }

    private void AcceptsTryParsablePrimitiveType([FromQuery] Guid id)
    {
    }

    private void AcceptsTryParsableEmployee([FromQuery] TryParsableEmployee employee)
    {
    }

    private void AcceptsConvertibleEmployee([FromQuery] ConvertibleEmployee employee)
    {
    }

#nullable enable

    private void AcceptsNullableTryParsableEmployee([FromQuery] TryParsableEmployee? employee)
    {
    }

    private void AcceptsTryParsableNullablePrimitiveType([FromQuery] Guid? id)
    {
    }

    private void AcceptsNullableConvertibleEmployee([FromQuery] ConvertibleEmployee? employee)
    {
    }

#nullable restore

    private void AcceptsOrderDTO(OrderDTO dto)
    {
    }

    private void AcceptsOrderDTO_Query([FromQuery] OrderDTO dto)
    {
    }

    private void AcceptsCycle(Cycle1 c)
    {
    }

    private void AcceptsHasCollection(HasCollection c)
    {
    }

    private void AcceptsHasCollection_Complex(HasCollection_Complex c)
    {
    }

    private void AcceptsRedundantMetadata([FromQuery] RedundantMetadata r)
    {
    }

    private void AcceptsPerson([FromForm] Person person)
    {
    }

    private void FromRouting([FromRoute] int id)
    {
    }

    private void FromModelBinding(int id)
    {
    }

    private void FromCustom([ModelBinder<BodyModelBinder>] int id)
    {
    }

    private void FromHeader([FromHeader] int id)
    {
    }

    private void FromBody([FromBody] int id)
    {
    }

    private void AcceptsMultipleProperties([FromQuery] MultipleProperties model)
    {
    }

    private void AcceptsMultiplePropertiesNested([FromQuery] MultiplePropertiesContainer model)
    {
    }

    private void ParameterDefaultValue(int value = 10) { }

    private class TestController
    {
        [FromRoute]
        public int Id { get; set; }

        [FromBody]
        public Product Product { get; set; }

        [FromHeader]
        public string UserId { get; set; }

        [FromServices]
        public ITestService TestService { get; set; }

        [ModelBinder]
        public string Comments { get; set; }

        public string NotBound { get; set; }

        public void FromQueryName([FromQuery] string name)
        {
        }
    }

    public class Customer
    {
    }

    public class BadData
    {
    }

    public class ErrorDetails
    {
    }

    public class BaseProducesController : ControllerBase
    {
        public IActionResult ReturnsActionResult()
        {
            return null;
        }

        public Task ReturnsTask()
        {
            return null;
        }

        public ValueTask ReturnsValueTask()
        {
            return default;
        }

        public void ReturnsVoid()
        {
        }
    }

    public class DerivedProducesController : BaseProducesController
    {
    }

    private class Employee
    {
        public string Name { get; set; }
    }

    [TypeConverter(typeof(EmployeeConverter))]
    private class ConvertibleEmployee
    {
        public string Name { get; set; }
    }

    private struct TryParsableEmployee : IParsable<TryParsableEmployee>
    {
        public string Name { get; set; }

        public static TryParsableEmployee Parse(string s, IFormatProvider provider)
        {
            if (TryParse(s, provider, out var result))
            {
                return result;
            }

            throw new FormatException($"{nameof(s)} is not in the correct format");
        }

        public static bool TryParse([NotNullWhen(true)] string s, IFormatProvider provider, [MaybeNullWhen(false)] out TryParsableEmployee result)
        {
            result = new() { Name = s };
            return true;
        }
    }

    private class Manager
    {
        [FromQuery(Name = "managerid")]
        public string ManagerId { get; set; }

        [FromQuery(Name = "name")]
        public string Name { get; set; }
    }

    private class Product
    {
        public int ProductId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }

    private class Order
    {
        public int OrderId { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }
    }

    private class ProductChangeDTO
    {
        [FromRoute]
        public int Id { get; set; }

        [FromBody]
        public Product Product { get; set; }

        [FromHeader]
        public string UserId { get; set; }

        [FromServices]
        public ITestService TestService { get; set; }

        public string Comments { get; set; }
    }

    private class OrderDTO
    {
        [FromRoute]
        public int Id { get; set; }

        public int Quantity { get; set; }

        public OrderProductDTO Product { get; set; }
    }

    private class OrderProductDTO
    {
        public int Id { get; set; }

        [FromQuery]
        public decimal Price { get; set; }
    }

    private class Cycle1
    {
        public Cycle2 C { get; set; }
    }

    private class Cycle2
    {
        [FromQuery]
        public Cycle1 C { get; set; }
    }

    private class HasCollection
    {
        [FromQuery]
        public Product[] Products { get; set; }
    }

    private class HasCollection_Complex
    {
        public Child[] Items { get; set; }
    }

    private class Child
    {
        [FromQuery]
        public int Id { get; set; }

        public string Name { get; set; }
    }

    private class RedundantMetadata
    {
        [FromQuery]
        public int Id { get; set; }

        [FromQuery]
        public string Name { get; set; }
    }

    public class Person
    {
        [FromHeader(Name = "Name")]
        public string Name { get; set; }

        [FromForm]
        public int Id { get; set; }
    }

    private class MultiplePropertiesContainer
    {
        public MultipleProperties Group1 { get; set; }
        public MultipleProperties Group2 { get; set; }
    }

    private class MultipleProperties
    {
        public Parent Parent1 { get; set; }
        public Parent Parent2 { get; set; }
    }

    private class Parent
    {
        public Child Child { get; set; }
    }

    private class MockInputFormatter : TextInputFormatter
    {
        public List<Type> SupportedTypes { get; } = new List<Type>();

        public override Task<InputFormatterResult> ReadRequestBodyAsync(
            InputFormatterContext context,
            Encoding effectiveEncoding)
        {
            throw new NotImplementedException();
        }

        protected override bool CanReadType(Type type)
        {
            if (SupportedTypes.Count == 0)
            {
                return true;
            }
            else if (type == null)
            {
                return false;
            }
            else
            {
                return SupportedTypes.Contains(type);
            }
        }
    }

    private class MockOutputFormatter : OutputFormatter
    {
        public List<Type> SupportedTypes { get; } = new List<Type>();

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            throw new NotImplementedException();
        }

        protected override bool CanWriteType(Type type)
        {
            if (SupportedTypes.Count == 0)
            {
                return true;
            }
            else if (type == null)
            {
                return false;
            }
            else
            {
                return SupportedTypes.Contains(type);
            }
        }
    }

    private class ContentTypeAttribute :
        Attribute,
        IFilterMetadata,
        IApiResponseMetadataProvider,
        IApiRequestMetadataProvider
    {
        public ContentTypeAttribute(string mediaType)
        {
            ContentTypes.Add(mediaType);
            StatusCode = 200;
        }

        public MediaTypeCollection ContentTypes { get; } = new MediaTypeCollection();

        public int StatusCode { get; set; }

        public Type Type { get; set; }

        public void SetContentTypes(MediaTypeCollection contentTypes)
        {
            contentTypes.Clear();
            foreach (var contentType in ContentTypes)
            {
                contentTypes.Add(contentType);
            }
        }
    }

    private interface ITestService
    {
    }

    private class FromFormFileAttribute : Attribute, IBindingSourceMetadata
    {
        public BindingSource BindingSource => BindingSource.FormFile;
    }

    private class XmlOnlyMetadata : Http.Metadata.IAcceptsMetadata
    {
        public IReadOnlyList<string> ContentTypes => new[] { "text/xml", "application/xml" };

        public Type RequestType => null;

        public bool IsOptional => false;
    }

    private class EmployeeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string input)
            {
                return new ConvertibleEmployee() { Name = input };
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}
