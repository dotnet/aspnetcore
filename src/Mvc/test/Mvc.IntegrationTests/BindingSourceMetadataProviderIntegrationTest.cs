// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class BindingSourceMetadataProviderIntegrationTest
{
    [Fact]
    public async Task BindParameter_WithCancellationToken_BindingSourceSpecial()
    {
        // Arrange
        var options = new MvcOptions();
        var setup = new MvcCoreMvcOptionsSetup(new TestHttpRequestStreamReaderFactory());

        options.ModelBinderProviders.Insert(0, new CancellationTokenModelBinderProvider());

        setup.Configure(options);

        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(options);
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo(),
            ParameterType = typeof(CancellationTokenBundle),
        };

        var testContext = ModelBindingTestHelper.GetTestContext(request =>
        {
            request.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                    { "name", new[] { "Fred" } }
            });
        });

        var modelState = testContext.ModelState;
        var token = testContext.HttpContext.RequestAborted;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var boundPerson = Assert.IsType<CancellationTokenBundle>(modelBindingResult.Model);
        Assert.NotNull(boundPerson);
        Assert.Equal("Fred", boundPerson.Name);
        Assert.Equal(token, boundPerson.Token);

        // ModelState
        Assert.True(modelState.IsValid);
    }

    private class CancellationTokenBundle
    {
        public string Name { get; set; }

        public CancellationToken Token { get; set; }
    }

    [Fact]
    public async Task BindParameter_WithFormFile_BindingSourceFormFile()
    {
        // Arrange
        var options = new MvcOptions();
        var setup = new MvcCoreMvcOptionsSetup(new TestHttpRequestStreamReaderFactory());

        options.ModelBinderProviders.Insert(0, new FormFileModelBinderProvider());

        setup.Configure(options);

        var parameterBinder = ModelBindingTestHelper.GetParameterBinder(options);
        var parameter = new ParameterDescriptor()
        {
            Name = "Parameter1",
            BindingInfo = new BindingInfo(),
            ParameterType = typeof(FormFileBundle),
        };

        var data = "Some Data Is Better Than No Data.";
        var testContext = ModelBindingTestHelper.GetTestContext(
            request =>
            {
                request.QueryString = QueryString.Create("Name", "Fred");
                UpdateRequest(request, data, "File");
            });

        var modelState = testContext.ModelState;

        // Act
        var modelBindingResult = await parameterBinder.BindModelAsync(parameter, testContext);

        // Assert
        // ModelBindingResult
        Assert.True(modelBindingResult.IsModelSet);

        // Model
        var boundPerson = Assert.IsType<FormFileBundle>(modelBindingResult.Model);
        Assert.Equal("Fred", boundPerson.Name);
        Assert.Equal("text.txt", boundPerson.File.FileName);

        // ModelState
        Assert.True(modelState.IsValid);
    }

    private class FormFileBundle
    {
        public string Name { get; set; }

        public IFormFile File { get; set; }
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
