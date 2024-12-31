// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestsBase
{
    [Fact]
    public async Task CanValidateOnAllMapOverloads()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder();

var app = builder.Build();

app.Conventions.WithValidation();

app.MapGet("/todos/{id}", ([Range(1, 10)] int id) => id);
app.MapPost("/todos", (Todo todo) => todo.Id);
app.MapPut("/todos", (Todo todo) => todo.Id);
app.MapMethods("/todos/{id}", new [] { "delete", "PaTcH" }, ([Range(1, 10)] int id) => id);

app.Run();

public class Todo
{
    [Range(1, 10)]
    public int Id { get; set; }
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, async client =>
        {
            var response = await client.GetAsync("/todos/12");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            var problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
            var error = Assert.Single(problemDetails.Errors);
            Assert.Equal("id", error.Key);
            Assert.Equal("The field id must be between 1 and 10.", error.Value.Single());

            var invalidPayload = """
            {
                "Id": 12
            }
            """;
            var invalidResponse = await client.PostAsync("/todos", new StringContent(invalidPayload, new MediaTypeHeaderValue("application/json")));
            Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
            problemDetails = await invalidResponse.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
            error = Assert.Single(problemDetails.Errors);
            Assert.Equal("Id", error.Key);
            Assert.Equal("The field Id must be between 1 and 10.", error.Value.Single());

            invalidResponse = await client.PutAsync("/todos", new StringContent(invalidPayload, new MediaTypeHeaderValue("application/json")));
            Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
            problemDetails = await invalidResponse.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
            error = Assert.Single(problemDetails.Errors);
            Assert.Equal("Id", error.Key);
            Assert.Equal("The field Id must be between 1 and 10.", error.Value.Single());

            response = await client.DeleteAsync("/todos/12");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
            error = Assert.Single(problemDetails.Errors);
            Assert.Equal("id", error.Key);
            Assert.Equal("The field id must be between 1 and 10.", error.Value.Single());

            response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/todos/12"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
            error = Assert.Single(problemDetails.Errors);
            Assert.Equal("id", error.Key);
            Assert.Equal("The field id must be between 1 and 10.", error.Value.Single());

            response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/todos/12"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
            error = Assert.Single(problemDetails.Errors);
            Assert.Equal("The field id must be between 1 and 10.", error.Value.Single());
        });
    }
}
