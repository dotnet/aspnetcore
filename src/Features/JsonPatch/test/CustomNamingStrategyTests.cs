// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Dynamic;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch;

public class CustomNamingStrategyTests
{
    [Fact]
    public void AddProperty_ToDynamicTestObject_WithCustomNamingStrategy()
    {
        // Arrange
        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new TestNamingStrategy()
        };

        dynamic targetObject = new DynamicTestObject();
        targetObject.Test = 1;

        var patchDocument = new JsonPatchDocument();
        patchDocument.Add("NewInt", 1);
        patchDocument.ContractResolver = contractResolver;

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(1, targetObject.customNewInt);
        Assert.Equal(1, targetObject.Test);
    }

    [Fact]
    public void CopyPropertyValue_ToDynamicTestObject_WithCustomNamingStrategy()
    {
        // Arrange
        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new TestNamingStrategy()
        };

        dynamic targetObject = new DynamicTestObject();
        targetObject.customStringProperty = "A";
        targetObject.customAnotherStringProperty = "B";

        var patchDocument = new JsonPatchDocument();
        patchDocument.Copy("StringProperty", "AnotherStringProperty");
        patchDocument.ContractResolver = contractResolver;

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal("A", targetObject.customAnotherStringProperty);
    }

    [Fact]
    public void MovePropertyValue_ForExpandoObject_WithCustomNamingStrategy()
    {
        // Arrange
        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new TestNamingStrategy()
        };

        dynamic targetObject = new ExpandoObject();
        targetObject.customStringProperty = "A";
        targetObject.customAnotherStringProperty = "B";

        var patchDocument = new JsonPatchDocument();
        patchDocument.Move("StringProperty", "AnotherStringProperty");
        patchDocument.ContractResolver = contractResolver;

        // Act
        patchDocument.ApplyTo(targetObject);
        var cont = targetObject as IDictionary<string, object>;
        cont.TryGetValue("customStringProperty", out var valueFromDictionary);

        // Assert
        Assert.Equal("A", targetObject.customAnotherStringProperty);
        Assert.Null(valueFromDictionary);
    }

    [Fact]
    public void RemoveProperty_FromDictionaryObject_WithCustomNamingStrategy()
    {
        // Arrange
        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new TestNamingStrategy()
        };

        var targetObject = new Dictionary<string, int>()
            {
                { "customTest", 1},
            };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Remove("Test");
        patchDocument.ContractResolver = contractResolver;

        // Act
        patchDocument.ApplyTo(targetObject);
        var cont = targetObject as IDictionary<string, int>;
        cont.TryGetValue("customTest", out var valueFromDictionary);

        // Assert
        Assert.Equal(0, valueFromDictionary);
    }

    [Fact]
    public void ReplacePropertyValue_ForExpandoObject_WithCustomNamingStrategy()
    {
        // Arrange
        var contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new TestNamingStrategy()
        };

        dynamic targetObject = new ExpandoObject();
        targetObject.customTest = 1;

        var patchDocument = new JsonPatchDocument();
        patchDocument.Replace("Test", 2);
        patchDocument.ContractResolver = contractResolver;

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        Assert.Equal(2, targetObject.customTest);
    }

    private class TestNamingStrategy : NamingStrategy
    {
        public new bool ProcessDictionaryKeys => true;

        public override string GetDictionaryKey(string key)
        {
            return "custom" + key;
        }

        protected override string ResolvePropertyName(string name)
        {
            return name;
        }
    }
}
