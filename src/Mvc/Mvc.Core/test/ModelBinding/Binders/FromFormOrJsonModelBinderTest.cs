using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Tests;

public class FromFormOrJsonModelBinderTest
{
    private static DefaultHttpContext CreateHttpContextWithJson<T>(T payload)
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";

        var json = JsonSerializer.Serialize(payload);
        var bytes = Encoding.UTF8.GetBytes(json);
        context.Request.Body = new MemoryStream(bytes);

        return context;
    }

    private static DefaultHttpContext CreateHttpContextWithForm<T>(T payload)
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "multipart/form-data; boundary=----WebKitFormBoundary7MA4YWxkTrZu0gW";
        var form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "payload", JsonSerializer.Serialize(payload) }
        });

        context.Request.Form = form;
        return context;
    }

    public class SampleDto
    {
        public string Name { get; set; }
    }

    [Fact]
    public async Task BindsFromJsonPayload()
    {
        // Arrange
        var dto = new SampleDto { Name = "Json Test" };
        var httpContext = CreateHttpContextWithJson(dto);
        var bindingContext = GetBindingContext<SampleDto>(httpContext);

        var binder = new FromFormOrJsonModelBinder<SampleDto>();

        // Act
        await binder.BindModelAsync(bindingContext);

        // Assert
        Assert.True(bindingContext.Result.IsModelSet);
        var result = Assert.IsType<SampleDto>(bindingContext.Result.Model);
        Assert.Equal("Json Test", result.Name);
    }

    private static DefaultModelBindingContext GetBindingContext<T>(HttpContext httpContext)
    {
        var metadataProvider = new EmptyModelMetadataProvider();
        var modelMetadata = metadataProvider.GetMetadataForType(typeof(T));
        return new DefaultModelBindingContext
        {
            ModelMetadata = modelMetadata,
            ModelName = "model",
            ModelState = new ModelStateDictionary(),
            ValueProvider = new SimpleValueProvider(),
            ActionContext = new ActionContext
            {
                HttpContext = httpContext,
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            },
            FieldName = "model",
            BindingSource = BindingSource.Custom,
            HttpContext = httpContext,
        };
    }
}