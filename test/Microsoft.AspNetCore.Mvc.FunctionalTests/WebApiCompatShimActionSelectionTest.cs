// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class WebApiCompatShimActionSelectionTest : IClassFixture<MvcTestFixture<WebApiCompatShimWebSite.Startup>>
    {
        public WebApiCompatShimActionSelectionTest(MvcTestFixture<WebApiCompatShimWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Theory]
        [InlineData("GET", "GetItems")]
        [InlineData("PUT", "PutItems")]
        [InlineData("POST", "PostItems")]
        [InlineData("DELETE", "DeleteItems")]
        [InlineData("PATCH", "PatchItems")]
        [InlineData("HEAD", "HeadItems")]
        [InlineData("OPTIONS", "OptionsItems")]
        public async Task WebAPIConvention_TakesHttpMethodFromPrefix_UnnamedAction(string httpMethod, string actionName)
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod(httpMethod),
                "http://localhost/api/Admin/WebAPIActionConventions");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(actionName, result.ActionName);
        }

        [Theory]
        [InlineData("GET", "GetItems")]
        [InlineData("PUT", "PutItems")]
        [InlineData("POST", "PostItems")]
        [InlineData("DELETE", "DeleteItems")]
        [InlineData("PATCH", "PatchItems")]
        [InlineData("HEAD", "HeadItems")]
        [InlineData("OPTIONS", "OptionsItems")]
        public async Task WebAPIConvention_TakesHttpMethodFromPrefix_NamedAction(string httpMethod, string actionName)
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod(httpMethod),
                "http://localhost/api/Blog/WebAPIActionConventions/" + actionName);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(actionName, result.ActionName);
        }

        [Fact]
        public async Task WebAPIConvention_TakesHttpMethodFromPrefix_NamedAction_MismatchedVerb()
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod("POST"),
                "http://localhost/api/Blog/WebAPIActionConventions/GetItems");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task WebAPIConvention_TakesHttpMethodFromPrefix_UnnamedAction_DefaultVerbIsPost_Success()
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod("POST"),
                "http://localhost/api/Admin/WebApiActionConventionsDefaultPost");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("DefaultVerbIsPost", result.ActionName);
        }

        [Fact]
        public async Task WebAPIConvention_TakesHttpMethodFromPrefix_NamedAction_DefaultVerbIsPost_Success()
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod("POST"),
                "http://localhost/api/Blog/WebAPIActionConventionsDefaultPost/DefaultVerbIsPost");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("DefaultVerbIsPost", result.ActionName);
        }

        [Fact]
        public async Task WebAPIConvention_TakesHttpMethodFromPrefix_UnnamedAction_DefaultVerbIsPost_VerbMismatch()
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod("GET"),
                "http://localhost/api/Admin/WebApiActionConventionsDefaultPost");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task WebAPIConvention_TakesHttpMethodFromPrefix_NamedAction_DefaultVerbIsPost_VerbMismatch()
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod("PUT"),
                "http://localhost/api/Blog/WebApiActionConventionsDefaultPost/DefaultVerbIsPost");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task WebAPIConvention_TakesHttpMethodFromMethodName_NotActionName_UnnamedAction_Success()
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod("POST"),
                "http://localhost/api/Admin/WebAPIActionConventionsActionName");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("GetItems", result.ActionName);
        }

        [Fact]
        public async Task WebAPIConvention_TakesHttpMethodFromMethodName_NotActionName_NamedAction_Success()
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod("POST"),
                "http://localhost/api/Blog/WebAPIActionConventionsActionName/GetItems");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("GetItems", result.ActionName);
        }

        [Fact]
        public async Task WebAPIConvention_TakesHttpMethodFromMethodName_NotActionName_UnnamedAction_VerbMismatch()
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod("Get"),
                "http://localhost/api/Admin/WebAPIActionConventionsActionName");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task WebAPIConvention_TakesHttpMethodFromMethodName_NotActionName_NamedAction_VerbMismatch()
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod("GET"),
                "http://localhost/api/Blog/WebAPIActionConventionsActionName/GetItems");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task WebAPIConvention_HttpMethodOverride_UnnamedAction_Success()
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod("GET"),
                "http://localhost/api/Admin/WebAPIActionConventionsVerbOverride");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("PostItems", result.ActionName);
        }

        [Fact]
        public async Task WebAPIConvention_HttpMethodOverride_NamedAction_Success()
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod("GET"),
                "http://localhost/api/Blog/WebAPIActionConventionsVerbOverride/PostItems");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("PostItems", result.ActionName);
        }

        [Fact]
        public async Task WebAPIConvention_HttpMethodOverride_UnnamedAction_VerbMismatch()
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod("POST"),
                "http://localhost/api/Admin/WebAPIActionConventionsVerbOverride");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task WebAPIConvention_HttpMethodOverride_NamedAction_VerbMismatch()
        {
            // Arrange
            var request = new HttpRequestMessage(
                new HttpMethod("POST"),
                "http://localhost/api/Blog/WebAPIActionConventionsVerbOverride/PostItems");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // This was ported from the WebAPI 5.2 codebase. Kept the same intentionally for compatibility.
        [Theory]
        [InlineData("GET", "api/Admin/Test", "GetUsers")]
        [InlineData("GET", "api/Admin/Test/2", "GetUser")]
        [InlineData("GET", "api/Admin/Test/3?name=mario", "GetUserByNameAndId")]
        [InlineData("GET", "api/Admin/Test/3?name=mario&ssn=123456", "GetUserByNameIdAndSsn")]
        [InlineData("GET", "api/Admin/Test?name=mario&ssn=123456", "GetUserByNameAndSsn")]
        [InlineData("GET", "api/Admin/Test?name=mario&ssn=123456&age=3", "GetUserByNameAgeAndSsn")]
        [InlineData("GET", "api/Admin/Test/5?random=9", "GetUser")]
        // Note: Normally the following would not match DeleteUserByIdAndOptName because it has 'id' and 'age' as parameters while the DeleteUserByIdAndOptName action has 'id' and 'name'.
        // However, because the default value is provided on action parameter 'name', having the 'id' in the request was enough to match the action.
        [InlineData("DELETE", "api/Admin/Test/6?age=10", "DeleteUserByIdAndOptName")]
        [InlineData("DELETE", "api/Admin/Test", "DeleteUserByOptName")]
        [InlineData("DELETE", "api/Admin/Test?name=user", "DeleteUserByOptName")]
        [InlineData("DELETE", "api/Admin/Test/6?email=user@test.com", "DeleteUserById_Email_OptName_OptPhone")]
        [InlineData("DELETE", "api/Admin/Test/6?email=user@test.com&name=user", "DeleteUserById_Email_OptName_OptPhone")]
        [InlineData("DELETE", "api/Admin/Test/6?email=user@test.com&name=user&phone=123456789", "DeleteUserById_Email_OptName_OptPhone")]
        [InlineData("DELETE", "api/Admin/Test/6?email=user@test.com&height=1.8", "DeleteUserById_Email_Height_OptName_OptPhone")]
        [InlineData("DELETE", "api/Admin/Test/6?email=user@test.com&height=1.8&name=user", "DeleteUserById_Email_Height_OptName_OptPhone")]
        [InlineData("DELETE", "api/Admin/Test/6?email=user@test.com&height=1.8&name=user&phone=12345678", "DeleteUserById_Email_Height_OptName_OptPhone")]
        [InlineData("HEAD", "api/Admin/Test/6", "Head_Id_OptSize_OptIndex")]
        [InlineData("HEAD", "api/Admin/Test/6?size=2", "Head_Id_OptSize_OptIndex")]
        [InlineData("HEAD", "api/Admin/Test/6?index=2", "Head_Id_OptSize_OptIndex")]
        [InlineData("HEAD", "api/Admin/Test/6?index=2&size=10", "Head_Id_OptSize_OptIndex")]
        [InlineData("HEAD", "api/Admin/Test/6?index=2&otherParameter=10", "Head_Id_OptSize_OptIndex")]
        [InlineData("HEAD", "api/Admin/Test/6?otherQueryParameter=1234", "Head_Id_OptSize_OptIndex")]
        [InlineData("HEAD", "api/Admin/Test", "Head")]
        [InlineData("HEAD", "api/Admin/Test?otherParam=2", "Head")]
        [InlineData("HEAD", "api/Admin/Test?index=2&size=10", "Head")]
        public async Task LegacyActionSelection_OverloadedAction_WithUnnamedAction(string httpMethod, string requestUrl, string expectedActionName)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(httpMethod), "http://localhost/" + requestUrl);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedActionName, result.ActionName);
        }

        [Theory]
        [InlineData("api/Admin/Test", "PostUser")]
        [InlineData("api/Admin/Test?name=mario&age=10", "PostUserByNameAndAge")]
        public async Task LegacyActionSelection_OverloadedAction_WithUnnamedActionAndRequestContent(string requestUrl, string expectedActionName)
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/" + requestUrl);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedActionName, result.ActionName);
        }

        [Theory]
        [InlineData("GET", "api/Store/Test", "GetUsers")]
        [InlineData("GET", "api/Store/Test/2", "GetUsersByName")]
        [InlineData("GET", "api/Store/Test/luigi?ssn=123456", "GetUserByNameAndSsn")]
        [InlineData("GET", "api/Store/Test/luigi?ssn=123456&id=2&ssn=12345", "GetUserByNameIdAndSsn")]
        [InlineData("GET", "api/Store/Test?age=10&ssn=123456", "GetUsers")]
        [InlineData("GET", "api/Store/Test?id=3&ssn=123456&name=luigi", "GetUserByNameIdAndSsn")]
        [InlineData("POST", "api/Store/Test/luigi?age=20", "PostUserByNameAndAge")]
        public async Task LegacyActionSelection_OverloadedAction_NonIdRouteParameter(string httpMethod, string requestUrl, string expectedActionName)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(httpMethod), "http://localhost/" + requestUrl);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedActionName, result.ActionName);
        }

        [Theory]
        [InlineData("GET", "api/Admin/Test/3?NAME=mario", "GetUserByNameAndId")]
        [InlineData("GET", "api/Admin/Test/3?name=mario&SSN=123456", "GetUserByNameIdAndSsn")]
        [InlineData("GET", "api/Admin/Test?nAmE=mario&ssn=123456&AgE=3", "GetUserByNameAgeAndSsn")]
        [InlineData("DELETE", "api/Admin/Test/6?AGe=10", "DeleteUserByIdAndOptName")]
        public async Task LegacyActionSelection_OverloadedAction_Parameter_Casing(string httpMethod, string requestUrl, string expectedActionName)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(httpMethod), "http://localhost/" + requestUrl);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedActionName, result.ActionName);
        }

        [Theory]
        [InlineData("GET", "api/Blog/Test/GetUsers", "GetUsers")]
        [InlineData("GET", "api/Blog/Test/GetUser/7", "GetUser")]
        [InlineData("GET", "api/Blog/Test/GetUser?id=3", "GetUser")]
        [InlineData("GET", "api/Blog/Test/GetUser/4?id=3", "GetUser")]
        [InlineData("GET", "api/Blog/Test/GetUserByNameAgeAndSsn?name=user&age=90&ssn=123456789", "GetUserByNameAgeAndSsn")]
        [InlineData("GET", "api/Blog/Test/GetUserByNameAndSsn?name=user&ssn=123456789", "GetUserByNameAndSsn")]
        public async Task LegacyActionSelection_RouteWithActionName(string httpMethod, string requestUrl, string expectedActionName)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(httpMethod), "http://localhost/" + requestUrl);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedActionName, result.ActionName);
        }

        [Fact]
        public async Task LegacyActionSelection_RouteWithActionNameAndRequestContent()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/api/Blog/Test/PostUserByNameAndAddress?name=user");
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("PostUserByNameAndAddress", result.ActionName);
        }

        [Theory]
        [InlineData("GET", "api/Blog/Test/getusers", "GetUsers")]
        [InlineData("GET", "api/Blog/Test/getuseR/1", "GetUser")]
        [InlineData("GET", "api/Blog/Test/Getuser?iD=3", "GetUser")]
        [InlineData("GET", "api/Blog/Test/GetUser/4?Id=3", "GetUser")]
        [InlineData("GET", "api/Blog/Test/GetUserByNameAgeandSsn?name=user&age=90&ssn=123456789", "GetUserByNameAgeAndSsn")]
        [InlineData("GET", "api/Blog/Test/getUserByNameAndSsn?name=user&ssn=123456789", "GetUserByNameAndSsn")]
        public async Task LegacyActionSelection_RouteWithActionName_Casing(string httpMethod, string requestUrl, string expectedActionName)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(httpMethod), "http://localhost/" + requestUrl);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedActionName, result.ActionName);
        }

        [Fact]
        public async Task LegacyActionSelection_RouteWithActionNameAndRequestContent_Casing()
        {
            // Arrange
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                "http://localhost/api/Blog/Test/PostUserByNameAndAddress?name=user");
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("PostUserByNameAndAddress", result.ActionName);
        }

        [Theory]
        [InlineData("GET", "api/Admin/Test", "GetUsers")]
        [InlineData("GET", "api/Admin/Test/?name=peach", "GetUsersByName")]
        [InlineData("GET", "api/Admin/Test?name=peach", "GetUsersByName")]
        [InlineData("GET", "api/Admin/Test?name=peach&ssn=123456", "GetUserByNameAndSsn")]
        [InlineData("GET", "api/Admin/Test?name=peach&ssn=123456&age=3", "GetUserByNameAgeAndSsn")]
        public async Task LegacyActionSelection_RouteWithoutActionName(string httpMethod, string requestUrl, string expectedActionName)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(httpMethod), "http://localhost/" + requestUrl);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedActionName, result.ActionName);
        }

        [Theory]
        [InlineData("GET", "api/Admin/ParameterAttribute/2", "GetUser")]
        [InlineData("GET", "api/Admin/ParameterAttribute?id=2", "GetUser")]
        [InlineData("GET", "api/Admin/ParameterAttribute?myId=2", "GetUserByMyId")]
        [InlineData("DELETE", "api/Admin/ParameterAttribute/3?name=user", "DeleteUserWithNullableIdAndName")]
        [InlineData("DELETE", "api/Admin/ParameterAttribute?address=userStreet", "DeleteUser")]
        public async Task LegacyActionSelection_ModelBindingParameterAttribute_AreAppliedWhenSelectingActions(string httpMethod, string requestUrl, string expectedActionName)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(httpMethod), "http://localhost/" + requestUrl);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedActionName, result.ActionName);
        }

        [Theory]
        [InlineData("api/Admin/ParameterAttribute/3?name=user", "PostUserNameFromUri")]
        [InlineData("api/Admin/ParameterAttribute/3", "PostUserNameFromBody")]
        public async Task LegacyActionSelection_Post_ModelBindingParameterAttribute_AreAppliedWhenSelectingActions(string requestUrl, string expectedActionName)
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/" + requestUrl);
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedActionName, result.ActionName);
        }

        [Theory]
        [InlineData("GET", "api/Support/notActionParameterValue1/Test", "GetUsers")]
        [InlineData("GET", "api/Support/notActionParameterValue2/Test/2", "GetUser")]
        [InlineData("GET", "api/Support/notActionParameterValue1/Test?randomQueryVariable=val1", "GetUsers")]
        [InlineData("GET", "api/Support/notActionParameterValue2/Test/2?randomQueryVariable=val2", "GetUser")]
        public async Task LegacyActionSelection_ActionsThatHaveSubsetOfRouteParameters_AreConsideredForSelection(string httpMethod, string requestUrl, string expectedActionName)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(httpMethod), "http://localhost/" + requestUrl);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedActionName, result.ActionName);
        }

        [Theory]
        [InlineData("GET", "api/Admin/EnumParameterOverloads", "Get")]
        [InlineData("GET", "api/Admin/EnumParameterOverloads?scope=global", "GetWithEnumParameter")]
        [InlineData("GET", "api/Admin/EnumParameterOverloads?level=off&kind=trace", "GetWithTwoEnumParameters")]
        [InlineData("GET", "api/Admin/EnumParameterOverloads?level=", "GetWithNullableEnumParameter")]
        public async Task LegacyActionSelection_SelectAction_ReturnsActionDescriptor_ForEnumParameterOverloads(string httpMethod, string requestUrl, string expectedActionName)
        {
            // Arrange
            var request = new HttpRequestMessage(new HttpMethod(httpMethod), "http://localhost/" + requestUrl);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var data = Assert.Single(response.Headers.GetValues("ActionSelection"));
            var result = JsonConvert.DeserializeObject<ActionSelectionResult>(data);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedActionName, result.ActionName);
        }

        // Verify response has all the methods in its Allow header. values are unsorted.
        private void AssertAllowedHeaders(HttpResponseMessage response, params HttpMethod[] allowedMethods)
        {
            foreach (var method in allowedMethods)
            {
                Assert.Contains(method.ToString(), response.Content.Headers.Allow);
            }
            Assert.Equal(allowedMethods.Length, response.Content.Headers.Allow.Count);
        }

        private class ActionSelectionResult
        {
            public string ActionName { get; set; }

            public string ControllerName { get; set; }
        }
    }
}