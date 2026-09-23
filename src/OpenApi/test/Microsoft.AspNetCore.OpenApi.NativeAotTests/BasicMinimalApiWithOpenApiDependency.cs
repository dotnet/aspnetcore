// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

#pragma warning disable ASP0040

var builder = WebApplication.CreateSlimBuilder();

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
    options.SerializerOptions.Converters.Add(JsonArrayTupleConverters.CreateValueTuple<int, string>());
});

var serializerOptions = new JsonSerializerOptions(AppJsonSerializerContext.Default.Options);
serializerOptions.Converters.Add(JsonArrayTupleConverters.CreateValueTuple<int, string>());
var tupleTypeInfo = (JsonTypeInfo<(int, string)>)serializerOptions.GetTypeInfo(typeof((int, string)));
var tupleJson = JsonSerializer.Serialize((1, "two"), tupleTypeInfo);
var tuple = JsonSerializer.Deserialize(tupleJson, tupleTypeInfo);
if (tupleJson != "[1,\"two\"]" || tuple != (1, "two"))
{
    return -1;
}

var app = builder.Build();

app.MapOpenApi();

app.MapGet("/", () => "Hello World!");
app.MapGet("/{name}", (string name) => $"Hello {name}!");
app.MapGet("/tuple", () => (1, "two"));

return 100;

[JsonSerializable(typeof((int, string)))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;

#pragma warning restore ASP0040
