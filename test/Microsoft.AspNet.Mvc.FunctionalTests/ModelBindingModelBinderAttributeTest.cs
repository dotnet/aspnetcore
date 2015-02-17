// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using ModelBindingWebSite.Models;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ModelBindingModelBinderAttributeTest
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices(nameof(ModelBindingWebSite));
        private readonly Action<IApplicationBuilder> _app = new ModelBindingWebSite.Startup().Configure;

        [Fact]
        public async Task ModelBinderAttribute_CustomModelPrefix()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // [ModelBinder(Name = "customPrefix")] is used to apply a prefix
            var url = 
                "http://localhost/ModelBinderAttribute_Company/GetCompany?customPrefix.Employees[0].Name=somename";

            // Act
            var response = await client.GetAsync(url);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var company = JsonConvert.DeserializeObject<Company>(body);

            var employee = Assert.Single(company.Employees);
            Assert.Equal("somename", employee.Name);
        }

        [Fact]
        public async Task ModelBinderAttribute_CustomModelPrefix_OnProperty()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            var url =
                "http://localhost/ModelBinderAttribute_Company/CreateCompany?employees[0].Alias=somealias";

            // Act
            var response = await client.GetAsync(url);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var company = JsonConvert.DeserializeObject<Company>(body);

            var employee = Assert.Single(company.Employees);
            Assert.Equal("somealias", employee.EmailAlias);
        }

        [Theory]
        [InlineData("GetBinderType_UseModelBinderOnType")]
        [InlineData("GetBinderType_UseModelBinderProviderOnType")]
        public async Task ModelBinderAttribute_WithPrefixOnParameter(string action)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // [ModelBinder(Name = "customPrefix")] is used to apply a prefix
            var url =
                "http://localhost/ModelBinderAttribute_Product/" +
                action +
                "?customPrefix.ProductId=5";

            // Act
            var response = await client.GetAsync(url);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(
                "ModelBindingWebSite.Controllers.ModelBinderAttribute_ProductController+ProductModelBinder",
                body);
        }

        [Theory]
        [InlineData("GetBinderType_UseModelBinder")]
        [InlineData("GetBinderType_UseModelBinderProvider")]
        public async Task ModelBinderAttribute_WithBinderOnParameter(string action)
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            var url =
                "http://localhost/ModelBinderAttribute_Product/" +
                action +
                "?model.productId=5";

            // Act
            var response = await client.GetAsync(url);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(
                "ModelBindingWebSite.Controllers.ModelBinderAttribute_ProductController+ProductModelBinder", 
                body);
        }

        [Fact]
        public async Task ModelBinderAttribute_WithBinderOnEnum()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            var url =
                "http://localhost/ModelBinderAttribute_Product/" +
                "ModelBinderAttribute_UseModelBinderOnEnum" +
                "?status=Shipped";

            // Act
            var response = await client.GetAsync(url);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("StatusShipped", body);
        }

        private class Product
        {
            public int ProductId { get; set; }

            public string BinderType { get; set; }
        }
    }
}