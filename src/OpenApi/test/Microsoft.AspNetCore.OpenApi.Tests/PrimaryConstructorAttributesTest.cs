// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.OpenApi.Tests;

public class PrimaryConstructorAttributesTest
{
    [Fact]
    public async Task OpenApi_IncludesAttributesFromPrimaryConstructorParameters()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Define routes with DTOs using primary constructors
        app.MapPost("/users", (UserDto dto) => Results.Ok(dto))
            .WithName("CreateUser")
            .WithOpenApi();

        app.MapOpenApi(); // GET /openapi/v1.json

        await app.StartAsync();

        try
        {
            // Act
            var client = new HttpClient();
            var openApiUrl = $"http://{app.Urls.First()}/openapi/v1.json";
            var response = await client.GetAsync(openApiUrl);
            response.EnsureSuccessStatusCode();

            var openApiDoc = await response.Content.ReadFromJsonAsync<JsonNode>();
            Assert.NotNull(openApiDoc);

            // Assert
            // Find the UserDto schema in the components section
            var schemas = openApiDoc["components"]?["schemas"];
            Assert.NotNull(schemas);

            var userDtoSchema = schemas["UserDto"];
            Assert.NotNull(userDtoSchema);

            // Check that the age property has the Range attribute constraints
            var ageProperty = userDtoSchema["properties"]?["age"];
            Assert.NotNull(ageProperty);
            
            Assert.Equal(0, ageProperty["minimum"]?.GetValue<int>());
            Assert.Equal(120, ageProperty["maximum"]?.GetValue<int>());
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    // Simple DTO that uses a primary constructor (C# 12 feature)
    public class UserDto(string name, [property: Range(0, 120)] int age)
    {
        public string Name => name;
        public int Age => age;
    }
}