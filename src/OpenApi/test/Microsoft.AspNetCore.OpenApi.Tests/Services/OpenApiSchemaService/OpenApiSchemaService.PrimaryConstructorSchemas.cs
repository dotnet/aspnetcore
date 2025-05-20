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
    [InlineData(true)]  // Test with record
    [InlineData(false)] // Test with class
    public async Task GetOpenApiSchema_HandlesAttributesOnPrimaryConstructorParameters(bool useRecord)
    {
        // Arrange
        var builder = CreateBuilder();

        // Act
        if (useRecord)
        {
            builder.MapPost("/api/record", (RecordWithPrimaryConstructor dto) => dto);
        }
        else
        {
            builder.MapPost("/api/class", (ClassWithPrimaryConstructor dto) => dto);
        }

        // Assert
        await VerifyOpenApiDocument(builder, document =>
        {
            var path = useRecord ? "/api/record" : "/api/class";
            var operation = document.Paths[path].Operations[HttpMethod.Post];
            var requestBody = operation.RequestBody.Content["application/json"].Schema;

            var schemaType = useRecord ? nameof(RecordWithPrimaryConstructor) : nameof(ClassWithPrimaryConstructor);
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

    public record RecordWithPrimaryConstructor(
        [Required] string name, 
        [property: Range(0, 120)] int age);

    public class ClassWithPrimaryConstructor(
        [Required] string name, 
        [property: Range(0, 120)] int age)
    {
        public string Name => name;
        public int Age => age;
    }
}