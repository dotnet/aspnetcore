// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using ModelBindingWebSite;
using ModelBindingWebSite.Controllers;
using ModelBindingWebSite.Models;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ModelBindingFromQueryTest
    {
        private const string SiteName = nameof(ModelBindingWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task FromQuery_CustomModelPrefix_ForParameter()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // [FromQuery(Name = "customPrefix")] is used to apply a prefix
            var url =
                "http://localhost/FromQueryAttribute_Company/CreateCompany?customPrefix.Employees[0].Name=somename";

            // Act
            var response = await client.GetAsync(url);

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
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var url =
                "http://localhost/FromQueryAttribute_Company/CreateCompanyFromEmployees?customPrefix[0].Name=somename";

            // Act
            var response = await client.GetAsync(url);

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
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // [FromQuery(Name = "EmployeeId")] is used to apply a prefix
            var url =
                "http://localhost/FromQueryAttribute_Company/CreateCompany?customPrefix.Employees[0].EmployeeId=1234";

            // Act
            var response = await client.GetAsync(url);

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
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var url =
                "http://localhost/FromQueryAttribute_Company/CreateDepartment?TestEmployees[0].EmployeeId=1234";


            // Act
            var response = await client.GetAsync(url);

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
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var url =
                "http://localhost/FromQueryAttribute_Company/ValidateDepartment?TestEmployees[0].Department=contoso";


            // Act
            var response = await client.GetAsync(url);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Result>(body);
            var error = Assert.Single(result.ModelStateErrors);
            Assert.Equal("TestEmployees[0].EmployeeId", error);
        }
    }
}