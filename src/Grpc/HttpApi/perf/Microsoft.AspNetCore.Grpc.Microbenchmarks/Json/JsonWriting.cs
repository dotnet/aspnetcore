// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Google.Protobuf;
using Greet;
using Microsoft.AspNetCore.Grpc.HttpApi;
using Microsoft.AspNetCore.Grpc.HttpApi.Internal.Json;

namespace Microsoft.AspNetCore.Grpc.Microbenchmarks.Json;

public class JsonWriting
{
    private HelloRequest _request = default!;
    private JsonSerializerOptions _serializerOptions = default!;
    private JsonFormatter _jsonFormatter = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _request = new HelloRequest() { Name = "Hello world" };
        _serializerOptions = JsonConverterHelper.CreateSerializerOptions(new JsonSettings { WriteIndented = false });
        _jsonFormatter = new JsonFormatter(new JsonFormatter.Settings(formatDefaultValues: false));
    }

    [Benchmark]
    public void WriteMessage_JsonSerializer()
    {
        JsonSerializer.Serialize(_request, _serializerOptions);
    }

    [Benchmark]
    public void WriteMessage_JsonFormatter()
    {
        _jsonFormatter.Format(_request);
    }
}
