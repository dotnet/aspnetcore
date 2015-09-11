// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using ModelBindingWebSite;
using ModelBindingWebSite.Controllers;
using ModelBindingWebSite.Models;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ModelBindingFromQueryTest : IClassFixture<MvcTestFixture<ModelBindingWebSite.Startup>>
    {
        public ModelBindingFromQueryTest(MvcTestFixture<ModelBindingWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task FromQuery_CustomModelPrefix_ForParameter()
        {
            // Arrange
            // [FromQuery(Name = "customPrefix")] is used to apply a prefix
            var url =
                "http://localhost/FromQueryAttribute_Company/CreateCompany?customPrefix.Employees[0].Name=somename";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var company = JsonConvert.DeserializeObject<Company>(body);

            var employee = Assert.Single(company.Employees);
            Assert.Equal("somename", employee.Name);
        }

        [Fact]
        public async Task FromQuery_CustomModelPrefix_ForCollectionParameter()
        {
            // Arrange
            var url =
                "http://localhost/FromQueryAttribute_Company/CreateCompanyFromEmployees?customPrefix[0].Name=somename";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var company = JsonConvert.DeserializeObject<Company>(body);

            var employee = Assert.Single(company.Employees);
            Assert.Equal("somename", employee.Name);
        }

        [Fact]
        public async Task FromQuery_CustomModelPrefix_ForProperty()
        {
            // Arrange
            // [FromQuery(Name = "EmployeeId")] is used to apply a prefix
            var url =
                "http://localhost/FromQueryAttribute_Company/CreateCompany?customPrefix.Employees[0].EmployeeId=1234";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var company = JsonConvert.DeserializeObject<Company>(body);

            var employee = Assert.Single(company.Employees);

            Assert.Equal(1234, employee.Id);
        }

        [Fact]
        public async Task FromQuery_CustomModelPrefix_ForCollectionProperty()
        {
            // Arrange
            var url = "http://localhost/FromQueryAttribute_Company/CreateDepartment?TestEmployees[0].EmployeeId=1234";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var department = JsonConvert.DeserializeObject<
                FromQueryAttribute_CompanyController.FromQuery_Department>(body);

            var employee = Assert.Single(department.Employees);
            Assert.Equal(1234, employee.Id);
        }

        [Fact]
        public async Task FromQuery_NonExistingValueAddsValidationErrors_OnProperty_UsingCustomModelPrefix()
        {
            // Arrange
            var url =
                "http://localhost/FromQueryAttribute_Company/ValidateDepartment?TestEmployees[0].Department=contoso";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Result>(body);
            var error = Assert.Single(result.ModelStateErrors);
            Assert.Equal("TestEmployees[0].EmployeeId", error);
        }
    }
}