// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.AspNetCore.JsonPatch.Exceptions;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.IntegrationTests;

public class DictionaryTest
{
    [Fact]
    public void TestIntegerValue_IsSuccessful()
    {
        // Arrange
        var model = new IntDictionary();
        model.DictionaryOfStringToInteger["one"] = 1;
        model.DictionaryOfStringToInteger["two"] = 2;
        var patchDocument = new JsonPatchDocument();
        patchDocument.Test("/DictionaryOfStringToInteger/two", 2);

        // Act & Assert
        patchDocument.ApplyTo(model);
    }

    [Fact]
    public void AddIntegerValue_Succeeds()
    {
        // Arrange
        var model = new IntDictionary();
        model.DictionaryOfStringToInteger["one"] = 1;
        model.DictionaryOfStringToInteger["two"] = 2;
        var patchDocument = new JsonPatchDocument();
        patchDocument.Add("/DictionaryOfStringToInteger/three", 3);

        // Act
        patchDocument.ApplyTo(model);

        // Assert
        Assert.Equal(3, model.DictionaryOfStringToInteger.Count);
        Assert.Equal(1, model.DictionaryOfStringToInteger["one"]);
        Assert.Equal(2, model.DictionaryOfStringToInteger["two"]);
        Assert.Equal(3, model.DictionaryOfStringToInteger["three"]);
    }

    [Fact]
    public void RemoveIntegerValue_Succeeds()
    {
        // Arrange
        var model = new IntDictionary();
        model.DictionaryOfStringToInteger["one"] = 1;
        model.DictionaryOfStringToInteger["two"] = 2;
        var patchDocument = new JsonPatchDocument();
        patchDocument.Remove("/DictionaryOfStringToInteger/two");

        // Act
        patchDocument.ApplyTo(model);

        // Assert
        Assert.Equal(1, model.DictionaryOfStringToInteger.Count);
        Assert.Equal(1, model.DictionaryOfStringToInteger["one"]);
    }

    [Fact]
    public void MoveIntegerValue_Succeeds()
    {
        // Arrange
        var model = new IntDictionary();
        model.DictionaryOfStringToInteger["one"] = 1;
        model.DictionaryOfStringToInteger["two"] = 2;
        var patchDocument = new JsonPatchDocument();
        patchDocument.Move("/DictionaryOfStringToInteger/one", "/DictionaryOfStringToInteger/two");

        // Act
        patchDocument.ApplyTo(model);

        // Assert
        Assert.Equal(1, model.DictionaryOfStringToInteger.Count);
        Assert.Equal(1, model.DictionaryOfStringToInteger["two"]);
    }

    [Fact]
    public void ReplaceIntegerValue_Succeeds()
    {
        // Arrange
        var model = new IntDictionary();
        model.DictionaryOfStringToInteger["one"] = 1;
        model.DictionaryOfStringToInteger["two"] = 2;
        var patchDocument = new JsonPatchDocument();
        patchDocument.Replace("/DictionaryOfStringToInteger/two", 20);

        // Act
        patchDocument.ApplyTo(model);

        // Assert
        Assert.Equal(2, model.DictionaryOfStringToInteger.Count);
        Assert.Equal(1, model.DictionaryOfStringToInteger["one"]);
        Assert.Equal(20, model.DictionaryOfStringToInteger["two"]);
    }

    [Fact]
    public void CopyIntegerValue_Succeeds()
    {
        // Arrange
        var model = new IntDictionary();
        model.DictionaryOfStringToInteger["one"] = 1;
        model.DictionaryOfStringToInteger["two"] = 2;
        var patchDocument = new JsonPatchDocument();
        patchDocument.Copy("/DictionaryOfStringToInteger/one", "/DictionaryOfStringToInteger/two");

        // Act
        patchDocument.ApplyTo(model);

        // Assert
        Assert.Equal(2, model.DictionaryOfStringToInteger.Count);
        Assert.Equal(1, model.DictionaryOfStringToInteger["one"]);
        Assert.Equal(1, model.DictionaryOfStringToInteger["two"]);
    }

    private class Customer
    {
        public string Name { get; set; }
        public Address Address { get; set; }
    }

    private class Address
    {
        public string City { get; set; }
    }

    private class IntDictionary
    {
        public IDictionary<string, int> DictionaryOfStringToInteger { get; } = new Dictionary<string, int>();
    }

    private class CustomerDictionary
    {
        public IDictionary<int, Customer> DictionaryOfStringToCustomer { get; } = new Dictionary<int, Customer>();
    }

    [Fact]
    public void TestPocoObject_Succeeds()
    {
        // Arrange
        var key1 = 100;
        var value1 = new Customer() { Name = "James" };
        var model = new CustomerDictionary();
        model.DictionaryOfStringToCustomer[key1] = value1;
        var patchDocument = new JsonPatchDocument();
        patchDocument.Test($"/DictionaryOfStringToCustomer/{key1}/Name", "James");

        // Act & Assert
        patchDocument.ApplyTo(model);
    }

    [Fact]
    public void TestPocoObject_FailsWhenTestValueIsNotEqualToObjectValue()
    {
        // Arrange
        var key1 = 100;
        var value1 = new Customer() { Name = "James" };
        var model = new CustomerDictionary();
        model.DictionaryOfStringToCustomer[key1] = value1;
        var patchDocument = new JsonPatchDocument();
        patchDocument.Test($"/DictionaryOfStringToCustomer/{key1}/Name", "Mike");

        // Act
        var exception = Assert.Throws<JsonPatchException>(() =>
        {
            patchDocument.ApplyTo(model);
        });

        // Assert
        Assert.Equal("The current value 'James' at path 'Name' is not equal to the test value 'Mike'.", exception.Message);
    }

    [Fact]
    public void AddReplacesPocoObject_Succeeds()
    {
        // Arrange
        var key1 = 100;
        var value1 = new Customer() { Name = "Jamesss" };
        var key2 = 200;
        var value2 = new Customer() { Name = "Mike" };
        var model = new CustomerDictionary();
        model.DictionaryOfStringToCustomer[key1] = value1;
        model.DictionaryOfStringToCustomer[key2] = value2;
        var patchDocument = new JsonPatchDocument();
        patchDocument.Add($"/DictionaryOfStringToCustomer/{key1}/Name", "James");

        // Act
        patchDocument.ApplyTo(model);

        // Assert
        Assert.Equal(2, model.DictionaryOfStringToCustomer.Count);
        var actualValue1 = model.DictionaryOfStringToCustomer[key1];
        Assert.NotNull(actualValue1);
        Assert.Equal("James", actualValue1.Name);
    }

    [Fact]
    public void RemovePocoObject_Succeeds()
    {
        // Arrange
        var key1 = 100;
        var value1 = new Customer() { Name = "Jamesss" };
        var key2 = 200;
        var value2 = new Customer() { Name = "Mike" };
        var model = new CustomerDictionary();
        model.DictionaryOfStringToCustomer[key1] = value1;
        model.DictionaryOfStringToCustomer[key2] = value2;
        var patchDocument = new JsonPatchDocument();
        patchDocument.Remove($"/DictionaryOfStringToCustomer/{key1}/Name");

        // Act
        patchDocument.ApplyTo(model);

        // Assert
        var actualValue1 = model.DictionaryOfStringToCustomer[key1];
        Assert.Null(actualValue1.Name);
    }

    [Fact]
    public void MovePocoObject_Succeeds()
    {
        // Arrange
        var key1 = 100;
        var value1 = new Customer() { Name = "James" };
        var key2 = 200;
        var value2 = new Customer() { Name = "Mike" };
        var model = new CustomerDictionary();
        model.DictionaryOfStringToCustomer[key1] = value1;
        model.DictionaryOfStringToCustomer[key2] = value2;
        var patchDocument = new JsonPatchDocument();
        patchDocument.Move($"/DictionaryOfStringToCustomer/{key1}/Name", $"/DictionaryOfStringToCustomer/{key2}/Name");

        // Act
        patchDocument.ApplyTo(model);

        // Assert
        var actualValue2 = model.DictionaryOfStringToCustomer[key2];
        Assert.NotNull(actualValue2);
        Assert.Equal("James", actualValue2.Name);
    }

    [Fact]
    public void CopyPocoObject_Succeeds()
    {
        // Arrange
        var key1 = 100;
        var value1 = new Customer() { Name = "James" };
        var key2 = 200;
        var value2 = new Customer() { Name = "Mike" };
        var model = new CustomerDictionary();
        model.DictionaryOfStringToCustomer[key1] = value1;
        model.DictionaryOfStringToCustomer[key2] = value2;
        var patchDocument = new JsonPatchDocument();
        patchDocument.Copy($"/DictionaryOfStringToCustomer/{key1}/Name", $"/DictionaryOfStringToCustomer/{key2}/Name");

        // Act
        patchDocument.ApplyTo(model);

        // Assert
        Assert.Equal(2, model.DictionaryOfStringToCustomer.Count);
        var actualValue2 = model.DictionaryOfStringToCustomer[key2];
        Assert.NotNull(actualValue2);
        Assert.Equal("James", actualValue2.Name);
    }

    [Fact]
    public void ReplacePocoObject_Succeeds()
    {
        // Arrange
        var key1 = 100;
        var value1 = new Customer() { Name = "Jamesss" };
        var key2 = 200;
        var value2 = new Customer() { Name = "Mike" };
        var model = new CustomerDictionary();
        model.DictionaryOfStringToCustomer[key1] = value1;
        model.DictionaryOfStringToCustomer[key2] = value2;
        var patchDocument = new JsonPatchDocument();
        patchDocument.Replace($"/DictionaryOfStringToCustomer/{key1}/Name", "James");

        // Act
        patchDocument.ApplyTo(model);

        // Assert
        Assert.Equal(2, model.DictionaryOfStringToCustomer.Count);
        var actualValue1 = model.DictionaryOfStringToCustomer[key1];
        Assert.NotNull(actualValue1);
        Assert.Equal("James", actualValue1.Name);
    }

    [Fact]
    public void ReplacePocoObject_WithEscaping_Succeeds()
    {
        // Arrange
        var key1 = "Foo/Name";
        var value1 = 100;
        var key2 = "Foo";
        var value2 = 200;
        var model = new IntDictionary();
        model.DictionaryOfStringToInteger[key1] = value1;
        model.DictionaryOfStringToInteger[key2] = value2;
        var patchDocument = new JsonPatchDocument();
        patchDocument.Replace($"/DictionaryOfStringToInteger/Foo~1Name", 300);

        // Act
        patchDocument.ApplyTo(model);

        // Assert
        Assert.Equal(2, model.DictionaryOfStringToInteger.Count);
        var actualValue1 = model.DictionaryOfStringToInteger[key1];
        var actualValue2 = model.DictionaryOfStringToInteger[key2];
        Assert.Equal(300, actualValue1);
        Assert.Equal(200, actualValue2);
    }
}
