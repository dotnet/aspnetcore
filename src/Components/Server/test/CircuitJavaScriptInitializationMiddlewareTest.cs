// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Builder;

public class CircuitJavaScriptInitializationMiddlewareTest
{
    [Fact]
    public async Task InvokeAsync_UsesGeneratedMetadataWithoutReflectionResolver()
    {
        var jsonOptions = new JsonOptions();
        jsonOptions.SerializerOptions.TypeInfoResolverChain.Clear();

        var response = await InvokeMiddlewareAsync(jsonOptions);

        Assert.Equal("""["./initializer.js"]""", response);
    }

    [Fact]
    public async Task InvokeAsync_UsesConfiguredHttpJsonOptionsWithoutReflectionResolver()
    {
        var jsonOptions = new JsonOptions();
        jsonOptions.SerializerOptions.TypeInfoResolverChain.Clear();
        jsonOptions.SerializerOptions.Converters.Add(new InitializersConverter());

        var response = await InvokeMiddlewareAsync(jsonOptions);

        Assert.Equal("\"configured\"", response);
    }

    private static async Task<string> InvokeMiddlewareAsync(JsonOptions jsonOptions)
    {
        var circuitOptions = new CircuitOptions();
        circuitOptions.JavaScriptInitializers.Add("./initializer.js");
        var middleware = new CircuitJavaScriptInitializationMiddleware(
            Options.Create(circuitOptions),
            Options.Create(jsonOptions),
            _ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private sealed class InitializersConverter : JsonConverter<IList<string>>
    {
        public override IList<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new NotSupportedException();

        public override void Write(Utf8JsonWriter writer, IList<string> value, JsonSerializerOptions options)
            => writer.WriteStringValue("configured");
    }
}
