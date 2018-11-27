// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
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

        [Fact]
        public async Task BindProperty_WithData_WithEmptyPrefix_GetsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor()
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Person)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = QueryString.Create("Address.Zip", "12345");
                    UpdateRequest(request, data, "Address.File");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
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
            Assert.Null(modelState[key].RawValue);
            Assert.Empty(modelState[key].Errors);
            Assert.Equal(ModelValidationState.Valid, modelState[key].ValidationState);
        }

        private class ListContainer1
        {
            [ModelBinder(Name = "files")]
            public List<IFormFile> ListProperty { get; set; }
        }

        [Fact]
        public async Task BindCollectionProperty_WithData_IsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(ListContainer1),
            };

            var data = "some data";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request => UpdateRequest(request, data, "files"));
            var modelState = testContext.ModelState;

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);

            // Model
            var boundContainer = Assert.IsType<ListContainer1>(result.Model);
            Assert.NotNull(boundContainer);
            Assert.NotNull(boundContainer.ListProperty);
            var file = Assert.Single(boundContainer.ListProperty);
            Assert.Equal("form-data; name=files; filename=text.txt", file.ContentDisposition);
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                Assert.Equal(data, reader.ReadToEnd());
            }

            // ModelState
            Assert.True(modelState.IsValid);
            var kvp = Assert.Single(modelState);
            Assert.Equal("files", kvp.Key);
            var modelStateEntry = kvp.Value;
            Assert.NotNull(modelStateEntry);
            Assert.Empty(modelStateEntry.Errors);
            Assert.Equal(ModelValidationState.Valid, modelStateEntry.ValidationState);
            Assert.Null(modelStateEntry.AttemptedValue);
            Assert.Null(modelStateEntry.RawValue);
        }

        [Fact]
        public async Task BindCollectionProperty_NoData_IsNotBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(ListContainer1),
            };

            var testContext = ModelBindingTestHelper.GetTestContext(
                request => UpdateRequest(request, data: null, name: null));
            var modelState = testContext.ModelState;

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);

            // Model (bound to an empty collection)
            var boundContainer = Assert.IsType<ListContainer1>(result.Model);
            Assert.NotNull(boundContainer);
            Assert.Null(boundContainer.ListProperty);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState);
        }

        private class ListContainer2
        {
            [ModelBinder(Name = "files")]
            public List<IFormFile> ListProperty { get; } = new List<IFormFile>
            {
                new FormFile(new MemoryStream(), baseStreamOffset: 0, length: 0, name: "file", fileName: "file1"),
                new FormFile(new MemoryStream(), baseStreamOffset: 0, length: 0, name: "file", fileName: "file2"),
                new FormFile(new MemoryStream(), baseStreamOffset: 0, length: 0, name: "file", fileName: "file3"),
            };
        }

        [Fact]
        public async Task BindReadOnlyCollectionProperty_WithData_IsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(ListContainer2),
            };

            var data = "some data";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request => UpdateRequest(request, data, "files"));
            var modelState = testContext.ModelState;

            // Act
            var result = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.True(result.IsModelSet);

            // Model
            var boundContainer = Assert.IsType<ListContainer2>(result.Model);
            Assert.NotNull(boundContainer);
            Assert.NotNull(boundContainer.ListProperty);
            var file = Assert.Single(boundContainer.ListProperty);
            Assert.Equal("form-data; name=files; filename=text.txt", file.ContentDisposition);
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                Assert.Equal(data, reader.ReadToEnd());
            }

            // ModelState
            Assert.True(modelState.IsValid);
            var kvp = Assert.Single(modelState);
            Assert.Equal("files", kvp.Key);
            var modelStateEntry = kvp.Value;
            Assert.NotNull(modelStateEntry);
            Assert.Empty(modelStateEntry.Errors);
            Assert.Equal(ModelValidationState.Valid, modelStateEntry.ValidationState);
            Assert.Null(modelStateEntry.AttemptedValue);
            Assert.Null(modelStateEntry.RawValue);
        }

        [Fact]
        public async Task BindParameter_WithData_GetsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "Parameter1",
                BindingInfo = new BindingInfo
                {
                    // Setting a custom parameter prevents it from falling back to an empty prefix.
                    BinderModelName = "CustomParameter",
                },
                ParameterType = typeof(IFormFile)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    UpdateRequest(request, data, "CustomParameter");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var file = Assert.IsType<FormFile>(modelBindingResult.Model);
            Assert.NotNull(file);
            Assert.Equal("form-data; name=CustomParameter; filename=text.txt", file.ContentDisposition);
            var reader = new StreamReader(file.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());

            // ModelState
            Assert.True(modelState.IsValid);
            var entry = Assert.Single(modelState);
            Assert.Equal("CustomParameter", entry.Key);
            Assert.Empty(entry.Value.Errors);
            Assert.Equal(ModelValidationState.Valid, entry.Value.ValidationState);
            Assert.Null(entry.Value.AttemptedValue);
            Assert.Null(entry.Value.RawValue);
        }

        [Fact]
        public async Task BindParameter_NoData_DoesNotGetBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
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
            var testContext = ModelBindingTestHelper.GetTestContext(
                request => UpdateRequest(request, data: null, name: null));

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert
            Assert.False(modelBindingResult.IsModelSet);

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Empty(modelState.Keys);
        }

        private class Car1
        {
            public string Name { get; set; }

            public FormFileCollection Specs { get; set; }
        }

        [Fact]
        public async Task BindProperty_WithData_WithPrefix_GetsBound()
        {
            // Arrange
            var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
            var parameter = new ParameterDescriptor
            {
                Name = "p",
                BindingInfo = new BindingInfo(),
                ParameterType = typeof(Car1)
            };

            var data = "Some Data Is Better Than No Data.";
            var testContext = ModelBindingTestHelper.GetTestContext(
                request =>
                {
                    request.QueryString = QueryString.Create("p.Name", "Accord");
                    UpdateRequest(request, data, "p.Specs");
                });

            var modelState = testContext.ModelState;

            // Act
            var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

            // Assert

            // ModelBindingResult
            Assert.True(modelBindingResult.IsModelSet);

            // Model
            var car = Assert.IsType<Car1>(modelBindingResult.Model);
            Assert.NotNull(car.Specs);
            var file = Assert.Single(car.Specs);
            Assert.Equal("form-data; name=p.Specs; filename=text.txt", file.ContentDisposition);
            var reader = new StreamReader(file.OpenReadStream());
            Assert.Equal(data, reader.ReadToEnd());

            // ModelState
            Assert.True(modelState.IsValid);
            Assert.Equal(2, modelState.Count);

            var entry = Assert.Single(modelState, e => e.Key == "p.Name").Value;
            Assert.Equal("Accord", entry.AttemptedValue);
            Assert.Equal("Accord", entry.RawValue);

            Assert.Single(modelState, e => e.Key == "p.Specs");
        }

        private void UpdateRequest(HttpRequest request, string data, string name)
        {
            const string fileName = "text.txt";
            var fileCollection = new FormFileCollection();
            var formCollection = new FormCollection(new Dictionary<string, StringValues>(), fileCollection);

            request.Form = formCollection;
            request.ContentType = "multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq";

            if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(name))
            {
                // Leave the submission empty.
                return;
            }

            request.Headers["Content-Disposition"] = $"form-data; name={name}; filename={fileName}";

            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            fileCollection.Add(new FormFile(memoryStream, 0, data.Length, name, fileName)
            {
                Headers = request.Headers
            });
        }
    }
}