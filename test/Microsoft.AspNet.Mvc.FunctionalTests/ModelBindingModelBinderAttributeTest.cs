// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using ModelBindingWebSite.Models;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ModelBindingModelBinderAttributeTest : IClassFixture<MvcTestFixture<ModelBindingWebSite.Startup>>
    {
        public ModelBindingModelBinderAttributeTest(MvcTestFixture<ModelBindingWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task ModelBinderAttribute_CustomModelPrefix()
        {
            // Arrange
            // [ModelBinder(Name = "customPrefix")] is used to apply a prefix
            var url =
                "http://localhost/ModelBinderAttribute_Company/GetCompany?customPrefix.Employees[0].Name=somename";

            // Act
            var response = await Client.GetAsync(url);

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
            var url = "http://localhost/ModelBinderAttribute_Company/CreateCompany?employees[0].Alias=somealias";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var company = JsonConvert.DeserializeObject<Company>(body);

            var employee = Assert.Single(company.Employees);
            Assert.Equal("somealias", employee.EmailAlias);
        }

        [Fact]
        public async Task ModelBinderAttribute_WithPrefixOnParameter()
        {
            // Arrange
            // [ModelBinder(Name = "customPrefix")] is used to apply a prefix
            var url = "http://localhost/ModelBinderAttribute_Product/GetBinderType_UseModelBinderOnType" +
                "?customPrefix.ProductId=5";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(
                "ModelBindingWebSite.Controllers.ModelBinderAttribute_ProductController+ProductModelBinder",
                body);
        }

        [Fact]
        public async Task ModelBinderAttribute_WithBinderOnParameter()
        {
            // Arrange
            var url = "http://localhost/ModelBinderAttribute_Product/GetBinderType_UseModelBinder/" +
                "?model.productId=5";

            // Act
            var response = await Client.GetAsync(url);

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
            var url = "http://localhost/ModelBinderAttribute_Product/ModelBinderAttribute_UseModelBinderOnEnum" +
                "?status=Shipped";

            // Act
            var response = await Client.GetAsync(url);

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