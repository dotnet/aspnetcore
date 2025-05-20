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

        // Define routes with DTOs using primary constructors - both with and without property prefix
        app.MapPost("/users/property-prefix", (UserDtoWithPropertyPrefix dto) => Results.Ok(dto))
            .WithName("CreateUserWithPropertyPrefix")
            .WithOpenApi();
            
        app.MapPost("/users/no-prefix", (UserDtoWithNoPrefix dto) => Results.Ok(dto))
            .WithName("CreateUserWithNoPrefix")
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

            // Assert for property prefix case
            // Find the UserDtoWithPropertyPrefix schema in the components section
            var schemas = openApiDoc["components"]?["schemas"];
            Assert.NotNull(schemas);

            var userDtoWithPropertyPrefixSchema = schemas["UserDtoWithPropertyPrefix"];
            Assert.NotNull(userDtoWithPropertyPrefixSchema);

            // Check that the age property has the Range attribute constraints
            var agePropertyWithPrefix = userDtoWithPropertyPrefixSchema["properties"]?["age"];
            Assert.NotNull(agePropertyWithPrefix);
            Assert.Equal(0, agePropertyWithPrefix["minimum"]?.GetValue<int>());
            Assert.Equal(120, agePropertyWithPrefix["maximum"]?.GetValue<int>());
            
            // Assert for no prefix case
            var userDtoWithNoPrefixSchema = schemas["UserDtoWithNoPrefix"];
            Assert.NotNull(userDtoWithNoPrefixSchema);

            // Check that the age property has the Range attribute constraints, even without property prefix
            var agePropertyNoPrefix = userDtoWithNoPrefixSchema["properties"]?["age"];
            Assert.NotNull(agePropertyNoPrefix);
            Assert.Equal(0, agePropertyNoPrefix["minimum"]?.GetValue<int>());
            Assert.Equal(120, agePropertyNoPrefix["maximum"]?.GetValue<int>());
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    // DTO with property: prefix on attributes
    public class UserDtoWithPropertyPrefix(string name, [property: Range(0, 120)] int age)
    {
        public string Name => name;
        public int Age => age;
    }
    
    // DTO without property: prefix on attributes
    public class UserDtoWithNoPrefix(string name, [Range(0, 120)] int age)
    {
        public string Name => name;
        public int Age => age;
    }
}