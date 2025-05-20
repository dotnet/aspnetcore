// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.OpenApi.Models;

public partial class OpenApiSchemaServiceTests
{
    [Theory]
    [InlineData(true, true)]   // Test with record, property prefix
    [InlineData(false, true)]  // Test with class, property prefix
    [InlineData(true, false)]  // Test with record, no property prefix
    [InlineData(false, false)] // Test with class, no property prefix
    public async Task GetOpenApiSchema_HandlesAttributesOnPrimaryConstructorParameters(bool useRecord, bool usePropertyPrefix)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        if (useRecord)
        {
            if (usePropertyPrefix)
            {
                builder.MapPost("/api/record/property-prefix", (RecordWithPropertyPrefix dto) => dto);
            }
            else
            {
                builder.MapPost("/api/record/no-prefix", (RecordWithNoPrefix dto) => dto);
            }
        }
        else
        {
            if (usePropertyPrefix)
            {
                builder.MapPost("/api/class/property-prefix", (ClassWithPropertyPrefix dto) => dto);
            }
            else
            {
                builder.MapPost("/api/class/no-prefix", (ClassWithNoPrefix dto) => dto);
            }
        }

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            string path;
            string schemaType;

            if (useRecord)
            {
                path = usePropertyPrefix ? "/api/record/property-prefix" : "/api/record/no-prefix";
                schemaType = usePropertyPrefix ? nameof(RecordWithPropertyPrefix) : nameof(RecordWithNoPrefix);
            }
            else
            {
                path = usePropertyPrefix ? "/api/class/property-prefix" : "/api/class/no-prefix";
                schemaType = usePropertyPrefix ? nameof(ClassWithPropertyPrefix) : nameof(ClassWithNoPrefix);
            }

            var operation = document.Paths[path].Operations[HttpMethod.Post];
            var requestBody = operation.RequestBody.Content["application/json"].Schema;

            Assert.Equal(schemaType, requestBody.Reference.Id);

            var schemaObject = document.Components.Schemas[schemaType];
            
            // Verify the 'age' property has the min/max constraints from the Range attribute
            var ageProperty = schemaObject.Properties["age"];
            Assert.Equal(0, ageProperty.Minimum);
            Assert.Equal(120, ageProperty.Maximum);
            
            // Verify the 'name' property is required from the Required attribute
            var nameProperty = schemaObject.Properties["name"];
            Assert.Contains("name", schemaObject.Required);
        });
    }

    public record RecordWithPropertyPrefix(
        [Required] string name, 
        [property: Range(0, 120)] int age);

    public class ClassWithPropertyPrefix(
        [Required] string name, 
        [property: Range(0, 120)] int age)
    {
        public string Name => name;
        public int Age => age;
    }

    public record RecordWithNoPrefix(
        [Required] string name, 
        [Range(0, 120)] int age);

    public class ClassWithNoPrefix(
        [Required] string name, 
        [Range(0, 120)] int age)
    {
        public string Name => name;
        public int Age => age;
    }
}