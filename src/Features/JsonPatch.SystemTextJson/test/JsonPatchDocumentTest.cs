// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Converters;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Exceptions;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson;

public class JsonPatchDocumentTest
{
    [Fact]
    public void InvalidPathAtBeginningShouldThrowException()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument();

        // Act
        var exception = Assert.Throws<JsonPatchException>(() =>
        {
            patchDocument.Add("//NewInt", 1);
        });

        // Assert
        Assert.Equal(
           "The provided string '//NewInt' is an invalid path.",
            exception.Message);
    }

    [Fact]
    public void InvalidPathAtEndShouldThrowException()
    {
        // Arrange
        var patchDocument = new JsonPatchDocument();

        // Act
        var exception = Assert.Throws<JsonPatchException>(() =>
        {
            patchDocument.Add("NewInt//", 1);
        });

        // Assert
        Assert.Equal(
           "The provided string 'NewInt//' is an invalid path.",
            exception.Message);
    }

    [Fact]
    public void NonGenericPatchDocToGenericMustSerialize()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            StringProperty = "A",
            AnotherStringProperty = "B"
        };

        var patchDocument = new JsonPatchDocument();
        patchDocument.Copy("StringProperty", "AnotherStringProperty");

        var serialized = JsonSerializer.Serialize(patchDocument);
        var deserialized = JsonSerializer.Deserialize<JsonPatchDocument<SimpleObject>>(serialized);

        // Act
        deserialized.ApplyTo(targetObject);

        // Assert
        Assert.Equal("A", targetObject.AnotherStringProperty);
    }

    public class Employee
    {
        public int EmployeeId { get; set; }
        public string Name { get; set; }
    }

    public class SalariedEmployee : Employee
    {
        public decimal AnnualSalary { get; set; }
    }

    public class Organization
    {
        public List<Employee> Employees { get; } = new();
    }

    [Fact]
    public void ListWithGenericTypeWorkForSpecificChildren()
    {
        //Arrange
        var org = new Organization();
        // Populate Employees with two employees
        org.Employees.Add(new SalariedEmployee { EmployeeId = 2, Name = "Jane", AnnualSalary = 50000 });
        org.Employees.Add(new Employee { EmployeeId = 1, Name = "John" });

        var doc = new JsonPatchDocument<Organization>();
        doc.Operations.Add(new Operations.Operation<Organization>("add", "/Employees/0/AnnualSalary", "", 100));

        // Act
        doc.ApplyTo(org);

        // Assert
        Assert.Equal(100, (org.Employees[0] as SalariedEmployee).AnnualSalary);
    }

    [Fact]
    public void GenericPatchDocToNonGenericMustSerialize()
    {
        // Arrange
        var targetObject = new SimpleObject()
        {
            StringProperty = "A",
            AnotherStringProperty = "B"
        };

        var patchDocTyped = new JsonPatchDocument<SimpleObject>();
        patchDocTyped.Copy(o => o.StringProperty, o => o.AnotherStringProperty);

        var patchDocUntyped = new JsonPatchDocument();
        patchDocUntyped.Copy("StringProperty", "AnotherStringProperty");

        var serializedTyped = JsonSerializer.Serialize(patchDocTyped);
        var serializedUntyped = JsonSerializer.Serialize(patchDocUntyped);
        var deserialized = JsonSerializer.Deserialize<JsonPatchDocument>(serializedTyped);

        // Act
        deserialized.ApplyTo(targetObject);

        // Assert
        Assert.Equal("A", targetObject.AnotherStringProperty);
    }

    [Fact]
    public void Deserialization_Successful_ForValidJsonPatchDocument()
    {
        // Arrange
        var doc = new SimpleObject()
        {
            StringProperty = "A",
            DecimalValue = 10,
            DoubleValue = 10,
            FloatValue = 10,
            IntegerValue = 10
        };

        var patchDocument = new JsonPatchDocument<SimpleObject>();
        patchDocument.Replace(o => o.StringProperty, "B");
        patchDocument.Replace(o => o.DecimalValue, 12);
        patchDocument.Replace(o => o.DoubleValue, 12);
        patchDocument.Replace(o => o.FloatValue, 12);
        patchDocument.Replace(o => o.IntegerValue, 12);

        // default: no envelope
        var serialized = JsonSerializer.Serialize(patchDocument);

        // Act
        var deserialized = JsonSerializer.Deserialize<JsonPatchDocument<SimpleObject>>(serialized);

        // Assert
        Assert.IsType<JsonPatchDocument<SimpleObject>>(deserialized);
    }

    [Fact]
    public void Deserialization_Fails_ForInvalidJsonPatchDocument()
    {
        // Arrange
        var serialized = "{\"Operations\": [{ \"op\": \"replace\", \"path\": \"/title\", \"value\": \"New Title\"}]}";

        // Act
        var exception = Assert.Throws<JsonException>(() =>
        {
            var deserialized
                = JsonSerializer.Deserialize<JsonPatchDocument>(serialized);
        });

        // Assert
        Assert.Equal("The JSON patch document was malformed and could not be parsed.", exception.Message);
    }

    [Fact]
    public void Deserialization_Fails_ForInvalidTypedJsonPatchDocument()
    {
        // Arrange
        var serialized = "{\"Operations\": [{ \"op\": \"replace\", \"path\": \"/title\", \"value\": \"New Title\"}]}";

        // Act
        var exception = Assert.Throws<JsonException>(() =>
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            options.Converters.Add(new JsonConverterForJsonPatchDocumentOfT<SimpleObject>());
            var deserialized
                = JsonSerializer.Deserialize<JsonPatchDocument<SimpleObject>>(serialized, options);
        });

        // Assert
        Assert.Equal("The JSON patch document was malformed and could not be parsed.", exception.Message);
    }

    [Fact]
    public void Deserialization_RespectsNamingPolicy()
    {
        // Arrange
        var childToAdd = new SimpleObject
        {
            GuidValue = Guid.NewGuid(),
            StringProperty = "some test data"
        };

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;

        var json = GeneratePatchDocumentJson(childToAdd, options);

        var getTestObject = () => new SimpleObject() { SimpleObjectList = new() };

        //Act
        var docSuccess = DeserializePatchDocumentWithNamingPolicy(json, JsonNamingPolicy.CamelCase);
        var docFail = DeserializePatchDocumentWithNamingPolicy(json, JsonNamingPolicy.KebabCaseLower);

        // Assert

        // The following call should succeed
        docSuccess.ApplyTo(getTestObject());

        // The following call should fail
        Assert.Throws<JsonPatchException>(() =>
        {
            docFail.ApplyTo(getTestObject());
        });
    }

    private static JsonPatchDocument<SimpleObject> DeserializePatchDocumentWithNamingPolicy(string json, JsonNamingPolicy policy)
    {
        var compatibleSerializerOption = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        compatibleSerializerOption.PropertyNamingPolicy = policy;
        var docSuccess = JsonSerializer.Deserialize<JsonPatchDocument<SimpleObject>>(json, compatibleSerializerOption);
        return docSuccess;
    }

    private string GeneratePatchDocumentJson(SimpleObject toAdd, JsonSerializerOptions jsonSerializerOptions)
    {
        var document = new JsonPatchDocument<SimpleObject>();
        var operation = new Operation<SimpleObject>
        {
            op = "add",
            path = "/simpleObjectList/-",
            value = toAdd
        };
        document.Operations.Add(operation);

        return JsonSerializer.Serialize<JsonPatchDocument<SimpleObject>>(document, jsonSerializerOptions);
    }
}
