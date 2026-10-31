// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class FormCollectionModelBindingIntegrationTest
{
    private class Person
    {
        public Address Address { get; set; }
    }

    private class Address
    {
        public int Zip { get; set; }

        public IFormCollection FileCollection { get; set; }
    }

    [Fact]
    public async Task BindProperty_WithData_WithEmptyPrefix_GetsBound()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor
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
        var formCollection = Assert.IsAssignableFrom<IFormCollection>(boundPerson.Address.FileCollection);
        var file = Assert.Single(formCollection.Files);
        Assert.Equal("form-data; name=Address.File; filename=text.txt", file.ContentDisposition);
        var reader = new StreamReader(file.OpenReadStream());
        Assert.Equal(data, reader.ReadToEnd());

        // ModelState
        Assert.True(modelState.IsValid);
        var entry = Assert.Single(modelState);
        Assert.Equal("Address.Zip", entry.Key);
        Assert.Empty(entry.Value.Errors);
        Assert.Equal(ModelValidationState.Valid, entry.Value.ValidationState);
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
            ParameterType = typeof(IFormCollection)
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
        var formCollection = Assert.IsAssignableFrom<IFormCollection>(modelBindingResult.Model);
        var file = Assert.Single(formCollection.Files);
        Assert.NotNull(file);
        Assert.Equal("form-data; name=CustomParameter; filename=text.txt", file.ContentDisposition);
        var reader = new StreamReader(file.OpenReadStream());
        Assert.Equal(data, reader.ReadToEnd());

        // ModelState
        Assert.True(modelState.IsValid);
        Assert.Empty(modelState);
    }

    [Fact]
    public async Task BindParameter_NoData_BindsWithEmptyCollection()
    {
        // Arrange
        var parameterBinder = ModelBindingTestHelper.GetParameterBinder();
        var parameter = new ParameterDescriptor
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo
            {
                BinderModelName = "CustomParameter",
            },
            ParameterType = typeof(IFormCollection)
        };

        // No data is passed.
        var testContext = ModelBindingTestHelper.GetTestContext();

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert

        // ModelBindingResult
        var collection = Assert.IsAssignableFrom<IFormCollection>(modelBindingResult.Model);

        // ModelState
        Assert.True(modelState.IsValid);
        Assert.Empty(modelState);

        // FormCollection
        Assert.Empty(collection);
        Assert.Empty(collection.Files);
        Assert.Empty(collection.Keys);
    }

    private void UpdateRequest(HttpRequest request, string data, string name)
    {
        const string fileName = "text.txt";
        var fileCollection = new FormFileCollection();
        var formCollection = new FormCollection(new Dictionary<string, StringValues>(), fileCollection);
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(data));

        request.Form = formCollection;
        request.ContentType = "multipart/form-data; boundary=----WebKitFormBoundarymx2fSWqWSd0OxQqq";
        request.Headers["Content-Disposition"] = $"form-data; name={name}; filename={fileName}";
        fileCollection.Add(new FormFile(memoryStream, 0, data.Length, name, fileName)
        {
            Headers = request.Headers
        });
    }
}
