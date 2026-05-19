// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson;

public class CustomNamingStrategyTests
{
    [Fact]
    public void RemoveProperty_FromDictionaryObject_WithCustomNamingStrategy()
    {
        // Arrange
        var serializerOptions = new JsonSerializerOptions();
        serializerOptions.PropertyNamingPolicy = new TestNamingPolicy();
        serializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver();

        var targetObject = new Dictionary<string, int>()
            {
                { "customTest", 1},
            };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Remove("customTest");
        patchDocument.SerializerOptions = serializerOptions;

        // Act
        patchDocument.ApplyTo(targetObject);
        var cont = targetObject as IDictionary<string, int>;
        cont.TryGetValue("customTest", out var valueFromDictionary);

        // Assert
        Assert.Equal(0, valueFromDictionary);
    }

    private class TestNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return "custom" + name;
        }
    }
}
