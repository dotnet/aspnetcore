// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class ApplicationModelTest : IClassFixture<MvcTestFixture<ApplicationModelWebSite.Startup>>
    {
        public ApplicationModelTest(MvcTestFixture<ApplicationModelWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task ControllerModel_CustomizedWithAttribute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/CoolController/GetControllerName");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("CoolController", body);
        }

        [Fact]
        public async Task ActionModel_CustomizedWithAttribute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ActionModel/ActionName");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("ActionName", body);
        }

        [Fact]
        public async Task ParameterModel_CustomizedWithAttribute()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ParameterModel/GetParameterMetadata");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("CoolMetadata", body);
        }

        [Fact]
        public async Task ApplicationModel_AddPropertyToActionDescriptor_FromApplicationModel()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Home/GetCommonDescription");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Common Application Description", body);
        }

        [Fact]
        public async Task ApplicationModel_AddPropertyToActionDescriptor_ControllerModelOverwritesCommonApplicationProperty()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApplicationModel/GetControllerDescription");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Common Controller Description", body);
        }

        [Fact]
        public async Task ApplicationModel_ProvidesMetadataToActionDescriptor_ActionModelOverwritesControllerModelProperty()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/ApplicationModel/GetActionSpecificDescription");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Specific Action Description", body);
       }

        [Fact]
        public async Task ApplicationModelExtensions_AddsConventionToAllControllers()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/License/GetLicense");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("Copyright (c) .NET Foundation. All rights reserved." +
                " Licensed under the Apache License, Version 2.0. See License.txt " +
                "in the project root for license information.", body);
        }

        [Fact]
        public async Task ApplicationModelExtensions_AddsConventionToAllActions()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Home/GetHelloWorld");
            request.Headers.Add("helloWorld", "HelloWorld");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("From Header - HelloWorld", body);
        }

        [Fact]
        public async Task ActionModelSuppressedForPathMatching_CannotBeRouted()
        {
            // Arrange & Act
            var response = await Client.GetAsync("Home/CannotBeRouted");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ActionModelNotSuppressedForPathMatching_CanBeRouted()
        {
            // Arrange & Act
            var response = await Client.GetStringAsync("Home/CanBeRouted");

            // Assert
            Assert.Equal("Hello world", response);
        }

        [Fact]
        public async Task ActionModelSuppressedForLinkGeneration_CannotBeLinked()
        {
            // Act & Assert
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => Client.GetStringAsync("Home/RouteToSuppressLinkGeneration"));
            Assert.Equal("No route matches the supplied values.", ex.Message);
        }

        [Fact]
        public async Task ActionModelSuppressedForPathMatching_CanBeLinked()
        {
            // Arrange & Act
            var response = await Client.GetAsync("Home/RouteToSuppressPathMatching");

            // Assert
            Assert.Equal("/Home/CannotBeRouted", response.Headers.Location.ToString());
        }

        [Theory]
        [InlineData("Products", "Products View")]
        [InlineData("Services", "Services View")]
        [InlineData("Manage", "Manage View")]
        public async Task ApplicationModel_CanDuplicateController_InMultipleAreas(string areaName, string expectedContent)
        {
            // Arrange & Act
            var response = await Client.GetAsync(areaName + "/MultipleAreas/Index");
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(expectedContent, content);
        }

        [Theory]
        [InlineData("Help", "This is the help page")]        
        [InlineData("MoreHelp", "This is the more help page")]        
        public async Task ControllerModel_CanDuplicateActions_RoutesToDifferentNames(string actionName, string expectedContent)
        {
            // Arrange & Act
            var response = await Client.GetAsync("ActionModel/" + actionName);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            Assert.Contains(expectedContent, body);
        }
    }
}