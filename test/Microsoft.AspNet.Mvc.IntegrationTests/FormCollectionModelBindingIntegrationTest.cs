// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Collections;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class FormCollectionModelBindingIntegrationTest
    {
        private class Person
        {
            public Address Address { get; set; }
        }

        private class Address
        {
            public int Zip { get; set; }

            public FormCollection FileCollection { get; set; }
        }

        [Fact(Skip = "ModelState.Value not set due to #2445, Extra entries in model state #2446.")]
        public async Task BindProperty_WithData_WithEmptyPrefix_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Person)
            };

            var data = "Some Data Is Better Than No Data.";
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request =>
                {
                    request.QueryString = new QueryString("Address.Zip", "12345");
                    UpdateRequest(request, data, "Address.File");
                });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var boundPerson = Assert.IsType<Person>(modelBindingResult.Model);
            Assert.NotNull(boundPerson.Address);
            var formCollection = Assert.IsAssignableFrom<FormCollection>(boundPerson.Address.FileCollection);
            var file = Assert.Single(formCollection.Files);
            Assert.Equal("form-data; name=Address.File; filename=text.txt", file.ContentDisposition);
            var reader = new StreamReader(file.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Equal(2, modelState.Count);
            Assert.Single(modelState.Keys, k => k == "Address.Zip");
            var key = Assert.Single(modelState.Keys, k => k == "Address.File"); // Should be only one key. bug #2446
            Assert.NotNull(modelState[key].Value); // should be non null bug #2445.
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Fact(Skip = "Extra entries in model state #2446.")]
        public async Task BindParameter_WithData_GetsBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    // Setting a custom parameter prevents it from falling back to an empty prefix.
                    BinderModelName = "CustomParameter",
                },

                ParameterType = typeof(FormCollection)
            };

            var data = "Some Data Is Better Than No Data.";
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request =>
                {
                    UpdateRequest(request, data, "CustomParameter");
                });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert
            // ModelBindingResult
            Assert.NotNull(modelBindingResult);
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var formCollection = Assert.IsType<FormCollection>(modelBindingResult.Model);
            Assert.NotNull(formCollection);
            var file = Assert.Single(formCollection.Files);
            Assert.NotNull(file);
            Assert.Equal("form-data; name=CustomParameter; filename=text.txt", file.ContentDisposition);
            var reader = new StreamReader(file.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());

            // ModelState
            Assert.True(modelState.IsValid);

            // Validation should be skipped because we do not validate any parameters and since IFormFile is not
            // IValidatableObject, we should have no entries in the model state dictionary.
            Assert.Empty(modelState.Keys); // Enable when we fix #2446.
        }

        [Fact(Skip = "Extra entries in model state #2446.")]
        public async Task BindParameter_NoData_DoesNotGetBound()
        {
            // Arrange
            var argumentBinder = ModelBindingTestHelper.GetArgumentBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo()
                {
                    BinderModelName = "CustomParameter",
                },

                ParameterType = typeof(FormCollection)
            };

            // No data is passed.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(request =>
            {
                request.ContentType = "multipart/form-data";
            });

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult
            Assert.NotNull(modelBindingResult); // Fails due to bug #2456
            Assert.Null(modelBindingResult.Model);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState.Keys);
        }

        private void UpdateRequest(HttpRequest request, string data, string name)
        {
            var fileCollection = new FormFileCollection();
            var formCollection = new FormCollection(new Dictionary<string, string[]>(), fileCollection);
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data));

            request.Form = formCollection;
            request.ContentType = "multipart/form-data";
            request.Headers["Content-Disposition"] = "form-data; name=" + name + "; filename=text.txt";
            fileCollection.Add(new FormFile(memoryStream, 0, data.Length)
            {
                Headers = request.Headers
            });
        }
    }
}