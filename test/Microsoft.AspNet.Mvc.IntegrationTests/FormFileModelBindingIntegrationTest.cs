// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features.Internal;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Mvc.ModelBinding;
using Xunit;

namespace Microsoft.AspNet.Mvc.IntegrationTests
{
    public class FormFileModelBindingIntegrationTest
    {
        private class Person
        {
            public Address Address { get; set; }
        }

        private class Address
        {
            public int Zip { get; set; }

            public IFormFile File { get; set; }
        }

        [Fact(Skip = "ModelState.Value not set due to #2445")]
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
                    request.QueryString = QueryString.Create("Address.Zip", "12345");
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
            var file = Assert.IsAssignableFrom<IFormFile>(boundPerson.Address.File);
            Assert.Equal("form-data; name=Address.File; filename=text.txt", file.ContentDisposition);
            var reader = new StreamReader(boundPerson.Address.File.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Equal(2, modelState.Count);
            Assert.Single(modelState.Keys, k => k == "Address.Zip");
            var key = Assert.Single(modelState.Keys, k => k == "Address.File");
            Assert.NotNull(modelState[key].Value); // should be non null bug #2445.
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        [Fact]
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

                ParameterType = typeof(IFormFile)
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
            var file = Assert.IsType<FormFile>(modelBindingResult.Model);
            Assert.NotNull(file);
            Assert.Equal("form-data; name=CustomParameter; filename=text.txt", file.ContentDisposition);
            var reader = new StreamReader(file.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());

            // ModelState
            Assert.True(modelState.IsValid);

            // Validation should be skipped because we do not validate any parameters and since IFormFile is not
            // IValidatableObject, we should have no entries in the model state dictionary.
            Assert.Empty(modelState.Keys);
        }

        [Fact]
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

                ParameterType = typeof(IFormFile)
            };

            // No data is passed.
            var operationContext = ModelBindingTestHelper.GetOperationBindingContext(
                request => UpdateRequest(request, data: null, name: null));

            var modelState = new ModelStateDictionary();

            // Act
            var modelBindingResult = await argumentBinder.BindModelAsync(parameter, modelState, operationContext);

            // Assert

            // ModelBindingResult; expect null because binding failed.
            Assert.Null(modelBindingResult);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState.Keys);
        }

        private void UpdateRequest(HttpRequest request, string data, string name)
        {
            var fileCollection = new FormFileCollection();
            var formCollection = new FormCollection(new Dictionary<string, string[]>(), fileCollection);

            request.Form = formCollection;
            request.ContentType = "multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq";

            if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(name))
            {
                // Leave the submission empty.
                return;
            }

            request.Headers["Content-Disposition"] = "form-data; name=" + name + "; filename=text.txt";

            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            fileCollection.Add(new FormFile(memoryStream, 0, data.Length)
            {
                Headers = request.Headers
            });
        }
    }
}