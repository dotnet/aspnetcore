// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ApiExplorerWebSite;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Testing;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class ApiExplorerTest : IClassFixture<MvcTestFixture<ApiExplorerWebSite.Startup>>
    {
        public ApiExplorerTest(MvcTestFixture<ApiExplorerWebSite.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task ApiExplorer_IsVisible_EnabledWithConvention()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerVisibilityEnabledByConvention");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task ApiExplorer_IsVisible_DisabledWithConvention()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerVisibilityDisabledByConvention");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ApiExplorer_IsVisible_DisabledWithAttribute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerVisibilitySetExplicitly/Disabled");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ApiExplorer_IsVisible_EnabledWithAttribute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerVisibilitySetExplicitly/Enabled");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task ApiExplorer_GroupName_SetByConvention()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerNameSetByConvention");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal("ApiExplorerNameSetByConvention", description.GroupName);
        }

        [Fact]
        public async Task ApiExplorer_GroupName_SetByAttributeOnController()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerNameSetExplicitly/SetOnController");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal("SetOnController", description.GroupName);
        }

        [Fact]
        public async Task ApiExplorer_GroupName_SetByAttributeOnAction()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerNameSetExplicitly/SetOnAction");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal("SetOnAction", description.GroupName);
        }

        [Fact]
        public async Task ApiExplorer_RouteTemplate_DisplaysFixedRoute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerRouteAndPathParametersInformation");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal("ApiExplorerRouteAndPathParametersInformation", description.RelativePath);
        }

        [Fact]
        public async Task ApiExplorer_RouteTemplate_DisplaysRouteWithParameters()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerRouteAndPathParametersInformation/5");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal("ApiExplorerRouteAndPathParametersInformation/{id}", description.RelativePath);

            var parameter = Assert.Single(description.ParameterDescriptions);
            Assert.Equal("id", parameter.Name);
            Assert.False(parameter.RouteInfo.IsOptional);
            Assert.Equal("Path", parameter.Source);
            Assert.Empty(parameter.RouteInfo.ConstraintTypes);
        }

        [Fact]
        public async Task ApiExplorer_RouteTemplate_StripsInlineConstraintsFromThePath()
        {
            // Arrange
            var url = "http://localhost/ApiExplorerRouteAndPathParametersInformation/Constraint/5";

            // Act
            var response = await Client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal("ApiExplorerRouteAndPathParametersInformation/Constraint/{integer}", description.RelativePath);

            var parameter = Assert.Single(description.ParameterDescriptions);
            Assert.Equal("integer", parameter.Name);
            Assert.False(parameter.RouteInfo.IsOptional);
            Assert.Equal("Path", parameter.Source);
            Assert.Equal("IntRouteConstraint", Assert.Single(parameter.RouteInfo.ConstraintTypes));
        }

        [Fact]
        public async Task ApiExplorer_RouteTemplate_StripsCatchAllsFromThePath()
        {
            // Arrange
            var url = "http://localhost/ApiExplorerRouteAndPathParametersInformation/CatchAll/5";

            // Act
            var response = await Client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal("ApiExplorerRouteAndPathParametersInformation/CatchAll/{parameter}", description.RelativePath);

            var parameter = Assert.Single(description.ParameterDescriptions);
            Assert.Equal("parameter", parameter.Name);
            Assert.False(parameter.RouteInfo.IsOptional);
            Assert.Equal("Path", parameter.Source);
        }

        [Fact]
        public async Task ApiExplorer_RouteTemplate_StripsCatchAllsWithConstraintsFromThePath()
        {
            // Arrange
            var url = "http://localhost/ApiExplorerRouteAndPathParametersInformation/CatchAllAndConstraint/5";

            // Act
            var response = await Client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(
                "ApiExplorerRouteAndPathParametersInformation/CatchAllAndConstraint/{integer}",
                description.RelativePath);

            var parameter = Assert.Single(description.ParameterDescriptions);
            Assert.Equal("integer", parameter.Name);
            Assert.False(parameter.RouteInfo.IsOptional);
            Assert.Equal("Path", parameter.Source);
            Assert.Equal("IntRouteConstraint", Assert.Single(parameter.RouteInfo.ConstraintTypes));
        }

        [Fact]
        public async Task ApiExplorer_RouteTemplateStripsMultipleConstraints_OnTheSamePathSegment()
        {
            // Arrange
            var url = "http://localhost/ApiExplorerRouteAndPathParametersInformation/"
                + "MultipleParametersInSegment/12-01-1987";

            var expectedRelativePath = "ApiExplorerRouteAndPathParametersInformation/"
                + "MultipleParametersInSegment/{month}-{day}-{year}";

            // Act
            var response = await Client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(expectedRelativePath, description.RelativePath);

            var month = Assert.Single(description.ParameterDescriptions, p => p.Name == "month");
            Assert.False(month.RouteInfo.IsOptional);
            Assert.Equal("Path", month.Source);
            Assert.Equal("RangeRouteConstraint", Assert.Single(month.RouteInfo.ConstraintTypes));

            var day = Assert.Single(description.ParameterDescriptions, p => p.Name == "day");
            Assert.False(day.RouteInfo.IsOptional);
            Assert.Equal("Path", day.Source);
            Assert.Equal("IntRouteConstraint", Assert.Single(day.RouteInfo.ConstraintTypes));

            var year = Assert.Single(description.ParameterDescriptions, p => p.Name == "year");
            Assert.False(year.RouteInfo.IsOptional);
            Assert.Equal("Path", year.Source);
            Assert.Equal("IntRouteConstraint", Assert.Single(year.RouteInfo.ConstraintTypes));
        }

        [Fact]
        public async Task ApiExplorer_RouteTemplateStripsMultipleConstraints_InMultipleSegments()
        {
            // Arrange
            var url = "http://localhost/ApiExplorerRouteAndPathParametersInformation/"
                + "MultipleParametersInMultipleSegments/12/01/1987";

            var expectedRelativePath = "ApiExplorerRouteAndPathParametersInformation/"
                + "MultipleParametersInMultipleSegments/{month}/{day}/{year}";

            // Act
            var response = await Client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(expectedRelativePath, description.RelativePath);

            var month = Assert.Single(description.ParameterDescriptions, p => p.Name == "month");
            Assert.False(month.RouteInfo.IsOptional);
            Assert.Equal("Path", month.Source);
            Assert.Equal("RangeRouteConstraint", Assert.Single(month.RouteInfo.ConstraintTypes));

            var day = Assert.Single(description.ParameterDescriptions, p => p.Name == "day");
            Assert.True(day.RouteInfo.IsOptional);
            Assert.Equal("ModelBinding", day.Source);
            Assert.Equal("IntRouteConstraint", Assert.Single(day.RouteInfo.ConstraintTypes));

            var year = Assert.Single(description.ParameterDescriptions, p => p.Name == "year");
            Assert.True(year.RouteInfo.IsOptional);
            Assert.Equal("ModelBinding", year.Source);
            Assert.Equal("IntRouteConstraint", Assert.Single(year.RouteInfo.ConstraintTypes));
        }

        [Fact]
        public async Task ApiExplorer_DescribeParameters_FromAllSources()
        {
            // Arrange
            var url = "http://localhost/ApiExplorerRouteAndPathParametersInformation/MultipleTypesOfParameters/1/2/3";

            var expectedRelativePath = "ApiExplorerRouteAndPathParametersInformation/"
                + "MultipleTypesOfParameters/{path}/{pathAndQuery}/{pathAndFromBody}";

            // Act
            var response = await Client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(expectedRelativePath, description.RelativePath);

            var path = Assert.Single(description.ParameterDescriptions, p => p.Name == "path");
            Assert.Equal("Path", path.Source);

            var pathAndQuery = Assert.Single(description.ParameterDescriptions, p => p.Name == "pathAndQuery");
            Assert.Equal("Path", pathAndQuery.Source);

            Assert.Single(description.ParameterDescriptions, p => p.Name == "pathAndFromBody" && p.Source == "Body");
            Assert.Single(description.ParameterDescriptions, p => p.Name == "pathAndFromBody" && p.Source == "Path");
        }

        [Fact]
        public async Task ApiExplorer_RouteTemplate_MakesParametersOptional()
        {
            // Arrange & Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerRouteAndPathParametersInformation/Optional/");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal("ApiExplorerRouteAndPathParametersInformation/Optional/{id}", description.RelativePath);

            var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "id");
            Assert.True(id.RouteInfo.IsOptional);
            Assert.Equal("ModelBinding", id.Source);
        }

        [Fact]
        public async Task ApiExplorer_HttpMethod_All()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerHttpMethod/All");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Null(description.HttpMethod);
        }

        [Fact]
        public async Task ApiExplorer_HttpMethod_Single_GET()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerHttpMethod/Get");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal("GET", description.HttpMethod);
        }

        // This is hitting one action with two allowed methods (using [AcceptVerbs]). This should
        // return two api descriptions.
        [Fact]
        public async Task ApiExplorer_HttpMethod_Single_PUT()
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod("PUT"),
                "http://localhost/ApiExplorerHttpMethod/Single");

            // Act
            var response = await Client.SendAsync(request);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            Assert.Equal(2, result.Count);

            Assert.Single(result, d => d.HttpMethod == "PUT");
            Assert.Single(result, d => d.HttpMethod == "POST");
        }

        // This is hitting one action with two allowed methods (using [AcceptVerbs]). This should
        // return two api descriptions.
        [Fact]
        public async Task ApiExplorer_HttpMethod_Single_POST()
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod("POST"),
                "http://localhost/ApiExplorerHttpMethod/Single");

            // Act
            var response = await Client.SendAsync(request);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            Assert.Equal(2, result.Count);

            Assert.Single(result, d => d.HttpMethod == "PUT");
            Assert.Single(result, d => d.HttpMethod == "POST");
        }

        [Theory]
        [InlineData("GetVoidWithExplicitResponseTypeStatusCode")]
        [InlineData("GetTaskWithExplicitResponseTypeStatusCode")]
        public async Task ApiExplorer_ResponseType_VoidWithResponseTypeAttributeStatusCode(string action)
        {
            // Arrange & Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeWithAttribute/" + action);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            var responseType = Assert.Single(description.SupportedResponseTypes);
            Assert.Equal(typeof(void).FullName, responseType.ResponseType);
            Assert.Equal(204, responseType.StatusCode);
            Assert.Empty(responseType.ResponseFormats);
        }

        [Theory]
        [InlineData("GetVoid")]
        [InlineData("GetTask")]
        public async Task ApiExplorer_ResponseType_VoidWithoutAttributeDefaultStatusCode(string action)
        {
            // Arrange & Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeWithoutAttribute/" + action);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            var responseType = Assert.Single(description.SupportedResponseTypes);
            Assert.Equal(typeof(void).FullName, responseType.ResponseType);
            Assert.Equal(200, responseType.StatusCode);
            Assert.Empty(responseType.ResponseFormats);
        }

        [Theory]
        [InlineData("GetObject")]
        [InlineData("GetIActionResult")]
        [InlineData("GetDerivedActionResult")]
        [InlineData("GetTaskOfObject")]
        [InlineData("GetTaskOfIActionResult")]
        [InlineData("GetTaskOfDerivedActionResult")]
        public async Task ApiExplorer_ResponseType_UnknownWithoutAttribute(string action)
        {
            // Arrange & Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeWithoutAttribute/" + action);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Empty(description.SupportedResponseTypes);
        }

        [Theory]
        [InlineData("GetProduct", "ApiExplorerWebSite.Product")]
        [InlineData("GetActionResultProduct", "ApiExplorerWebSite.Product")]
        [InlineData("GetInt", "System.Int32")]
        [InlineData("GetTaskOfProduct", "ApiExplorerWebSite.Product")]
        [InlineData("GetTaskOfInt", "System.Int32")]
        public async Task ApiExplorer_ResponseType_KnownWithoutAttribute(string action, string type)
        {
            // Arrange & Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeWithoutAttribute/" + action);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);
            var expectedMediaTypes = new[] { "application/json", "application/xml", "text/json", "text/xml" };

            // Assert
            var description = Assert.Single(result);
            var responseType = Assert.Single(description.SupportedResponseTypes);
            Assert.Equal(200, responseType.StatusCode);
            Assert.Equal(type, responseType.ResponseType);
            Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
        }

        [Fact]
        public async Task ApiExplorer_ResponseType_KnownWithoutAttribute_ReturnVoid()
        {
            // Arrange
            var type = "ApiExplorerWebSite.Customer";
            var expectedMediaTypes = new[] { "application/json", "application/xml", "text/json", "text/xml" };

            // Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeWithAttribute/GetVoid");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            var responseType = Assert.Single(description.SupportedResponseTypes);
            Assert.Equal(200, responseType.StatusCode);
            Assert.Equal(type, responseType.ResponseType);
            Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
        }

        [Fact]
        public async Task ApiExplorer_ResponseType_DifferentOnAttributeThanReturnType()
        {
            // Arrange
            var type = "ApiExplorerWebSite.Customer";
            var expectedMediaTypes = new[] { "application/json", "application/xml", "text/json", "text/xml" };

            // Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeWithAttribute/GetProduct");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            var responseType = Assert.Single(description.SupportedResponseTypes);
            Assert.Equal(200, responseType.StatusCode);
            Assert.Equal(type, responseType.ResponseType);
            Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
        }

        [Theory]
        [InlineData("GetObject", "ApiExplorerWebSite.Product")]
        [InlineData("GetIActionResult", "System.String")]
        [InlineData("GetTask", "System.Int32")]
        public async Task ApiExplorer_ResponseType_KnownWithAttribute(string action, string type)
        {
            // Arrange & Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeWithAttribute/" + action);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            var responseType = Assert.Single(description.SupportedResponseTypes);
            Assert.Equal(type, responseType.ResponseType);
            Assert.Equal(200, responseType.StatusCode);
            var responseFormat = Assert.Single(responseType.ResponseFormats);
            Assert.Equal("application/json", responseFormat.MediaType);
        }

        [Fact]
        public async Task ExplicitResponseTypeDecoration_SuppressesDefaultStatus()
        {
            // Arrange
            var type1 = typeof(ApiExplorerWebSite.Product).FullName;
            var type2 = typeof(SerializableError).FullName;
            var expectedMediaTypes = new[] { "application/json", "application/xml", "text/json", "text/xml" };

            // Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeWithAttribute/CreateProductWithDefaultResponseContentTypes");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(2, description.SupportedResponseTypes.Count);
            var responseType = description.SupportedResponseTypes[0];
            Assert.Equal(type1, responseType.ResponseType);
            Assert.Equal(201, responseType.StatusCode);
            Assert.Equal(
                expectedMediaTypes,
                responseType.ResponseFormats.Select(responseFormat => responseFormat.MediaType).OrderBy(o => o).ToArray());
            responseType = description.SupportedResponseTypes[1];
            Assert.Equal(type2, responseType.ResponseType);
            Assert.Equal(400, responseType.StatusCode);
            Assert.Equal(
                expectedMediaTypes,
                responseType.ResponseFormats.Select(responseFormat => responseFormat.MediaType).OrderBy(o => o).ToArray());
        }

        [Fact]
        public async Task ExplicitResponseTypeDecoration_SuppressesDefaultStatus_AlsoHonorsProducesContentTypes()
        {
            // Arrange
            var type1 = typeof(ApiExplorerWebSite.Product).FullName;
            var type2 = typeof(SerializableError).FullName;
            var expectedMediaTypes = new[] { "text/xml" };

            // Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeWithAttribute/CreateProductWithLimitedResponseContentTypes");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(2, description.SupportedResponseTypes.Count);
            var responseType = description.SupportedResponseTypes[0];
            Assert.Equal(type1, responseType.ResponseType);
            Assert.Equal(201, responseType.StatusCode);
            Assert.Equal(
                expectedMediaTypes,
                responseType.ResponseFormats.Select(responseFormat => responseFormat.MediaType).ToArray());
            responseType = description.SupportedResponseTypes[1];
            Assert.Equal(type2, responseType.ResponseType);
            Assert.Equal(400, responseType.StatusCode);
            Assert.Equal(
                expectedMediaTypes,
                responseType.ResponseFormats.Select(responseFormat => responseFormat.MediaType).ToArray());
        }

        [Fact]
        public async Task ExplicitResponseTypeDecoration_WithExplicitDefaultStatus()
        {
            // Arrange
            var type1 = typeof(ApiExplorerWebSite.Product).FullName;
            var type2 = typeof(SerializableError).FullName;
            var expectedMediaTypes = new[] { "application/json", "application/xml", "text/json", "text/xml" };

            // Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeWithAttribute/UpdateProductWithDefaultResponseContentTypes");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(2, description.SupportedResponseTypes.Count);
            var responseType = description.SupportedResponseTypes[0];
            Assert.Equal(type1, responseType.ResponseType);
            Assert.Equal(200, responseType.StatusCode);
            Assert.Equal(
                expectedMediaTypes,
                responseType.ResponseFormats.Select(responseFormat => responseFormat.MediaType).OrderBy(o => o).ToArray());
            responseType = description.SupportedResponseTypes[1];
            Assert.Equal(type2, responseType.ResponseType);
            Assert.Equal(400, responseType.StatusCode);
            Assert.Equal(
                expectedMediaTypes,
                responseType.ResponseFormats.Select(responseFormat => responseFormat.MediaType).OrderBy(o => o).ToArray());
        }

        [Fact]
        public async Task ExplicitResponseTypeDecoration_WithExplicitDefaultStatus_SpecifiedViaProducesAttribute()
        {
            // Arrange
            var type1 = typeof(ApiExplorerWebSite.Product).FullName;
            var type2 = typeof(SerializableError).FullName;
            var expectedMediaTypes = new[] { "text/xml" };

            // Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeWithAttribute/UpdateProductWithLimitedResponseContentTypes");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(2, description.SupportedResponseTypes.Count);
            var responseType = description.SupportedResponseTypes[0];
            Assert.Equal(type1, responseType.ResponseType);
            Assert.Equal(200, responseType.StatusCode);
            Assert.Equal(
                expectedMediaTypes,
                responseType.ResponseFormats.Select(responseFormat => responseFormat.MediaType).ToArray());
            responseType = description.SupportedResponseTypes[1];
            Assert.Equal(type2, responseType.ResponseType);
            Assert.Equal(400, responseType.StatusCode);
            Assert.Equal(
                expectedMediaTypes,
                responseType.ResponseFormats.Select(responseFormat => responseFormat.MediaType).ToArray());
        }
        [Fact]
        public async Task ApiExplorer_ResponseType_InheritingFromController()
        {
            // Arrange
            var type = "ApiExplorerWebSite.Product";
            var errorType = "ApiExplorerWebSite.ErrorInfo";

            // Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeOverrideOnAction/Controller");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);

            Assert.Collection(
                description.SupportedResponseTypes.OrderBy(responseType => responseType.StatusCode),
                responseType =>
                {
                    Assert.Equal(type, responseType.ResponseType);
                    Assert.Equal(200, responseType.StatusCode);
                    var responseFormat = Assert.Single(responseType.ResponseFormats);
                    Assert.Equal("application/json", responseFormat.MediaType);
                },
                responseType =>
                {
                    Assert.Equal(errorType, responseType.ResponseType);
                    Assert.Equal(500, responseType.StatusCode);
                    var responseFormat = Assert.Single(responseType.ResponseFormats);
                    Assert.Equal("application/json", responseFormat.MediaType);
                });
        }

        [Fact]
        public async Task ApiExplorer_ResponseType_OverrideOnAction()
        {
            // Arrange
            var type = "ApiExplorerWebSite.Customer";
            // type overriding the one specified on the controller
            var errorType = "ApiExplorerWebSite.ErrorInfoOverride";
            var expectedMediaTypes = new[] { "application/json", "application/xml", "text/json", "text/xml" };

            // Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeOverrideOnAction/Action");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);

            Assert.Collection(
                description.SupportedResponseTypes.OrderBy(responseType => responseType.StatusCode),
                responseType =>
                {
                    Assert.Equal(type, responseType.ResponseType);
                    Assert.Equal(200, responseType.StatusCode);
                    Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
                },
                responseType =>
                {
                    Assert.Equal(errorType, responseType.ResponseType);
                    Assert.Equal(500, responseType.StatusCode);
                    Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
                });
        }

        [ConditionalFact]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        public async Task ApiExplorer_ResponseContentType_Unset()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerResponseContentType/Unset");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);

            var responseType = Assert.Single(description.SupportedResponseTypes);
            Assert.Equal(4, responseType.ResponseFormats.Count);

            var textXml = Assert.Single(responseType.ResponseFormats, f => f.MediaType == "text/xml");
            Assert.Equal(typeof(XmlDataContractSerializerOutputFormatter).FullName, textXml.FormatterType);
            var applicationXml = Assert.Single(responseType.ResponseFormats, f => f.MediaType == "application/xml");
            Assert.Equal(typeof(XmlDataContractSerializerOutputFormatter).FullName, applicationXml.FormatterType);

            var textJson = Assert.Single(responseType.ResponseFormats, f => f.MediaType == "text/json");
            Assert.Equal(typeof(NewtonsoftJsonOutputFormatter).FullName, textJson.FormatterType);
            var applicationJson = Assert.Single(responseType.ResponseFormats, f => f.MediaType == "application/json");
            Assert.Equal(typeof(NewtonsoftJsonOutputFormatter).FullName, applicationJson.FormatterType);
        }

        [Fact]
        public async Task ApiExplorer_ResponseContentType_Specific()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerResponseContentType/Specific");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);

            var responseType = Assert.Single(description.SupportedResponseTypes);
            Assert.Equal(2, responseType.ResponseFormats.Count);

            var applicationJson = Assert.Single(
                responseType.ResponseFormats,
                format => format.MediaType == "application/json");
            Assert.Equal(typeof(NewtonsoftJsonOutputFormatter).FullName, applicationJson.FormatterType);

            var textJson = Assert.Single(responseType.ResponseFormats, f => f.MediaType == "text/json");
            Assert.Equal(typeof(NewtonsoftJsonOutputFormatter).FullName, textJson.FormatterType);
        }

        [Fact]
        public async Task ApiExplorer_ResponseContentType_WildcardMatch()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerResponseContentType/WildcardMatch");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            var responseType = Assert.Single(description.SupportedResponseTypes);
            Assert.Collection(
                responseType.ResponseFormats,
                responseFormat =>
                {
                    Assert.Equal("application/hal+custom", responseFormat.MediaType);
                    Assert.Null(responseFormat.FormatterType);
                },
                responseFormat =>
                {
                    Assert.Equal("application/hal+json", responseFormat.MediaType);
                    Assert.Equal(typeof(NewtonsoftJsonOutputFormatter).FullName, responseFormat.FormatterType);
                });
        }

        [Fact]
        public async Task ApiExplorer_ResponseContentType_NoMatch()
        {
            // Arrange
            var expectedMediaTypes = new[] { "application/custom", "text/hal+bson" };

            // Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerResponseContentType/NoMatch");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            var responseType = Assert.Single(description.SupportedResponseTypes);


            Assert.Equal(typeof(Product).FullName, responseType.ResponseType);
            Assert.Equal(200, responseType.StatusCode);
            Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
        }

        [ConditionalTheory]
        // Mono issue - https://github.com/aspnet/External/issues/18
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [InlineData("Controller", "text/xml", typeof(XmlDataContractSerializerOutputFormatter))]
        [InlineData("Action", "application/json", typeof(NewtonsoftJsonOutputFormatter))]
        public async Task ApiExplorer_ResponseContentType_OverrideOnAction(
            string action,
            string contentType,
            Type formatterType)
        {
            // Arrange & Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerResponseContentTypeOverrideOnAction/" + action);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);

            var responseType = Assert.Single(description.SupportedResponseTypes);
            var responseFormat = Assert.Single(responseType.ResponseFormats);
            Assert.Equal(contentType, responseFormat.MediaType);
            Assert.Equal(formatterType.FullName, responseFormat.FormatterType);
        }

        [Fact]
        public async Task ApiExplorer_Parameters_SimpleTypes_Default()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerParameters/SimpleParameters");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            var parameters = description.ParameterDescriptions;

            Assert.Equal(2, parameters.Count);

            var i = Assert.Single(parameters, p => p.Name == "i");
            Assert.Equal(BindingSource.ModelBinding.Id, i.Source);
            Assert.Equal(typeof(int).FullName, i.Type);

            var s = Assert.Single(parameters, p => p.Name == "s");
            Assert.Equal(BindingSource.ModelBinding.Id, s.Source);
            Assert.Equal(typeof(string).FullName, s.Type);
        }

        [Fact]
        public async Task ApiExplorer_Parameters_SimpleTypes_BinderMetadataOnParameters()
        {
            // Arrange & Act
            var response = await Client.GetAsync(
                "http://localhost/ApiExplorerParameters/SimpleParametersWithBinderMetadata");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            var parameters = description.ParameterDescriptions;

            Assert.Equal(2, parameters.Count);

            var i = Assert.Single(parameters, p => p.Name == "i");
            Assert.Equal(BindingSource.Query.Id, i.Source);
            Assert.Equal(typeof(int).FullName, i.Type);

            var s = Assert.Single(parameters, p => p.Name == "s");
            Assert.Equal(BindingSource.Path.Id, s.Source);
            Assert.Equal(typeof(string).FullName, s.Type);
        }

        [Fact]
        public async Task ApiExplorer_ParametersSimpleModel()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerParameters/SimpleModel");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            var parameters = description.ParameterDescriptions;

            Assert.Equal(2, parameters.Count);

            var id = Assert.Single(parameters, p => p.Name == "Id");
            Assert.Equal(BindingSource.ModelBinding.Id, id.Source);
            Assert.Equal(typeof(int).FullName, id.Type);

            var name = Assert.Single(parameters, p => p.Name == "Name");
            Assert.Equal(BindingSource.ModelBinding.Id, name.Source);
            Assert.Equal(typeof(string).FullName, name.Type);
        }

        [Fact]
        public async Task ApiExplorer_Parameters_SimpleTypes_SimpleModel_FromBody()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerParameters/SimpleModelFromBody/5");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            var parameters = description.ParameterDescriptions;

            Assert.Equal(2, parameters.Count);

            var id = Assert.Single(parameters, p => p.Name == "id");
            Assert.Equal(BindingSource.Path.Id, id.Source);
            Assert.Equal(typeof(int).FullName, id.Type);

            var product = Assert.Single(parameters, p => p.Name == "product");
            Assert.Equal(BindingSource.Body.Id, product.Source);
            Assert.Equal(typeof(ApiExplorerWebSite.Product).FullName, product.Type);
        }

        [Fact]
        public async Task ApiExplorer_Parameters_SimpleTypes_ComplexModel()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApiExplorerParameters/ComplexModel");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            var parameters = description.ParameterDescriptions;

            Assert.Equal(7, parameters.Count);

            var customerId = Assert.Single(parameters, p => p.Name == "CustomerId");
            Assert.Equal(BindingSource.Query.Id, customerId.Source);
            Assert.Equal(typeof(string).FullName, customerId.Type);

            var referrer = Assert.Single(parameters, p => p.Name == "Referrer");
            Assert.Equal(BindingSource.Header.Id, referrer.Source);
            Assert.Equal(typeof(string).FullName, referrer.Type);

            var quantity = Assert.Single(parameters, p => p.Name == "Details.Quantity");
            Assert.Equal(BindingSource.Form.Id, quantity.Source);
            Assert.Equal(typeof(int).FullName, quantity.Type);

            var productId = Assert.Single(parameters, p => p.Name == "Details.Product.Id");
            Assert.Equal(BindingSource.Form.Id, productId.Source);
            Assert.Equal(typeof(int).FullName, productId.Type);

            var productName = Assert.Single(parameters, p => p.Name == "Details.Product.Name");
            Assert.Equal(BindingSource.Form.Id, productName.Source);
            Assert.Equal(typeof(string).FullName, productName.Type);

            var shippingInstructions = Assert.Single(parameters, p => p.Name == "Comments.ShippingInstructions");
            Assert.Equal(BindingSource.Query.Id, shippingInstructions.Source);
            Assert.Equal(typeof(string).FullName, shippingInstructions.Type);

            var feedback = Assert.Single(parameters, p => p.Name == "Comments.Feedback");
            Assert.Equal(BindingSource.Form.Id, feedback.Source);
            Assert.Equal(typeof(string).FullName, feedback.Type);
        }

        [Fact]
        public async Task ApiExplorer_Parameters_DefaultValue()
        {
            // Arrange & Act
            var response = await Client.GetAsync("ApiExplorerParameters/DefaultValueParameters");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            var parameters = description.ParameterDescriptions;

            Assert.Collection(
                parameters,
                parameter =>
                {
                    Assert.Equal("searchTerm", parameter.Name);
                    Assert.Null(parameter.DefaultValue);
                },
                parameter =>
                {
                    Assert.Equal("top", parameter.Name);
                    Assert.Equal("10", parameter.DefaultValue);
                },
                parameter =>
                {
                    Assert.Equal("searchDay", parameter.Name);
                    Assert.Equal(nameof(DayOfWeek.Wednesday), parameter.DefaultValue);
                });
        }

        [Fact]
        public async Task ApiExplorer_Parameters_IsRequired()
        {
            // Arrange & Act
            var response = await Client.GetAsync("ApiExplorerParameters/IsRequiredParameters");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            var parameters = description.ParameterDescriptions;

            Assert.Collection(
                parameters,
                parameter =>
                {
                    Assert.Equal("requiredParam", parameter.Name);
                    Assert.True(parameter.IsRequired);
                },
                parameter =>
                {
                    Assert.Equal("notRequiredParam", parameter.Name);
                    Assert.False(parameter.IsRequired);
                },
                parameter =>
                {
                    Assert.Equal("Id", parameter.Name);
                    Assert.True(parameter.IsRequired);
                },
                parameter =>
                {
                    Assert.Equal("Name", parameter.Name);
                    Assert.False(parameter.IsRequired);
                });
        }

        [Fact]
        public async Task ApiExplorer_Updates_WhenActionDescriptorCollectionIsUpdated()
        {
            // Act - 1
            var body = await Client.GetStringAsync("ApiExplorerReload/Index");
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert - 1
            var description = Assert.Single(result);
            Assert.Empty(description.ParameterDescriptions);
            Assert.Equal("ApiExplorerReload/Index", description.RelativePath);

            // Act - 2
            var response = await Client.GetAsync("ApiExplorerReload/Reload");

            // Assert - 2
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Act - 3
            response = await Client.GetAsync("ApiExplorerReload/Index");

            // Assert - 3
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            // Act - 4
            body = await Client.GetStringAsync("ApiExplorerReload/NewIndex");
            result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert - 4
            description = Assert.Single(result);
            Assert.Empty(description.ParameterDescriptions);
            Assert.Equal("ApiExplorerReload/NewIndex", description.RelativePath);
        }

        [Fact]
        public async Task ApiExplorer_DoesNotListActionsSuppressedForPathMatching()
        {
            // Act
            var body = await Client.GetStringAsync("ApiExplorerInboundOutbound/SuppressedForLinkGeneration");
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Empty(description.ParameterDescriptions);
            Assert.Equal("ApiExplorerInboundOutbound/SuppressedForLinkGeneration", description.RelativePath);
        }

        [Fact]
        public async Task ApiBehavior_AddsMultipartFormDataConsumesConstraint_ForActionsWithFormFileParameters()
        {
            // Act
            var body = await Client.GetStringAsync("ApiExplorerApiController/ActionWithFormFileCollectionParameter");
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            var requestFormat = Assert.Single(description.SupportedRequestFormats);
            Assert.Equal("multipart/form-data", requestFormat.MediaType);
        }

        [Fact]
        public async Task ApiBehavior_UsesContentTypeFromProducesAttribute_WhenNoFormatterSupportsIt()
        {
            // Arrange
            var expectedMediaTypes = new[] { "application/pdf" };

            // Act
            var body = await Client.GetStringAsync("ApiExplorerApiController/ProducesWithUnsupportedContentType");
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Collection(
                description.SupportedResponseTypes.OrderBy(r => r.StatusCode),
                responseType =>
                {
                    Assert.Equal(typeof(Stream).FullName, responseType.ResponseType);
                    Assert.Equal(200, responseType.StatusCode);
                    Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
                });
        }

        [Fact]
        public Task ApiConvention_ForGetMethod_ReturningModel() => ApiConvention_ForGetMethod("GetProduct");

        [Fact]
        public Task ApiConvention_ForGetMethod_ReturningTaskOfActionResultOfModel() => ApiConvention_ForGetMethod("GetTaskOfActionResultOfProduct");

        private async Task ApiConvention_ForGetMethod(string action)
        {
            // Arrange
            var expectedMediaTypes = new[] { "application/json", "application/xml", "text/json", "text/xml" };

            // Act
            var response = await Client.GetStringAsync(
                $"ApiExplorerResponseTypeWithApiConventionController/{action}");
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(response);

            // Assert
            var description = Assert.Single(result);

            Assert.Collection(
                description.SupportedResponseTypes.OrderBy(r => r.StatusCode),
                responseType =>
                {
                    Assert.True(responseType.IsDefaultResponse);
                },
                responseType =>
                {
                    Assert.Equal(typeof(Product).FullName, responseType.ResponseType);
                    Assert.Equal(200, responseType.StatusCode);
                    Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
                },
                responseType =>
                {
                    Assert.Equal(typeof(ProblemDetails).FullName, responseType.ResponseType);
                    Assert.Equal(404, responseType.StatusCode);
                    Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
                });
        }

        [Fact]
        public async Task ApiConvention_ForGetMethodThatDoesNotMatchConvention()
        {
            // Arrange
            var expectedMediaTypes = new[] { "application/json", "application/xml", "text/json", "text/xml" };

            // Act
            var response = await Client.GetStringAsync(
                $"ApiExplorerResponseTypeWithApiConventionController/GetProducts");
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(response);

            // Assert
            var description = Assert.Single(result);

            Assert.Collection(
                description.SupportedResponseTypes.OrderBy(r => r.StatusCode),
                responseType =>
                {
                    Assert.Equal(typeof(IEnumerable<Product>).FullName, responseType.ResponseType);
                    Assert.Equal(200, responseType.StatusCode);
                    var actualMediaTypes = responseType.ResponseFormats.Select(r => r.MediaType).OrderBy(r => r);
                    Assert.Equal(expectedMediaTypes, actualMediaTypes);
                });
        }

        [Fact]
        public async Task ApiConvention_ForMethodWithResponseTypeAttributes()
        {
            // Arrange
            var expectedMediaTypes = new[] { "application/json" };

            // Act
            var response = await Client.PostAsync(
                $"ApiExplorerResponseTypeWithApiConventionController/PostWithConventions",
                new StringContent(string.Empty));
            var responseBody = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(responseBody);

            // Assert
            var description = Assert.Single(result);
            Assert.Collection(
                description.SupportedResponseTypes.OrderBy(r => r.StatusCode),
                responseType =>
                {
                    Assert.Equal(typeof(void).FullName, responseType.ResponseType);
                    Assert.Equal(202, responseType.StatusCode);
                    Assert.Empty(responseType.ResponseFormats);
                },
                responseType =>
                {
                    Assert.Equal(typeof(ProblemDetails).FullName, responseType.ResponseType);
                    Assert.Equal(403, responseType.StatusCode);
                    Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
                });
        }

        [Fact]
        public async Task ApiConvention_ForPostMethodThatMatchesConvention()
        {
            // Arrange
            var expectedMediaTypes = new[] { "application/json", "application/xml", "text/json", "text/xml" };

            // Act
            var response = await Client.PostAsync(
                $"ApiExplorerResponseTypeWithApiConventionController/PostTaskOfProduct",
                new StringContent(string.Empty));
            var responseBody = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(responseBody);

            // Assert
            var description = Assert.Single(result);
            Assert.Collection(
                description.SupportedResponseTypes.OrderBy(r => r.StatusCode),
                responseType =>
                {
                    Assert.True(responseType.IsDefaultResponse);
                },
                responseType =>
                {
                    Assert.Equal(typeof(void).FullName, responseType.ResponseType);
                    Assert.Equal(201, responseType.StatusCode);
                    Assert.Empty(responseType.ResponseFormats);
                },
                responseType =>
                {
                    Assert.Equal(typeof(ProblemDetails).FullName, responseType.ResponseType);
                    Assert.Equal(400, responseType.StatusCode);
                    Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
                });
        }

        [Fact]
        public async Task ApiConvention_ForPostActionWithProducesAttribute()
        {
            // Arrange
            var expectedMediaTypes = new[] { "application/json", "text/json", };

            // Act
            var response = await Client.PostAsync(
                $"ApiExplorerResponseTypeWithApiConventionController/PostWithProduces",
                new StringContent(string.Empty));
            var responseBody = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(responseBody);

            // Assert
            var description = Assert.Single(result);
            Assert.Collection(
                description.SupportedResponseTypes.OrderBy(r => r.StatusCode),
                responseType =>
                {
                    Assert.True(responseType.IsDefaultResponse);
                },
                responseType =>
                {
                    Assert.Equal(typeof(void).FullName, responseType.ResponseType);
                    Assert.Equal(201, responseType.StatusCode);
                    Assert.Empty(responseType.ResponseFormats);
                },
                responseType =>
                {
                    Assert.Equal(typeof(ProblemDetails).FullName, responseType.ResponseType);
                    Assert.Equal(400, responseType.StatusCode);
                    Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
                });
        }

        [Fact]
        public async Task ApiConvention_ForPutActionThatMatchesConvention()
        {
            // Arrange
            var expectedMediaTypes = new[] { "application/json", "application/xml", "text/json", "text/xml" };

            // Act
            var response = await Client.PutAsync(
                $"ApiExplorerResponseTypeWithApiConventionController/Put",
                new StringContent(string.Empty));
            var responseBody = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(responseBody);

            // Assert
            var description = Assert.Single(result);
            Assert.Collection(
                description.SupportedResponseTypes.OrderBy(r => r.StatusCode),
                responseType =>
                {
                    Assert.True(responseType.IsDefaultResponse);
                },
                responseType =>
                {
                    Assert.Equal(typeof(void).FullName, responseType.ResponseType);
                    Assert.Equal(204, responseType.StatusCode);
                    Assert.Empty(responseType.ResponseFormats);
                },
                responseType =>
                {
                    Assert.Equal(typeof(ProblemDetails).FullName, responseType.ResponseType);
                    Assert.Equal(400, responseType.StatusCode);
                    Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
                },
                responseType =>
                {
                    Assert.Equal(typeof(ProblemDetails).FullName, responseType.ResponseType);
                    Assert.Equal(404, responseType.StatusCode);
                    Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
                });
        }

        [Fact]
        public async Task ApiConvention_ForDeleteActionThatMatchesConvention()
        {
            // Arrange
            var expectedMediaTypes = new[] { "application/json", "application/xml", "text/json", "text/xml" };

            // Act
            var response = await Client.DeleteAsync(
                $"ApiExplorerResponseTypeWithApiConventionController/DeleteProduct");
            var responseBody = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(responseBody);

            // Assert
            var description = Assert.Single(result);
            Assert.Collection(
                description.SupportedResponseTypes.OrderBy(r => r.StatusCode),
                responseType =>
                {
                    Assert.True(responseType.IsDefaultResponse);
                },
                responseType =>
                {
                    Assert.Equal(typeof(void).FullName, responseType.ResponseType);
                    Assert.Equal(200, responseType.StatusCode);
                    Assert.Empty(responseType.ResponseFormats);
                },
                responseType =>
                {
                    Assert.Equal(typeof(ProblemDetails).FullName, responseType.ResponseType);
                    Assert.Equal(400, responseType.StatusCode);
                    Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
                },
                responseType =>
                {
                    Assert.Equal(typeof(ProblemDetails).FullName, responseType.ResponseType);
                    Assert.Equal(404, responseType.StatusCode);
                    Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
                });
        }

        [Fact]
        public async Task ApiConvention_ForActionWithApiConventionMethod()
        {
            // Arrange
            var expectedMediaTypes = new[] { "application/json", "application/xml", "text/json", "text/xml" };

            // Act
            var response = await Client.PostAsync(
                "ApiExplorerResponseTypeWithApiConventionController/PostItem",
                new StringContent(string.Empty));
            var responseBody = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(responseBody);

            // Assert
            var description = Assert.Single(result);
            Assert.Collection(
                description.SupportedResponseTypes.OrderBy(r => r.StatusCode),
                responseType =>
                {
                    Assert.Equal(typeof(void).FullName, responseType.ResponseType);
                    Assert.Equal(302, responseType.StatusCode);
                    Assert.Empty(responseType.ResponseFormats);
                },
                responseType =>
                {
                    Assert.Equal(typeof(ProblemDetails).FullName, responseType.ResponseType);
                    Assert.Equal(409, responseType.StatusCode);
                    Assert.Equal(expectedMediaTypes, GetSortedMediaTypes(responseType));
                });
        }

        private IEnumerable<string> GetSortedMediaTypes(ApiExplorerResponseType apiResponseType)
        {
            return apiResponseType.ResponseFormats
                .OrderBy(format => format.MediaType)
                .Select(format => format.MediaType);
        }

        // Used to serialize data between client and server
        private class ApiExplorerData
        {
            public string GroupName { get; set; }

            public string HttpMethod { get; set; }

            public List<ApiExplorerParameterData> ParameterDescriptions { get; } = new List<ApiExplorerParameterData>();

            public string RelativePath { get; set; }

            public List<ApiExplorerResponseType> SupportedResponseTypes { get; } = new List<ApiExplorerResponseType>();

            public List<ApiExplorerRequestFormat> SupportedRequestFormats { get; } = new List<ApiExplorerRequestFormat>();
        }

        // Used to serialize data between client and server
        private class ApiExplorerParameterData
        {
            public string Name { get; set; }

            public ApiExplorerParameterRouteInfo RouteInfo { get; set; }

            public string Source { get; set; }

            public string Type { get; set; }

            public string DefaultValue { get; set; }

            public bool IsRequired { get; set; }
        }

        // Used to serialize data between client and server
        private class ApiExplorerParameterRouteInfo
        {
            public string[] ConstraintTypes { get; set; }

            public object DefaultValue { get; set; }

            public bool IsOptional { get; set; }
        }

        // Used to serialize data between client and server
        private class ApiExplorerResponseType
        {
            public IList<ApiExplorerResponseFormat> ResponseFormats { get; }
                = new List<ApiExplorerResponseFormat>();

            public string ResponseType { get; set; }

            public int StatusCode { get; set; }

            public bool IsDefaultResponse { get; set; }
        }

        private class ApiExplorerResponseFormat
        {
            public string MediaType { get; set; }

            public string FormatterType { get; set; }
        }

        private class ApiExplorerRequestFormat
        {
            public string MediaType { get; set; }

            public string FormatterType { get; set; }
        }
    }
}
