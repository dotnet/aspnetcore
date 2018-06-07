// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer
{
    public class ApiResponseTypeProviderTest
    {
        [Theory]
        [InlineData("id", "model")]
        [InlineData("id", "person")]
        [InlineData("id", "i")]
        public void IsParameterNameMatch_ReturnsFalse_IfConventionNameIsNotSuffix(string parameterName, string conventionName)
        {
            // Act
            var result = ApiResponseTypeProvider.IsParameterNameMatch(parameterName, conventionName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsParameterNameMatch_ReturnsFalse_IfConventionNameIsNotExactCaseSensitiveMatch()
        {
            // Arrange
            var parameterName = "Id";
            var conventionName = "id";

            // Act
            var result = ApiResponseTypeProvider.IsParameterNameMatch(parameterName, conventionName);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("rid", "id")]
        [InlineData("candid", "id")]
        [InlineData("colocation", "location")]
        public void IsParamterNameMatch_ReturnsFalse_IfConventionNameIsNotProperSuffix(string parameterName, string conventionName)
        {
            // Act
            var result = ApiResponseTypeProvider.IsParameterNameMatch(parameterName, conventionName);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("id", "id")]
        [InlineData("model", "model")]
        public void IsParamterNameMatch_ReturnsTrue_IfConventionNameIsExactMatch(string parameterName, string conventionName)
        {
            // Act
            var result = ApiResponseTypeProvider.IsParameterNameMatch(parameterName, conventionName);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("id", "_id")]
        [InlineData("model", "_model")]
        public void IsParamterNameMatch_ReturnsTrue_IfConventionNameIsExactMatchIgnoringLeadingUnderscores(string parameterName, string conventionName)
        {
            // Act
            var result = ApiResponseTypeProvider.IsParameterNameMatch(parameterName, conventionName);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("personId", "id")]
        [InlineData("userModel", "model")]
        [InlineData("beaconLocation", "Location")]
        public void IsParamterNameMatch_ReturnsTrue_IfConventionNameIsProperSuffix(string parameterName, string conventionName)
        {
            // Act
            var result = ApiResponseTypeProvider.IsParameterNameMatch(parameterName, conventionName);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("personId", "_id")]
        [InlineData("userModel", "_model")]
        [InlineData("userModel", "__model")]
        public void IsParamterNameMatch_ReturnsTrue_IfConventionNameIsProperSuffixIgnoringLeadingUnderscores(string parameterName, string conventionName)
        {
            // Act
            var result = ApiResponseTypeProvider.IsParameterNameMatch(parameterName, conventionName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsParameterTypeMatch_ReturnsFalse_ForUnrelatedTypes()
        {
            // Arrange
            var type = typeof(string);
            var conventionType = typeof(int);

            // Act
            var result = ApiResponseTypeProvider.IsParameterTypeMatch(type, conventionType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsParameterTypeMatch_ReturnsFalse_IfTypeIsBaseClassOfConvention()
        {
            // Arrange
            var type = typeof(BaseModel);
            var conventionType = typeof(DerivedModel);

            // Act
            var result = ApiResponseTypeProvider.IsParameterTypeMatch(type, conventionType);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsParameterTypeMatch_ReturnsTrue_IfTypeIsExact()
        {
            // Arrange
            var type = typeof(Uri);
            var conventionType = typeof(Uri);

            // Act
            var result = ApiResponseTypeProvider.IsParameterTypeMatch(type, conventionType);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsParameterTypeMatch_ReturnsTrue_IfTypeIsSubtypeOfConvention()
        {
            // Arrange
            var type = typeof(DerivedModel);
            var conventionType = typeof(BaseModel);

            // Act
            var result = ApiResponseTypeProvider.IsParameterTypeMatch(type, conventionType);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(DerivedModel))]
        public void IsParameterTypeMatch_ReturnsTrue_IfConventionTypeIsObject(Type type)
        {
            // Arrange
            var conventionType = typeof(object);

            // Act
            var result = ApiResponseTypeProvider.IsParameterTypeMatch(type, conventionType);

            // Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData("Get", "Post")]
        [InlineData("Post", "Get")]
        [InlineData("PostPerson", "Put")]
        public void IsMethodNameMatch_ReturnsFalse_IfMethodIsNotPrefix(string methodName, string conventionMethodName)
        {
            // Act
            var result = ApiResponseTypeProvider.IsMethodNameMatch(methodName, conventionMethodName);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("PostalService", "Post")]
        [InlineData("Listings", "List")]
        [InlineData("Putt", "Put")]
        public void IsMethodNameMatch_ReturnsFalse_IfMethodIsNotProperPrefix(string methodName, string conventionMethodName)
        {
            // Act
            var result = ApiResponseTypeProvider.IsMethodNameMatch(methodName, conventionMethodName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMethodNameMatch_ReturnsTrue_IfMethodNameIsExactMatch()
        {
            // Arrange
            var methodName = "Post";
            var conventionMethodName = "Post";

            // Act
            var result = ApiResponseTypeProvider.IsMethodNameMatch(methodName, conventionMethodName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsMethodNameMatch_ReturnsFalse_IfMethodNameIsExactMatchWithDifferentCasing()
        {
            // Arrange
            var methodName = "post";
            var conventionMethodName = "Post";

            // Act
            var result = ApiResponseTypeProvider.IsMethodNameMatch(methodName, conventionMethodName);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData("PostPerson", "Post")]
        [InlineData("GetById", "Get")]
        [InlineData("SearchList", "Search")]
        public void IsMethodNameMatch_ReturnsTrue_IfMethodNameIsProperSuffix(string methodName, string conventionMethodName)
        {
            // Act
            var result = ApiResponseTypeProvider.IsMethodNameMatch(methodName, conventionMethodName);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsMethodNameMatch_ReturnsFalse_IfMethodNameIsProperSuffix_WithDifferentCasing()
        {
            // Arrange
            var methodName = "getById";
            var conventionMethodName = "Get";

            // Act
            var result = ApiResponseTypeProvider.IsMethodNameMatch(methodName, conventionMethodName);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMatch_ReturnsFalse_IfMethodNamesAreNotMatches()
        {
            // Arrange
            var conventionMethod = typeof(DefaultApiConventions).GetMethod(nameof(DefaultApiConventions.Post));
            var method = typeof(TestController).GetMethod(nameof(TestController.GetUser));

            // Act
            var result = ApiResponseTypeProvider.IsMatch(method, conventionMethod);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMatch_ReturnsFalse_IfParameterCountsDoNotMatch()
        {
            // Arrange
            var conventionMethod = typeof(DefaultApiConventions).GetMethod(nameof(DefaultApiConventions.Get), new[] { typeof(object) });
            var method = typeof(TestController).GetMethod(nameof(TestController.GetUserLocation));

            // Act
            var result = ApiResponseTypeProvider.IsMatch(method, conventionMethod);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsMatch_ReturnsTrue_ForMethodWithObjectParameter()
        {
            // Arrange
            var conventionMethod = typeof(DefaultApiConventions).GetMethod(nameof(DefaultApiConventions.Get), new[] { typeof(object) });
            var method = typeof(TestController).GetMethod(nameof(TestController.GetUser));

            // Act
            var result = ApiResponseTypeProvider.IsMatch(method, conventionMethod);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsMatch_ReturnsTrue_ForConventionWithGenericParameter()
        {
            // Arrange
            var conventionMethod = typeof(DefaultApiConventions).GetMethod(nameof(DefaultApiConventions.Put));
            var method = typeof(TestController).GetMethod(nameof(TestController.PutModel));

            // Act
            var result = ApiResponseTypeProvider.IsMatch(method, conventionMethod);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresent()
        {
            // Arrange
            var actionDescriptor = GetControllerActionDescriptor(
                typeof(GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresentController),
                nameof(GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresentController.Get));

            var filter = new FilterDescriptor(new ApiConventionAttribute(typeof(DefaultApiConventions)), FilterScope.Controller);
            actionDescriptor.FilterDescriptors.Add(filter);

            var provider = GetProvider();

            // Act
            var result = provider.GetApiResponseTypes(actionDescriptor);

            // Assert
            Assert.Collection(
                result.OrderBy(r => r.StatusCode),
                responseType =>
                {
                    Assert.Equal(200, responseType.StatusCode);
                    Assert.Equal(typeof(BaseModel), responseType.Type);
                    Assert.False(responseType.IsDefaultResponse);
                    Assert.Collection(
                        responseType.ApiResponseFormats,
                        format => Assert.Equal("application/json", format.MediaType));
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

        [ApiConvention(typeof(DefaultApiConventions))]
        public class GetApiResponseTypes_ReturnsResponseTypesFromActionIfPresentController : ControllerBase
        {
            [Produces(typeof(BaseModel))]
            [ProducesResponseType(301)]
            [ProducesResponseType(404)]
            public Task<ActionResult<BaseModel>> Get(int id) => null;
        }

        [Fact]
        public void GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventions()
        {
            // Arrange
            var actionDescriptor = GetControllerActionDescriptor(
                typeof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController),
                nameof(GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController.DeleteBase));
            var filter = new FilterDescriptor(new ApiConventionAttribute(typeof(DefaultApiConventions)), FilterScope.Controller);
            actionDescriptor.FilterDescriptors.Add(filter);

            var provider = GetProvider();

            // Act
            var result = provider.GetApiResponseTypes(actionDescriptor);

            // Assert
            Assert.Collection(
               result.OrderBy(r => r.StatusCode),
               responseType =>
               {
                   Assert.Equal(200, responseType.StatusCode);
                   Assert.Equal(typeof(void), responseType.Type);
                   Assert.False(responseType.IsDefaultResponse);
                   Assert.Empty(responseType.ApiResponseFormats);
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

        [ApiConvention(typeof(DefaultApiConventions))]
        public class GetApiResponseTypes_ReturnsResponseTypesFromDefaultConventionsController : ControllerBase
        {
            public Task<ActionResult<BaseModel>> DeleteBase(int id) => null;
        }

        [Fact]
        public void GetApiResponseTypes_ReturnsResponseTypesFromCustomConventions()
        {
            // Arrange
            var actionDescriptor = GetControllerActionDescriptor(
                typeof(GetApiResponseTypes_ReturnsResponseTypesFromCustomConventionsController),
                nameof(GetApiResponseTypes_ReturnsResponseTypesFromCustomConventionsController.SearchModel));
            var filter = new FilterDescriptor(new ApiConventionAttribute(typeof(SearchApiConventions)), FilterScope.Controller);
            actionDescriptor.FilterDescriptors.Add(filter);

            var provider = GetProvider();

            // Act
            var result = provider.GetApiResponseTypes(actionDescriptor);

            // Assert
            Assert.Collection(
               result.OrderBy(r => r.StatusCode),
               responseType =>
               {
                   Assert.Equal(206, responseType.StatusCode);
                   Assert.Equal(typeof(void), responseType.Type);
                   Assert.False(responseType.IsDefaultResponse);
                   Assert.Empty(responseType.ApiResponseFormats);
               },
               responseType =>
               {
                   Assert.Equal(406, responseType.StatusCode);
                   Assert.Equal(typeof(void), responseType.Type);
                   Assert.False(responseType.IsDefaultResponse);
                   Assert.Empty(responseType.ApiResponseFormats);
               });
        }

        [ApiConvention(typeof(SearchApiConventions))]
        public class GetApiResponseTypes_ReturnsResponseTypesFromCustomConventionsController : ControllerBase
        {
            public Task<ActionResult<BaseModel>> SearchModel(string searchTerm, int page) => null;
        }

        [Fact]
        public void GetApiResponseTypes_ReturnsResponseTypesFromFirstMatchingConvention_WhenMultipleConventionsArePresent()
        {
            // Arrange
            var actionDescriptor = GetControllerActionDescriptor(
                typeof(GetApiResponseTypes_ReturnsResponseTypesFromFirstMatchingConventionController),
                nameof(GetApiResponseTypes_ReturnsResponseTypesFromFirstMatchingConventionController.SearchModel));
            var filter = new FilterDescriptor(new ApiConventionAttribute(typeof(DefaultApiConventions)), FilterScope.Controller);
            actionDescriptor.FilterDescriptors.Add(filter);
            filter = new FilterDescriptor(new ApiConventionAttribute(typeof(SearchApiConventions)), FilterScope.Controller);
            actionDescriptor.FilterDescriptors.Add(filter);

            var provider = GetProvider();

            // Act
            var result = provider.GetApiResponseTypes(actionDescriptor);

            // Assert
            Assert.Collection(
               result.OrderBy(r => r.StatusCode),
               responseType =>
               {
                   Assert.Equal(206, responseType.StatusCode);
                   Assert.Equal(typeof(void), responseType.Type);
                   Assert.False(responseType.IsDefaultResponse);
                   Assert.Empty(responseType.ApiResponseFormats);
               },
               responseType =>
               {
                   Assert.Equal(406, responseType.StatusCode);
                   Assert.Equal(typeof(void), responseType.Type);
                   Assert.False(responseType.IsDefaultResponse);
                   Assert.Empty(responseType.ApiResponseFormats);
               });
        }

        [ApiConvention(typeof(DefaultApiConventions))]
        [ApiConvention(typeof(SearchApiConventions))]
        public class GetApiResponseTypes_ReturnsResponseTypesFromFirstMatchingConventionController : ControllerBase
        {
            public Task<ActionResult<BaseModel>> Get(int id) => null;

            public Task<ActionResult<BaseModel>> SearchModel(string searchTerm, int page) => null;
        }

        [Fact]
        public void GetApiResponseTypes_ReturnsResponseTypesFromDefaultConvention_WhenMultipleConventionsArePresent()
        {
            // Arrange
            var actionDescriptor = GetControllerActionDescriptor(
                typeof(GetApiResponseTypes_ReturnsResponseTypesFromFirstMatchingConventionController),
                nameof(GetApiResponseTypes_ReturnsResponseTypesFromFirstMatchingConventionController.Get));
            var filter = new FilterDescriptor(new ApiConventionAttribute(typeof(DefaultApiConventions)), FilterScope.Controller);
            actionDescriptor.FilterDescriptors.Add(filter);
            filter = new FilterDescriptor(new ApiConventionAttribute(typeof(SearchApiConventions)), FilterScope.Controller);
            actionDescriptor.FilterDescriptors.Add(filter);

            var provider = GetProvider();

            // Act
            var result = provider.GetApiResponseTypes(actionDescriptor);

            // Assert
            Assert.Collection(
               result.OrderBy(r => r.StatusCode),
               responseType =>
               {
                   Assert.Equal(200, responseType.StatusCode);
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
        public void GetApiResponseTypes_ReturnsDefaultResultsIfNoConventionsMatch()
        {
            // Arrange
            var actionDescriptor = GetControllerActionDescriptor(
                typeof(GetApiResponseTypes_ReturnsDefaultResultsIfNoConventionsMatchController),
                nameof(GetApiResponseTypes_ReturnsDefaultResultsIfNoConventionsMatchController.PostModel));
            var filter = new FilterDescriptor(new ApiConventionAttribute(typeof(DefaultApiConventions)), FilterScope.Controller);
            actionDescriptor.FilterDescriptors.Add(filter);

            var provider = GetProvider();

            // Act
            var result = provider.GetApiResponseTypes(actionDescriptor);

            // Assert
            Assert.Collection(
               result.OrderBy(r => r.StatusCode),
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

        [ApiConvention(typeof(DefaultApiConventions))]
        public class GetApiResponseTypes_ReturnsDefaultResultsIfNoConventionsMatchController : ControllerBase
        {
            public Task<ActionResult<BaseModel>> PostModel(int id, BaseModel model) => null;
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
        }

        private class TestOutputFormatter : OutputFormatter
        {
            public TestOutputFormatter()
            {
                SupportedMediaTypes.Add(new Net.Http.Headers.MediaTypeHeaderValue("application/json"));
            }

            public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context) => Task.CompletedTask;
        }

        public static class SearchApiConventions
        {
            [ProducesResponseType(206)]
            [ProducesResponseType(406)]
            public static void Search(object searchTerm, int page) { }
        }
    }
}
