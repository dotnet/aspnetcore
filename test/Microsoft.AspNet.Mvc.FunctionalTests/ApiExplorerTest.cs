// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ApiExplorerTest
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices("ApiExplorerWebSite");
        private readonly Action<IApplicationBuilder> _app = new ApiExplorer.Startup().Configure;

        [Fact]
        public async Task ApiExplorer_IsVisible_EnabledWithConvention()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerVisbilityEnabledByConvention");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task ApiExplorer_IsVisible_DisabledWithConvention()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerVisbilityDisabledByConvention");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ApiExplorer_IsVisible_DisabledWithAttribute()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerVisibilitySetExplicitly/Disabled");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task ApiExplorer_IsVisible_EnabledWithAttribute()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerVisibilitySetExplicitly/Enabled");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task ApiExplorer_GroupName_SetByConvention()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerNameSetByConvention");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(description.GroupName, "ApiExplorerNameSetByConvention");
        }

        [Fact]
        public async Task ApiExplorer_GroupName_SetByAttributeOnController()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerNameSetExplicitly/SetOnController");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(description.GroupName, "SetOnController");
        }

        [Fact]
        public async Task ApiExplorer_GroupName_SetByAttributeOnAction()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerNameSetExplicitly/SetOnAction");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(description.GroupName, "SetOnAction");
        }

        [Fact]
        public async Task ApiExplorer_RouteTemplate_DisplaysFixedRoute()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerRouteAndPathParametersInformation");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(description.RelativePath, "ApiExplorerRouteAndPathParametersInformation");
        }

        [Fact]
        public async Task ApiExplorer_RouteTemplate_DisplaysRouteWithParameters()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerRouteAndPathParametersInformation/5");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(description.RelativePath, "ApiExplorerRouteAndPathParametersInformation/{id}");

            var parameter = Assert.Single(description.ParameterDescriptions);
            Assert.Equal("id", parameter.Name);
            Assert.False(parameter.IsOptional);
            Assert.Equal("Path", parameter.Source);
            Assert.Null(parameter.ConstraintType);
        }

        [Fact]
        public async Task ApiExplorer_RouteTemplate_StripsInlineConstraintsFromThePath()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var url = "http://localhost/ApiExplorerRouteAndPathParametersInformation/Constraint/5";

            // Act
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal("ApiExplorerRouteAndPathParametersInformation/Constraint/{integer}", description.RelativePath);

            var parameter = Assert.Single(description.ParameterDescriptions);
            Assert.Equal("integer", parameter.Name);
            Assert.False(parameter.IsOptional);
            Assert.Equal("Path", parameter.Source);
            Assert.Equal("IntRouteConstraint", parameter.ConstraintType);
        }

        [Fact]
        public async Task ApiExplorer_RouteTemplate_StripsCatchAllsFromThePath()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var url = "http://localhost/ApiExplorerRouteAndPathParametersInformation/CatchAll/5";

            // Act
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal("ApiExplorerRouteAndPathParametersInformation/CatchAll/{parameter}", description.RelativePath);

            var parameter = Assert.Single(description.ParameterDescriptions);
            Assert.Equal("parameter", parameter.Name);
            Assert.False(parameter.IsOptional);
            Assert.Equal("Path", parameter.Source);
        }

        [Fact]
        public async Task ApiExplorer_RouteTemplate_StripsCatchAllsWithConstraintsFromThePath()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var url = "http://localhost/ApiExplorerRouteAndPathParametersInformation/CatchAllAndConstraint/5";

            // Act
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(
                "ApiExplorerRouteAndPathParametersInformation/CatchAllAndConstraint/{integer}",
                description.RelativePath);

            var parameter = Assert.Single(description.ParameterDescriptions);
            Assert.Equal("integer", parameter.Name);
            Assert.False(parameter.IsOptional);
            Assert.Equal("Path", parameter.Source);
            Assert.Equal("IntRouteConstraint", parameter.ConstraintType);
        }

        [Fact]
        public async Task ApiExplorer_RouteTemplateStripsMultipleConstraints_OnTheSamePathSegment()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var url = "http://localhost/ApiExplorerRouteAndPathParametersInformation/"
                + "MultipleParametersInSegment/12-01-1987";

            var expectedRelativePath = "ApiExplorerRouteAndPathParametersInformation/"
                + "MultipleParametersInSegment/{month}-{day}-{year}";

            // Act
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(expectedRelativePath, description.RelativePath);

            var month = Assert.Single(description.ParameterDescriptions, p => p.Name == "month");
            Assert.False(month.IsOptional);
            Assert.Equal("Path", month.Source);
            Assert.Equal("RangeRouteConstraint", month.ConstraintType);

            var day = Assert.Single(description.ParameterDescriptions, p => p.Name == "day");
            Assert.False(day.IsOptional);
            Assert.Equal("Path", day.Source);
            Assert.Equal("IntRouteConstraint", day.ConstraintType);

            var year = Assert.Single(description.ParameterDescriptions, p => p.Name == "year");
            Assert.False(year.IsOptional);
            Assert.Equal("Path", year.Source);
            Assert.Equal("IntRouteConstraint", year.ConstraintType);
        }

        [Fact]
        public async Task ApiExplorer_RouteTemplateStripsMultipleConstraints_InMultipleSegments()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var url = "http://localhost/ApiExplorerRouteAndPathParametersInformation/"
                + "MultipleParametersInMultipleSegments/12/01/1987";

            var expectedRelativePath = "ApiExplorerRouteAndPathParametersInformation/"
                + "MultipleParametersInMultipleSegments/{month}/{day}/{year}";

            // Act
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(expectedRelativePath, description.RelativePath);

            var month = Assert.Single(description.ParameterDescriptions, p => p.Name == "month");
            Assert.False(month.IsOptional);
            Assert.Equal("Path", month.Source);
            Assert.Equal("RangeRouteConstraint", month.ConstraintType);

            var day = Assert.Single(description.ParameterDescriptions, p => p.Name == "day");
            Assert.False(day.IsOptional);
            Assert.Equal("Path", day.Source);
            Assert.Equal("IntRouteConstraint", day.ConstraintType);

            var year = Assert.Single(description.ParameterDescriptions, p => p.Name == "year");
            Assert.True(year.IsOptional);
            Assert.Equal("Path", year.Source);
            Assert.Equal("IntRouteConstraint", year.ConstraintType);
        }

        [Fact]
        public async Task ApiExplorer_DescribeParameters_FromAllSources()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var url = "http://localhost/ApiExplorerRouteAndPathParametersInformation/MultipleTypesOfParameters/1/2/3";

            var expectedRelativePath = "ApiExplorerRouteAndPathParametersInformation/"
                + "MultipleTypesOfParameters/{path}/{pathAndQuery}/{pathAndFromBody}";

            // Act
            var response = await client.GetAsync(url);

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
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerRouteAndPathParametersInformation/Optional/");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal("ApiExplorerRouteAndPathParametersInformation/Optional/{id}", description.RelativePath);

            var id = Assert.Single(description.ParameterDescriptions, p => p.Name == "id");
            Assert.True(id.IsOptional);
            Assert.Equal("Path", id.Source);
        }

        [Fact]
        public async Task ApiExplorer_HttpMethod_All()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerHttpMethod/All");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Null(description.HttpMethod);
        }

        [Fact]
        public async Task ApiExplorer_HttpMethod_Single()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerHttpMethod/Get");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal("GET", description.HttpMethod);
        }

        // This is hitting one action with two allowed methods (using [AcceptVerbs]). This should
        // return two api descriptions.
        [Theory]
        [InlineData("PUT")]
        [InlineData("POST")]
        public async Task ApiExplorer_HttpMethod_Single(string httpMethod)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            var request = new HttpRequestMessage(
                new HttpMethod(httpMethod),
                "http://localhost/ApiExplorerHttpMethod/Single");

            // Act
            var response = await client.SendAsync(request);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            Assert.Equal(2, result.Count);

            Assert.Single(result, d => d.HttpMethod == "PUT");
            Assert.Single(result, d => d.HttpMethod == "POST");
        }

        [Theory]
        [InlineData("GetVoid")]
        [InlineData("GetTask")]
        public async Task ApiExplorer_ResponseType_VoidWithoutAttribute(string action)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeWithoutAttribute/" + action);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(typeof(void).FullName, description.ResponseType);
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
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeWithoutAttribute/" + action);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Null(description.ResponseType);
        }

        [Theory]
        [InlineData("GetProduct", "ApiExplorer.Product")]
        [InlineData("GetInt", "System.Int32")]
        [InlineData("GetTaskOfProduct", "ApiExplorer.Product")]
        [InlineData("GetTaskOfInt", "System.Int32")]
        public async Task ApiExplorer_ResponseType_KnownWithoutAttribute(string action, string type)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeWithoutAttribute/" + action);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(type, description.ResponseType);
        }

        [Theory]
        [InlineData("GetVoid", "ApiExplorer.Customer")]
        [InlineData("GetObject", "ApiExplorer.Product")]
        [InlineData("GetIActionResult", "System.String")]
        [InlineData("GetProduct", "ApiExplorer.Customer")]
        [InlineData("GetTask", "System.Int32")]
        public async Task ApiExplorer_ResponseType_KnownWithAttribute(string action, string type)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeWithAttribute/" + action);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(type, description.ResponseType);
        }

        [Theory]
        [InlineData("Controller", "ApiExplorer.Product")]
        [InlineData("Action", "ApiExplorer.Customer")]
        public async Task ApiExplorer_ResponseType_OverrideOnAction(string action, string type)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
                "http://localhost/ApiExplorerResponseTypeOverrideOnAction/" + action);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);
            Assert.Equal(type, description.ResponseType);
        }

        [Fact]
        public async Task ApiExplorer_ResponseContentType_Unset()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerResponseContentType/Unset");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);

            var formats = description.SupportedResponseFormats;
            Assert.Equal(4, formats.Count);

            var textXml = Assert.Single(formats, f => f.MediaType == "text/xml");
            Assert.Equal(typeof(XmlDataContractSerializerOutputFormatter).FullName, textXml.FormatterType);
            var applicationXml = Assert.Single(formats, f => f.MediaType == "application/xml");
            Assert.Equal(typeof(XmlDataContractSerializerOutputFormatter).FullName, applicationXml.FormatterType);

            var textJson = Assert.Single(formats, f => f.MediaType == "text/json");
            Assert.Equal(typeof(JsonOutputFormatter).FullName, textJson.FormatterType);
            var applicationJson = Assert.Single(formats, f => f.MediaType == "application/json");
            Assert.Equal(typeof(JsonOutputFormatter).FullName, applicationJson.FormatterType);
        }

        // uses [Produces("*/*")]
        [Fact]
        public async Task ApiExplorer_ResponseContentType_AllTypes()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerResponseContentType/AllTypes");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);

            var formats = description.SupportedResponseFormats;
            Assert.Equal(4, formats.Count);

            var textXml = Assert.Single(formats, f => f.MediaType == "text/xml");
            Assert.Equal(typeof(XmlDataContractSerializerOutputFormatter).FullName, textXml.FormatterType);
            var applicationXml = Assert.Single(formats, f => f.MediaType == "application/xml");
            Assert.Equal(typeof(XmlDataContractSerializerOutputFormatter).FullName, applicationXml.FormatterType);

            var textJson = Assert.Single(formats, f => f.MediaType == "text/json");
            Assert.Equal(typeof(JsonOutputFormatter).FullName, textJson.FormatterType);
            var applicationJson = Assert.Single(formats, f => f.MediaType == "application/json");
            Assert.Equal(typeof(JsonOutputFormatter).FullName, applicationJson.FormatterType);
        }

        [Fact]
        public async Task ApiExplorer_ResponseContentType_Range()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerResponseContentType/Range");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);

            var formats = description.SupportedResponseFormats;
            Assert.Equal(2, formats.Count);

            var textXml = Assert.Single(formats, f => f.MediaType == "text/xml");
            Assert.Equal(typeof(XmlDataContractSerializerOutputFormatter).FullName, textXml.FormatterType);

            var textJson = Assert.Single(formats, f => f.MediaType == "text/json");
            Assert.Equal(typeof(JsonOutputFormatter).FullName, textJson.FormatterType);
        }

        [Fact]
        public async Task ApiExplorer_ResponseContentType_Specific()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerResponseContentType/Specific");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);

            var formats = description.SupportedResponseFormats;
            Assert.Equal(1, formats.Count);

            var applicationJson = Assert.Single(formats, f => f.MediaType == "application/json");
            Assert.Equal(typeof(JsonOutputFormatter).FullName, applicationJson.FormatterType);
        }

        [Fact]
        public async Task ApiExplorer_ResponseContentType_NoMatch()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/ApiExplorerResponseContentType/NoMatch");

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);

            var formats = description.SupportedResponseFormats;
            Assert.Empty(formats);
        }

        [Theory]
        [InlineData("Controller", "text/xml", "Microsoft.AspNet.Mvc.XmlDataContractSerializerOutputFormatter")]
        [InlineData("Action", "application/json", "Microsoft.AspNet.Mvc.JsonOutputFormatter")]
        public async Task ApiExplorer_ResponseContentType_OverrideOnAction(
            string action,
            string contentType,
            string formatterType)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync(
                "http://localhost/ApiExplorerResponseContentTypeOverrideOnAction/" + action);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<List<ApiExplorerData>>(body);

            // Assert
            var description = Assert.Single(result);

            var format = Assert.Single(description.SupportedResponseFormats);
            Assert.Equal(contentType, format.MediaType);
            Assert.Equal(formatterType, format.FormatterType);
        }

        // Used to serialize data between client and server
        private class ApiExplorerData
        {
            public string GroupName { get; set; }

            public string HttpMethod { get; set; }

            public List<ApiExplorerParameterData> ParameterDescriptions { get; } = new List<ApiExplorerParameterData>();

            public string RelativePath { get; set; }

            public string ResponseType { get; set; }

            public List<ApiExplorerResponseData> SupportedResponseFormats { get; } = new List<ApiExplorerResponseData>();
        }

        // Used to serialize data between client and server
        private class ApiExplorerParameterData
        {
            public bool IsOptional { get; set; }

            public string Name { get; set; }

            public string Source { get; set; }

            public string Type { get; set; }

            public string ConstraintType { get; set; }
        }

        // Used to serialize data between client and server
        private class ApiExplorerResponseData
        {
            public string MediaType { get; set; }

            public string FormatterType { get; set; }
        }
    }
}