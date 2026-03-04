#pragma warning disable ASP0029 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.Extensions.Validation.GeneratorTests;

public class ValidationsGeneratorDisplayNameTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task PropertyDisplayName_WithNameOnly()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/property-display-name", (PropertyDisplayNameType model) => Results.Ok("Passed"!));

app.Run();

public class PropertyDisplayNameType
{
    [Range(10, 100, ErrorMessage = "Name:{0}"), Display(Name = "My Custom Name")]
    public int Value { get; set; } = 10;
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/property-display-name", async (endpoint, serviceProvider) =>
        {
            var payload = """{ "Value": 5 }""";
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            var kvp = Assert.Single(problemDetails.Errors);
            Assert.Equal("Value", kvp.Key);
            Assert.Equal("Name:My Custom Name", kvp.Value.Single());
        });
    }

    [Fact]
    public async Task PropertyDisplayName_WithResourceType()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/property-resource-display-name", (PropertyResourceDisplayNameType model) => Results.Ok("Passed"!));

app.Run();

public class PropertyResourceDisplayNameType
{
    [Range(10, 100, ErrorMessage = "Name:{0}"), Display(Name = "ValueDisplayName", ResourceType = typeof(TestResources))]
    public int Value { get; set; } = 10;
}

public class TestResources
{
    public static string ValueDisplayName => "Localized Value Name";
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/property-resource-display-name", async (endpoint, serviceProvider) =>
        {
            var payload = """{ "Value": 5 }""";
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            var kvp = Assert.Single(problemDetails.Errors);
            Assert.Equal("Value", kvp.Key);
            Assert.Equal("Name:Localized Value Name", kvp.Value.Single());
        });
    }

    [Fact]
    public async Task ParameterDisplayName_WithNameOnly()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/param-display-name", (
    [Range(10, 100, ErrorMessage = "Name:{0}"), Display(Name = "Parameter Label")] int value) => "OK");

app.Run();
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/param-display-name", async (endpoint, serviceProvider) =>
        {
            var context = CreateHttpContext(serviceProvider);
            context.Request.QueryString = new QueryString("?value=5");
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            var kvp = Assert.Single(problemDetails.Errors);
            Assert.Equal("value", kvp.Key);
            Assert.Equal("Name:Parameter Label", kvp.Value.Single());
        });
    }

    [Fact]
    public async Task ParameterDisplayName_WithResourceType()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/param-resource-display-name", (
    [Range(10, 100, ErrorMessage = "Name:{0}"), Display(Name = "ParamDisplayName", ResourceType = typeof(ParamResources))] int value) => "OK");

app.Run();

public class ParamResources
{
    public static string ParamDisplayName => "Localized Parameter Name";
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/param-resource-display-name", async (endpoint, serviceProvider) =>
        {
            var context = CreateHttpContext(serviceProvider);
            context.Request.QueryString = new QueryString("?value=5");
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            var kvp = Assert.Single(problemDetails.Errors);
            Assert.Equal("value", kvp.Key);
            Assert.Equal("Name:Localized Parameter Name", kvp.Value.Single());
        });
    }

    [Fact]
    public async Task TypeDisplayName_WithNameOnly()
    {
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/type-display-name", (TypeWithDisplayName model) => Results.Ok("Passed"!));

app.Run();

[Display(Name = "My Model")]
public class TypeWithDisplayName
{
    [Range(10, 100, ErrorMessage = "Name:{0}")]
    public int Value { get; set; } = 10;
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/type-display-name", async (endpoint, serviceProvider) =>
        {
            var payload = """{ "Value": 5 }""";
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            var kvp = Assert.Single(problemDetails.Errors);
            Assert.Equal("Value", kvp.Key);
            Assert.Equal("Name:Value", kvp.Value.Single());
        });
    }

    [Fact]
    public async Task TypeDisplayName_WithResourceType()
    {
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/type-resource-display-name", (TypeWithResourceDisplayName model) => Results.Ok("Passed"!));

app.Run();

[Display(Name = "TypeDisplayName", ResourceType = typeof(TypeResources))]
public class TypeWithResourceDisplayName
{
    [Range(10, 100, ErrorMessage = "Name:{0}")]
    public int Value { get; set; } = 10;
}

public class TypeResources
{
    public static string TypeDisplayName => "Localized Type Name";
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/type-resource-display-name", async (endpoint, serviceProvider) =>
        {
            var payload = """{ "Value": 5 }""";
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            var kvp = Assert.Single(problemDetails.Errors);
            Assert.Equal("Value", kvp.Key);
            Assert.Equal("Name:Value", kvp.Value.Single());
        });
    }

    [Fact]
    public async Task PropertyDisplayName_WithoutDisplayAttribute_UsesPropertyName()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/no-display-attr", (NoDisplayAttributeType model) => Results.Ok("Passed"!));

app.Run();

public class NoDisplayAttributeType
{
    [Range(10, 100, ErrorMessage = "Name:{0}")]
    public int MyProperty { get; set; } = 10;
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/no-display-attr", async (endpoint, serviceProvider) =>
        {
            var payload = """{ "MyProperty": 5 }""";
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            var kvp = Assert.Single(problemDetails.Errors);
            Assert.Equal("MyProperty", kvp.Key);
            Assert.Equal("Name:MyProperty", kvp.Value.Single());
        });
    }

    [Fact]
    public async Task RecordPropertyDisplayName_WithNameOnly()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/record-display-name", (RecordWithDisplayName model) => Results.Ok("Passed"!));

app.Run();

public record RecordWithDisplayName(
    [Range(10, 100, ErrorMessage = "Name:{0}"), Display(Name = "Record Field Label")] int Value);
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/record-display-name", async (endpoint, serviceProvider) =>
        {
            var payload = """{ "Value": 5 }""";
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            var kvp = Assert.Single(problemDetails.Errors);
            Assert.Equal("Value", kvp.Key);
            Assert.Equal("Name:Record Field Label", kvp.Value.Single());
        });
    }

    [Fact]
    public async Task RecordPropertyDisplayName_WithResourceType()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/record-resource-display-name", (RecordWithResourceDisplayName model) => Results.Ok("Passed"!));

app.Run();

public record RecordWithResourceDisplayName(
    [Range(10, 100, ErrorMessage = "Name:{0}"), Display(Name = "RecordValueDisplayName", ResourceType = typeof(RecordResources))] int Value);

public class RecordResources
{
    public static string RecordValueDisplayName => "Localized Record Value";
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/record-resource-display-name", async (endpoint, serviceProvider) =>
        {
            var payload = """{ "Value": 5 }""";
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            var kvp = Assert.Single(problemDetails.Errors);
            Assert.Equal("Value", kvp.Key);
            Assert.Equal("Name:Localized Record Value", kvp.Value.Single());
        });
    }

    [Fact]
    public async Task PropertyDisplayName_WithDisplayNameAttribute()
    {
        var source = """
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/display-name-attr", (DisplayNameAttrType model) => Results.Ok("Passed"!));

app.Run();

public class DisplayNameAttrType
{
    [Range(10, 100, ErrorMessage = "Name:{0}"), DisplayName("Friendly Name")]
    public int Value { get; set; } = 10;
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/display-name-attr", async (endpoint, serviceProvider) =>
        {
            var payload = """{ "Value": 5 }""";
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            var kvp = Assert.Single(problemDetails.Errors);
            Assert.Equal("Value", kvp.Key);
            Assert.Equal("Name:Friendly Name", kvp.Value.Single());
        });
    }

    [Fact]
    public async Task PropertyDisplayName_DisplayAttributeTakesPrecedenceOverDisplayNameAttribute()
    {
        var source = """
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/both-display-attrs", (BothDisplayAttrsType model) => Results.Ok("Passed"!));

app.Run();

public class BothDisplayAttrsType
{
    [Range(10, 100, ErrorMessage = "Name:{0}"), Display(Name = "Display Attr Name"), DisplayName("DisplayName Attr Name")]
    public int Value { get; set; } = 10;
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/both-display-attrs", async (endpoint, serviceProvider) =>
        {
            var payload = """{ "Value": 5 }""";
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            var kvp = Assert.Single(problemDetails.Errors);
            Assert.Equal("Value", kvp.Key);
            Assert.Equal("Name:Display Attr Name", kvp.Value.Single());
        });
    }

    [Fact]
    public async Task PropertyDisplayName_DisplayAttributeResourceTypeTakesPrecedenceOverDisplayNameAttribute()
    {
        var source = """
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/resource-over-displayname", (ResourceOverDisplayNameType model) => Results.Ok("Passed"!));

app.Run();

public class ResourceOverDisplayNameType
{
    [Range(10, 100, ErrorMessage = "Name:{0}"), Display(Name = "ValueDisplayName", ResourceType = typeof(PrecedenceResources)), DisplayName("Should Not Use This")]
    public int Value { get; set; } = 10;
}

public class PrecedenceResources
{
    public static string ValueDisplayName => "Localized From Resource";
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/resource-over-displayname", async (endpoint, serviceProvider) =>
        {
            var payload = """{ "Value": 5 }""";
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            var kvp = Assert.Single(problemDetails.Errors);
            Assert.Equal("Value", kvp.Key);
            Assert.Equal("Name:Localized From Resource", kvp.Value.Single());
        });
    }

    [Fact]
    public async Task TypeDisplayName_WithDisplayNameAttribute()
    {
        var source = """
#pragma warning disable ASP0029

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

builder.Services.AddValidation();

var app = builder.Build();

app.MapPost("/type-displayname-attr", (TypeWithDisplayNameAttr model) => Results.Ok("Passed"!));

app.Run();

[DisplayName("Friendly Type")]
public class TypeWithDisplayNameAttr
{
    [Range(10, 100, ErrorMessage = "Name:{0}")]
    public int Value { get; set; } = 10;
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, "/type-displayname-attr", async (endpoint, serviceProvider) =>
        {
            var payload = """{ "Value": 5 }""";
            var context = CreateHttpContextWithPayload(payload, serviceProvider);
            await endpoint.RequestDelegate(context);

            var problemDetails = await AssertBadRequest(context);
            var kvp = Assert.Single(problemDetails.Errors);
            Assert.Equal("Value", kvp.Key);
            // Type display name doesn't affect per-property error messages;
            // the property still uses its own name as the display name.
            Assert.Equal("Name:Value", kvp.Value.Single());
        });
    }
}
