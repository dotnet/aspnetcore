// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{
    public class JQueryFormatModelBindingIntegrationTest
    {
        [Fact]
        public async Task BindsJQueryFormatData_FromQuery()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Customer)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = new QueryString(
                        "?Name=James&Address[0][City]=Redmond&Address[0][State][ShortName]=WA&Address[0][State][LongName]=Washington");
                },
                options =>
                {
                    options.ValueProviderFactories.Add(new JQueryQueryStringValueProviderFactory());
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Customer>(modelBindingResult.Model);
            Assert.Equal("James", model.Name);
            Assert.NotNull(model.Address);
            var address = Assert.Single(model.Address);
            Assert.Equal("Redmond", address.City);
            Assert.NotNull(address.State);
            Assert.Equal("WA", address.State.ShortName);
            Assert.Equal("Washington", address.State.LongName);
            Assert.True(modelState.IsValid);
        }

        [Fact]
        public async Task BindsJQueryFormatData_FromRequestBody()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "parameter",
                ParameterType = typeof(Customer)
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.Body = new MemoryStream(Encoding.UTF8.GetBytes(
                        "Name=James&Address[0][City]=Redmond&Address[0][State][ShortName]=WA&Address[0][State][LongName]=Washington"));
                    request.ContentType = "application/x-www-form-urlencoded;charset=utf-8";
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(modelBindingResult.IsModelSet);

            var model = Assert.IsType<Customer>(modelBindingResult.Model);
            Assert.Equal("James", model.Name);
            Assert.NotNull(model.Address);
            var address = Assert.Single(model.Address);
            Assert.Equal("Redmond", address.City);
            Assert.NotNull(address.State);
            Assert.Equal("WA", address.State.ShortName);
            Assert.Equal("Washington", address.State.LongName);
            Assert.True(modelState.IsValid);
        }

        private class Customer
        {
            public string Name { get; set; }
            public List<Address> Address { get; set; }
        }

        private class Address
        {
            public string City { get; set; }
            public State State { get; set; }
        }

        private class State
        {
            public string ShortName { get; set; }
            public string LongName { get; set; }
        }
    }
}