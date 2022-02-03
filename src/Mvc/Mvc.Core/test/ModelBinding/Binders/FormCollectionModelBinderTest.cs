// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

public class FormCollectionModelBinderTest
{
    [Fact]
    public async Task FormCollectionModelBinder_ValidType_BindSuccessful()
    {
        // Arrange
        var formCollection = new FormCollection(new Dictionary<string, StringValues>
            {
                { "field1", "value1" },
                { "field2", "value2" }
            });
        var httpContext = GetMockHttpContext(formCollection);
        var bindingContext = GetBindingContext(typeof(IFormCollection), httpContext);
        var binder = new FormCollectionModelBinder(NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        Assert.Empty(bindingContext.ValidationState);

        var form = Assert.IsAssignableFrom<IFormCollection>(bindingContext.Result.Model);
        Assert.Equal(2, form.Count);
        Assert.Equal("value1", form["field1"]);
        Assert.Equal("value2", form["field2"]);
    }

    [Fact]
    public async Task FormCollectionModelBinder_NoForm_BindSuccessful_ReturnsEmptyFormCollection()
    {
        // Arrange
        var httpContext = GetMockHttpContext(null, hasForm: false);
        var bindingContext = GetBindingContext(typeof(IFormCollection), httpContext);
        var binder = new FormCollectionModelBinder(NullLoggerFactory.Instance);

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var form = Assert.IsAssignableFrom<IFormCollection>(bindingContext.Result.Model);
        Assert.Empty(form);
    }

    private static HttpContext GetMockHttpContext(IFormCollection formCollection, bool hasForm = true)
    {
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(h => h.Request.ReadFormAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(formCollection));
        httpContext.Setup(h => h.Request.HasFormContentType).Returns(hasForm);
        return httpContext.Object;
    }

    private static DefaultModelBindingContext GetBindingContext(Type modelType, HttpContext httpContext)
    {
        var metadataProvider = new EmptyModelMetadataProvider();
        var bindingContext = new DefaultModelBindingContext
        {
            ActionContext = new ActionContext()
            {
                HttpContext = httpContext,
            },
            ModelMetadata = metadataProvider.GetMetadataForType(modelType),
            ModelName = "file",
            ValidationState = new ValidationStateDictionary(),
        };

        return bindingContext;
    }
}
