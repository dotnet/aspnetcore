// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Microsoft.AspNetCore.OpenApi.Tests;

public class NullableEnumDefaultTests
{
    [Fact]
    public void ApplyParameterInfo_SerializesNullableEnumDefault()
    {
        var parameter = typeof(NullableEnumDefaultTests).GetMethod(nameof(Handler), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetParameters()[0];
        var description = new ApiParameterDescription
        {
            ParameterDescriptor = new ControllerParameterDescriptor { ParameterInfo = parameter }
        };
        var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
        var schema = new JsonObject();

        schema.ApplyParameterInfo(description, options.GetTypeInfo(typeof(Status?)));

        Assert.Equal(1, schema["default"]!.GetValue<int>());
    }

    private static void Handler(Status? status = Status.Approved) { }

    public enum Status { Pending, Approved }
}
