// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using ModelBindingWebSite;
using ModelBindingWebSite.Controllers;
using ModelBindingWebSite.Models;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ModelBindingFromFormTest
    {
        private const string SiteName = nameof(ModelBindingWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task FromForm_CustomModelPrefix_ForParameter()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var url = "http://localhost/FromFormAttribute_Company/CreateCompany";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("customPrefix.Employees[0].Name", "somename"),
            };

            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var company = JsonConvert.DeserializeObject<Company>(body);

            var employee = Assert.Single(company.Employees);

            Assert.Equal("somename", employee.Name);
        }

        [Fact]
        public async Task FromForm_CustomModelPrefix_ForCollectionParameter()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var url = "http://localhost/FromFormAttribute_Company/CreateCompanyFromEmployees";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("customPrefix[0].Department", "Contoso"),
            };
            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var company = JsonConvert.DeserializeObject<Company>(body);

            var employee = Assert.Single(company.Employees);
            Assert.Equal("Contoso", employee.Department);
        }

        [Fact]
        public async Task FromForm_CustomModelPrefix_ForProperty()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var url = "http://localhost/FromFormAttribute_Company/CreateCompany";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("customPrefix.Employees[0].EmployeeSSN", "123132131"),
            };
            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var company = JsonConvert.DeserializeObject<Company>(body);

            var employee = Assert.Single(company.Employees);
            Assert.Equal("123132131", employee.SSN);
        }

        [Fact]
        public async Task FromForm_CustomModelPrefix_ForCollectionProperty()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var url = "http://localhost/FromFormAttribute_Company/CreateDepartment";
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var nameValueCollection = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("department.TestEmployees[0].EmployeeSSN", "123132131"),
            };
            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var department = JsonConvert.DeserializeObject<
                FromFormAttribute_CompanyController.FromForm_Department>(body);

            var employee = Assert.Single(department.Employees);
            Assert.Equal("123132131", employee.SSN);
        }

        [Fact]
        public async Task FromForm_NonExistingValueAddsValidationErrors_OnProperty_UsingCustomModelPrefix()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var url = "http://localhost/FromFormAttribute_Company/ValidateDepartment";
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            // No values.
            var nameValueCollection = new List<KeyValuePair<string, string>>();
            request.Content = new FormUrlEncodedContent(nameValueCollection);

            // Act
            var response = await client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Result>(body);
            Assert.Null(result.Value);
            var error = Assert.Single(result.ModelStateErrors);
            Assert.Equal("TestEmployees", error);
        }
    }
}