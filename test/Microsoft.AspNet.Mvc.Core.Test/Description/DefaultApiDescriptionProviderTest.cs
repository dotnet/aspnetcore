// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Routing.Constraints;
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

        // This is a test for the placeholder behavior - see #886
        [Fact]
        public void GetApiDescription_PopulatesParameters()
        {
            // Arrange
            var action = CreateActionDescriptor();
            action.Parameters = new List<ParameterDescriptor>()
            {
                new ParameterDescriptor()
                {
                    Name = "id",
                    IsOptional = true,
                    ParameterType = typeof(int),
                },
                new ParameterDescriptor()
                {
                    BinderMetadata = new FromBodyAttribute(),
                    Name = "username",
                    ParameterType = typeof(string),
                }
            };

            // Act
            var descriptions = GetApiDescriptions(action);

            // Assert
            var description = Assert.Single(descriptions);
            Assert.Equal(2, description.ParameterDescriptions.Count);

            var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "id");
            Assert.NotNull(id.ModelMetadata);
            Assert.True(id.IsOptional);
            Assert.Same(action.Parameters[0], id.ParameterDescriptor);
            Assert.Equal(ApiParameterSource.Query, id.Source);
            Assert.Equal(typeof(int), id.Type);

            var username = Assert.Single(description.ParameterDescriptions, p => p.Name == "username");
            Assert.NotNull(username.ModelMetadata);
            Assert.False(username.IsOptional);
            Assert.Same(action.Parameters[1], username.ParameterDescriptor);
            Assert.Equal(ApiParameterSource.Body, username.Source);
            Assert.Equal(typeof(string), username.Type);
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
            Assert.Equal(ApiParameterSource.Path, parameter.Source);
            Assert.Equal(isOptional, parameter.IsOptional);
            Assert.Equal("id", parameter.Name);
            Assert.Null(parameter.ParameterDescriptor);

            if (constraintType != null)
            {
                Assert.IsType(constraintType, parameter.Constraint);
            }

            if (defaultValue != null)
            {
                Assert.Equal(defaultValue, parameter.DefaultValue);
            }
            else
            {
                Assert.Null(parameter.DefaultValue);
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
            var action = CreateActionDescriptor();
            action.AttributeRouteInfo = new AttributeRouteInfo { Template = template };

            var parameterDescriptor = new ParameterDescriptor
            {
                Name = "id",
                IsOptional = true,
                ParameterType = typeof(int),
            };
            action.Parameters = new List<ParameterDescriptor> { parameterDescriptor };

            // Act
            var descriptions = GetApiDescriptions(action);

            // Assert
            var description = Assert.Single(descriptions);

            var parameter = Assert.Single(description.ParameterDescriptions);
            Assert.Equal(ApiParameterSource.Path, parameter.Source);
            Assert.Equal(isOptional, parameter.IsOptional);
            Assert.Equal("id", parameter.Name);
            Assert.Equal(parameterDescriptor, parameter.ParameterDescriptor);

            if (constraintType != null)
            {
                Assert.IsType(constraintType, parameter.Constraint);
            }

            if (defaultValue != null)
            {
                Assert.Equal(defaultValue, parameter.DefaultValue);
            }
            else
            {
                Assert.Null(parameter.DefaultValue);
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
        public void GetApiDescription_CreatesDifferentParameters_IfParameterDescriptorIsFromBody(
            string template,
            bool isOptional,
            Type constraintType,
            object defaultValue)
        {
            // Arrange
            var action = CreateActionDescriptor();
            action.AttributeRouteInfo = new AttributeRouteInfo { Template = template };

            var parameterDescriptor = new ParameterDescriptor
            {
                BinderMetadata = new FromBodyAttribute(),
                Name = "id",
                IsOptional = false,
                ParameterType = typeof(int),
            };
            action.Parameters = new List<ParameterDescriptor> { parameterDescriptor };

            // Act
            var descriptions = GetApiDescriptions(action);

            // Assert
            var description = Assert.Single(descriptions);

            var bodyParameter = Assert.Single(description.ParameterDescriptions, p => p.Source == ApiParameterSource.Body);
            Assert.False(bodyParameter.IsOptional);
            Assert.Equal("id", bodyParameter.Name);
            Assert.Equal(parameterDescriptor, bodyParameter.ParameterDescriptor);

            var pathParameter = Assert.Single(description.ParameterDescriptions, p => p.Source == ApiParameterSource.Path);
            Assert.Equal(isOptional, pathParameter.IsOptional);
            Assert.Equal("id", pathParameter.Name);
            Assert.Null(pathParameter.ParameterDescriptor);

            if (constraintType != null)
            {
                Assert.IsType(constraintType, pathParameter.Constraint);
            }

            if (defaultValue != null)
            {
                Assert.Equal(defaultValue, pathParameter.DefaultValue);
            }
            else
            {
                Assert.Null(pathParameter.DefaultValue);
            }
        }

        [Theory]
        [InlineData("api/products/{id}", false, false)]
        [InlineData("api/products/{id}", true, false)]
        [InlineData("api/products/{id?}", false, false)]
        [InlineData("api/products/{id?}", true, true)]
        [InlineData("api/products/{id=5}", false, false)]
        [InlineData("api/products/{id=5}", true, true)]
        public void GetApiDescription_ParameterFromPathAndDescriptor_IsOptionalOnly_IfBothAreOptional(
            string template,
            bool isDescriptorParameterOptional,
            bool expectedOptional)
        {
            // Arrange
            var action = CreateActionDescriptor();
            action.AttributeRouteInfo = new AttributeRouteInfo { Template = template };

            var parameterDescriptor = new ParameterDescriptor
            {
                Name = "id",
                IsOptional = isDescriptorParameterOptional,
                ParameterType = typeof(int),
            };
            action.Parameters = new List<ParameterDescriptor> { parameterDescriptor };

            // Act
            var descriptions = GetApiDescriptions(action);

            // Assert
            var description = Assert.Single(descriptions);
            var parameter = Assert.Single(description.ParameterDescriptions);
            Assert.Equal(expectedOptional, parameter.IsOptional);
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
            Assert.Equal(ApiParameterSource.Path, id1.Source);
            Assert.Null(id1.Constraint);

            var id2 = Assert.Single(description.ParameterDescriptions, p => p.Name == "id2");
            Assert.Equal(ApiParameterSource.Path, id2.Source);
            Assert.IsType<IntRouteConstraint>(id2.Constraint);
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
            Assert.Single(formats, f => f.MediaType.RawValue == "text/json");
            Assert.Single(formats, f => f.MediaType.RawValue == "application/json");
            Assert.Single(formats, f => f.MediaType.RawValue == "text/xml");
            Assert.Single(formats, f => f.MediaType.RawValue == "application/xml");
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
            Assert.Single(formats, f => f.MediaType.RawValue == "text/json");
            Assert.Single(formats, f => f.MediaType.RawValue == "text/xml");
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
            Assert.Single(formats, f => f.MediaType.RawValue == "text/json");
            Assert.Same(formatters[0], formats[0].Formatter);
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

            var formattersProvider = new Mock<IOutputFormattersProvider>(MockBehavior.Strict);
            formattersProvider.Setup(fp => fp.OutputFormatters).Returns(formatters);

            var constraintResolver = new Mock<IInlineConstraintResolver>();
            constraintResolver.Setup(c => c.ResolveConstraint("int"))
                .Returns(new IntRouteConstraint());

            var modelMetadataProvider = new Mock<IModelMetadataProvider>(MockBehavior.Strict);
            modelMetadataProvider
                .Setup(mmp => mmp.GetMetadataForType(null, It.IsAny<Type>()))
                .Returns((Func<object> accessor, Type type) =>
                {
                    return new ModelMetadata(modelMetadataProvider.Object, null, accessor, type, null);
                });

            var provider = new DefaultApiDescriptionProvider(
                formattersProvider.Object,
                constraintResolver.Object,
                modelMetadataProvider.Object);

            provider.Invoke(context, () => { });
            return context.Results;
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

        private ControllerActionDescriptor CreateActionDescriptor(string methodName = null)
        {
            var action = new ControllerActionDescriptor();
            action.SetProperty(new ApiDescriptionActionData());

            action.MethodInfo = GetType().GetMethod(
                methodName ?? "ReturnsObject",
                BindingFlags.Instance | BindingFlags.NonPublic);

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

        private class Product
        {
        }

        private class Order
        {
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
    }
}