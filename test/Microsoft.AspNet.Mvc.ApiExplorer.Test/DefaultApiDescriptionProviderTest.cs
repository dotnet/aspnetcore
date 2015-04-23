// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ApiExplorer;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Constraints;
using Microsoft.Framework.OptionsModel;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Description
{
    public class DefaultApiDescriptionProviderTest
    {
        [Fact]
        public void GetApiDescription_IgnoresNonReflectedActionDescriptor()
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
                new HttpMethodConstraint(new string[] { "PUT", "POST" }),
                new HttpMethodConstraint(new string[] { "GET" }),
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

            var parameterDescriptor = action.Parameters[0];

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
            action.AttributeRouteInfo = new AttributeRouteInfo();
            action.AttributeRouteInfo.Template = template;

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
            action.AttributeRouteInfo = new AttributeRouteInfo();
            action.AttributeRouteInfo.Template = "api/Products/{id1}-{id2:int}";

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
            action.AttributeRouteInfo = new AttributeRouteInfo();
            action.AttributeRouteInfo.Template = "api/Products/{id1}-{id2}/{id3:int}/{id4:int?}/{*id5:int}";

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
        public void GetApiDescription_PopulatesResponseType_WithProduct()
        {
            // Arrange
            var action = CreateActionDescriptor(nameof(ReturnsProduct));

            // Act
            var descriptions = GetApiDescriptions(action);

            // Assert
            var description = Assert.Single(descriptions);
            Assert.Equal(typeof(Product), description.ResponseType);
            Assert.NotNull(description.ResponseModelMetadata);
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
            Assert.Equal(typeof(Product), description.ResponseType);
            Assert.NotNull(description.ResponseModelMetadata);
        }

        [Theory]
        [InlineData(nameof(ReturnsObject))]
        [InlineData(nameof(ReturnsActionResult))]
        [InlineData(nameof(ReturnsJsonResult))]
        [InlineData(nameof(ReturnsTaskOfObject))]
        [InlineData(nameof(ReturnsTaskOfActionResult))]
        [InlineData(nameof(ReturnsTaskOfJsonResult))]
        public void GetApiDescription_DoesNotPopulatesResponseInformation_WhenUnknown(string methodName)
        {
            // Arrange
            var action = CreateActionDescriptor(methodName);

            // Act
            var descriptions = GetApiDescriptions(action);

            // Assert
            var description = Assert.Single(descriptions);
            Assert.Null(description.ResponseType);
            Assert.Null(description.ResponseModelMetadata);
            Assert.Empty(description.SupportedResponseFormats);
        }

        [Theory]
        [InlineData(nameof(ReturnsVoid))]
        [InlineData(nameof(ReturnsTask))]
        public void GetApiDescription_DoesNotPopulatesResponseInformation_WhenVoid(string methodName)
        {
            // Arrange
            var action = CreateActionDescriptor(methodName);

            // Act
            var descriptions = GetApiDescriptions(action);

            // Assert
            var description = Assert.Single(descriptions);
            Assert.Equal(typeof(void), description.ResponseType);
            Assert.Null(description.ResponseModelMetadata);
            Assert.Empty(description.SupportedResponseFormats);
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
        public void GetApiDescription_PopulatesResponseInformation_WhenSetByFilter(string methodName)
        {
            // Arrange
            var action = CreateActionDescriptor(methodName);
            var filter = new ContentTypeAttribute("text/*")
            {
                Type = typeof(Order)
            };

            action.FilterDescriptors = new List<FilterDescriptor>();
            action.FilterDescriptors.Add(new FilterDescriptor(filter, FilterScope.Action));

            // Act
            var descriptions = GetApiDescriptions(action);

            // Assert
            var description = Assert.Single(descriptions);
            Assert.Equal(typeof(Order), description.ResponseType);
            Assert.NotNull(description.ResponseModelMetadata);
        }

        [Fact]
        public void GetApiDescription_IncludesResponseFormats()
        {
            // Arrange
            var action = CreateActionDescriptor(nameof(ReturnsProduct));

            // Act
            var descriptions = GetApiDescriptions(action);

            // Assert
            var description = Assert.Single(descriptions);
            Assert.Equal(4, description.SupportedResponseFormats.Count);

            var formats = description.SupportedResponseFormats;
            Assert.Single(formats, f => f.MediaType.ToString() == "text/json");
            Assert.Single(formats, f => f.MediaType.ToString() == "application/json");
            Assert.Single(formats, f => f.MediaType.ToString() == "text/xml");
            Assert.Single(formats, f => f.MediaType.ToString() == "application/xml");
        }

        [Fact]
        public void GetApiDescription_IncludesResponseFormats_FilteredByAttribute()
        {
            // Arrange
            var action = CreateActionDescriptor(nameof(ReturnsProduct));

            action.FilterDescriptors = new List<FilterDescriptor>();
            action.FilterDescriptors.Add(new FilterDescriptor(new ContentTypeAttribute("text/*"), FilterScope.Action));

            // Act
            var descriptions = GetApiDescriptions(action);

            // Assert
            var description = Assert.Single(descriptions);
            Assert.Equal(2, description.SupportedResponseFormats.Count);

            var formats = description.SupportedResponseFormats;
            Assert.Single(formats, f => f.MediaType.ToString() == "text/json");
            Assert.Single(formats, f => f.MediaType.ToString() == "text/xml");
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

            action.FilterDescriptors = new List<FilterDescriptor>();
            action.FilterDescriptors.Add(new FilterDescriptor(filter, FilterScope.Action));

            var formatters = CreateFormatters();

            // This will just format Order
            formatters[0].SupportedTypes.Add(typeof(Order));

            // This will just format Product
            formatters[1].SupportedTypes.Add(typeof(Product));

            // Act
            var descriptions = GetApiDescriptions(action, formatters);

            // Assert
            var description = Assert.Single(descriptions);
            Assert.Equal(1, description.SupportedResponseFormats.Count);
            Assert.Equal(typeof(Order), description.ResponseType);
            Assert.NotNull(description.ResponseModelMetadata);

            var formats = description.SupportedResponseFormats;
            Assert.Single(formats, f => f.MediaType.ToString() == "text/json");
            Assert.Same(formatters[0], formats[0].Formatter);
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

            var parameter = Assert.Single(description.ParameterDescriptions);
            Assert.Equal("product", parameter.Name);
            Assert.Same(BindingSource.ModelBinding, parameter.Source);
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

            var parameter = Assert.Single(description.ParameterDescriptions);
            Assert.Equal("product", parameter.Name);
            Assert.Same(BindingSource.Form, parameter.Source);
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

            var parameter = Assert.Single(description.ParameterDescriptions);
            Assert.Equal("product", parameter.Name);
            Assert.Same(BindingSource.ModelBinding, parameter.Source);
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
            Assert.Equal(3, description.ParameterDescriptions.Count);

            var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "Id");
            Assert.Same(BindingSource.Path, id.Source);
            Assert.Equal(typeof(int), id.Type);

            var quantity = Assert.Single(description.ParameterDescriptions, p => p.Name == "Quantity");
            Assert.Same(BindingSource.Query, quantity.Source);
            Assert.Equal(typeof(int), quantity.Type);

            var product = Assert.Single(description.ParameterDescriptions, p => p.Name == "Product");
            Assert.Same(BindingSource.Query, product.Source);
            Assert.Equal(typeof(OrderProductDTO), product.Type);
        }

        [Fact]
        public void GetApiDescription_ParameterDescription_BreaksCycles()
        {
            // Arrange
            var action = CreateActionDescriptor(nameof(AcceptsCycle));
            var parameterDescriptor = action.Parameters.Single();

            // Act
            var descriptions = GetApiDescriptions(action);

            // Assert
            var description = Assert.Single(descriptions);

            var c = Assert.Single(description.ParameterDescriptions);
            Assert.Same(BindingSource.Query, c.Source);
            Assert.Equal("C.C", c.Name);
            Assert.Equal(typeof(Cycle1), c.Type);
        }

        [Fact]
        public void GetApiDescription_ParameterDescription_DTOWithCollection()
        {
            // Arrange
            var action = CreateActionDescriptor(nameof(AcceptsHasCollection));
            var parameterDescriptor = action.Parameters.Single();

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
            var parameterDescriptor = action.Parameters.Single();

            // Act
            var descriptions = GetApiDescriptions(action);

            // Assert
            var description = Assert.Single(descriptions);

            var c = Assert.Single(description.ParameterDescriptions);
            Assert.Same(BindingSource.ModelBinding, c.Source);
            Assert.Equal("c", c.Name);
            Assert.Equal(typeof(HasCollection_Complex), c.Type);
        }

        [Fact]
        public void GetApiDescription_ParameterDescription_RedundentMetadataMergedWithParent()
        {
            // Arrange
            var action = CreateActionDescriptor(nameof(AcceptsRedundentMetadata));
            var parameterDescriptor = action.Parameters.Single();

            // Act
            var descriptions = GetApiDescriptions(action);

            // Assert
            var description = Assert.Single(descriptions);

            var r = Assert.Single(description.ParameterDescriptions);
            Assert.Same(BindingSource.Query, r.Source);
            Assert.Equal("r", r.Name);
            Assert.Equal(typeof(RedundentMetadata), r.Type);
        }

        [Fact]
        public void GetApiDescription_ParameterDescription_RedundentMetadata_WithParameterMetadata()
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

        private IReadOnlyList<ApiDescription> GetApiDescriptions(ActionDescriptor action)
        {
            return GetApiDescriptions(action, CreateFormatters());
        }

        private IReadOnlyList<ApiDescription> GetApiDescriptions(
            ActionDescriptor action,
            List<MockFormatter> formatters)
        {
            var context = new ApiDescriptionProviderContext(new ActionDescriptor[] { action });

            var options = new MvcOptions();
            foreach (var formatter in formatters)
            {
                options.OutputFormatters.Add(formatter);
            }

            var optionsAccessor = new Mock<IOptions<MvcOptions>>();
            optionsAccessor.SetupGet(o => o.Options)
                .Returns(options);

            var constraintResolver = new Mock<IInlineConstraintResolver>();
            constraintResolver.Setup(c => c.ResolveConstraint("int"))
                .Returns(new IntRouteConstraint());

            var modelMetadataProvider = TestModelMetadataProvider.CreateDefaultProvider();

            var provider = new DefaultApiDescriptionProvider(
                optionsAccessor.Object,
                constraintResolver.Object,
                modelMetadataProvider);

            provider.OnProvidersExecuting(context);
            provider.OnProvidersExecuted(context);

            return new ReadOnlyCollection<ApiDescription>(context.Results);
        }

        private List<MockFormatter> CreateFormatters()
        {
            // Include some default formatters that look reasonable, some tests will override this.
            var formatters = new List<MockFormatter>()
            {
                new MockFormatter(),
                new MockFormatter(),
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
                    BindingFlags.Instance | BindingFlags.Public);

                action.ControllerTypeInfo = controllerType.GetTypeInfo();
                action.BoundProperties = new List<ParameterDescriptor>();

                foreach (var property in action.ControllerTypeInfo.GetProperties())
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
                action.Parameters.Add(new ParameterDescriptor()
                {
                    Name = parameter.Name,
                    ParameterType = parameter.ParameterType,
                    BindingInfo = BindingInfo.GetBindingInfo(parameter.GetCustomAttributes().OfType<object>())
                });
            }

            return action;
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

        private Product ReturnsProduct()
        {
            return null;
        }

        private void AcceptsProduct(Product product)
        {
        }

        private void AcceptsProduct_Body([FromBody] Product product)
        {
        }

        private void AcceptsProduct_Form([FromForm] Product product)
        {
        }

        // This will show up as source = model binding
        private void AcceptsProduct_Default([ModelBinder] Product product)
        {
        }

        // This will show up as source = unknown
        private void AcceptsProduct_Custom([ModelBinder(BinderType = typeof(BodyModelBinder))] Product product)
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

        private void AcceptsFormatters_Services([FromServices] ITestService tempDataProvider)
        {
        }

        private void AcceptsProductChangeDTO(ProductChangeDTO dto)
        {
        }

        private void AcceptsProductChangeDTO_Query([FromQuery] ProductChangeDTO dto)
        {
        }

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

        private void AcceptsRedundentMetadata([FromQuery] RedundentMetadata r)
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

        private void FromCustom([ModelBinder(BinderType = typeof(BodyModelBinder))] int id)
        {
        }

        private void FromHeader([FromHeader] int id)
        {
        }

        private void FromBody([FromBody] int id)
        {
        }

        private class TestController
        {
            [FromRoute]
            public int Id { get; set; }

            [FromBody]
            public Product Product { get; set; }

            [FromHeader]
            public string UserId { get; set; }

            [ModelBinder]
            public string Comments { get; set; }

            public string NotBound { get; set; }

            public void FromQueryName([FromQuery] string name)
            {
            }
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

        private class RedundentMetadata
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

        private class MockFormatter : OutputFormatter
        {
            public List<Type> SupportedTypes { get; } = new List<Type>();

            public override Task WriteResponseBodyAsync(OutputFormatterContext context)
            {
                throw new NotImplementedException();
            }

            protected override bool CanWriteType(Type declaredType, Type actualType)
            {
                if (SupportedTypes.Count == 0)
                {
                    return true;
                }
                else if ((actualType ?? declaredType) == null)
                {
                    return false;
                }
                else
                {
                    return SupportedTypes.Contains(actualType ?? declaredType);
                }
            }
        }

        private class ContentTypeAttribute : Attribute, IFilter, IApiResponseMetadataProvider
        {
            public ContentTypeAttribute(string mediaType)
            {
                ContentTypes.Add(MediaTypeHeaderValue.Parse(mediaType));
            }

            public List<MediaTypeHeaderValue> ContentTypes { get; } = new List<MediaTypeHeaderValue>();

            public Type Type { get; set; }

            public void SetContentTypes(IList<MediaTypeHeaderValue> contentTypes)
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
    }
}