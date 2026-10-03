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
    options.SerializerOptions.Converters.Add(JsonArrayTupleConverters.CreateValueTuple<long, bool>());
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapGet("/{name}", (string name) => $"Hello {name}!");
app.MapGet("/tuple", () => (1, "two"));
app.MapGet("/explicit-tuple", () => (3L, true));
app.MapOpenApi();

var serializerOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>().Value.SerializerOptions;
var tupleTypeInfo = (JsonTypeInfo<(int, string)>)serializerOptions.GetTypeInfo(typeof((int, string)));
var tupleJson = JsonSerializer.Serialize((1, "two"), tupleTypeInfo);
var tuple = JsonSerializer.Deserialize(tupleJson, tupleTypeInfo);
var explicitTupleTypeInfo = (JsonTypeInfo<(long, bool)>)serializerOptions.GetTypeInfo(typeof((long, bool)));
var explicitTupleJson = JsonSerializer.Serialize((3L, true), explicitTupleTypeInfo);
var explicitTuple = JsonSerializer.Deserialize(explicitTupleJson, explicitTupleTypeInfo);
var explicitTupleConverterCount = 0;
foreach (var converter in serializerOptions.Converters)
{
    if (converter.CanConvert(typeof((long, bool))))
    {
        explicitTupleConverterCount++;
    }
}
if (tupleJson != "[1,\"two\"]" ||
    tuple != (1, "two") ||
    explicitTupleJson != "[3,true]" ||
    explicitTuple != (3L, true) ||
    explicitTupleConverterCount != 1)
{
    return -1;
}

return 100;

[JsonSerializable(typeof((int, string)))]
[JsonSerializable(typeof((long, bool)))]
internal sealed partial class AppJsonSerializerContext : JsonSerializerContext;

#pragma warning restore ASP0040
