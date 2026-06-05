// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing;

public class ValidationEndpointFilterFactoryTests
{
    [Fact]
    public async Task Validate_NullableValueTypeParameter_WhenNull_RunsCustomValidationAttributes()
    {
        var services = new ServiceCollection();
        services.AddValidation();
        var serviceProvider = services.BuildServiceProvider();

        var builder = new DefaultEndpointRouteBuilder(new ApplicationBuilder(serviceProvider));
        builder.MapGet("validation-test", ([AlwaysFail] int? id) => "Validation enabled here.");

        var dataSource = Assert.Single(builder.DataSources);
        var endpoint = Assert.Single(dataSource.Endpoints);

        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };

        context.Request.Method = "GET";
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await endpoint.RequestDelegate(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.StartsWith(MediaTypeNames.Application.Json, context.Response.ContentType, StringComparison.OrdinalIgnoreCase);

        responseBody.Seek(0, SeekOrigin.Begin);
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(responseBody, JsonSerializerOptions.Web);
        Assert.NotNull(problemDetails);
        Assert.True(problemDetails.Extensions.TryGetValue("errors", out var errorsObject));

        var errors = Assert.IsType<JsonElement>(errorsObject);
        var error = Assert.Single(errors.EnumerateObject());
        Assert.Equal("id", error.Name);
        Assert.Equal("Always failing attribute.", error.Value.EnumerateArray().Single().GetString());
    }

    private sealed class DefaultEndpointRouteBuilder(IApplicationBuilder applicationBuilder) : IEndpointRouteBuilder
    {
        private IApplicationBuilder ApplicationBuilder { get; } = applicationBuilder ?? throw new ArgumentNullException(nameof(applicationBuilder));
        public IApplicationBuilder CreateApplicationBuilder() => ApplicationBuilder.New();
        public ICollection<EndpointDataSource> DataSources { get; } = [];
        public IServiceProvider ServiceProvider => ApplicationBuilder.ApplicationServices;
    }

    private sealed class AlwaysFailAttribute : ValidationAttribute
    {
        public AlwaysFailAttribute()
            => ErrorMessage = "Always failing attribute.";

        public override bool IsValid(object value) => false;
    }
}
