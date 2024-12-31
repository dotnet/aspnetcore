// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Asn1;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestsBase
{
    [Fact]
    public async Task CanValidateRecursiveTypes()
    {
        var source = """
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();

var app = builder.Build();

app.Conventions.WithValidation();

app.MapPost("/recursive-type", (RecursiveType model) => Results.Ok());

app.Run();

public class RecursiveType
{
    [Range(10, 100)]
    public int Value { get; set; }
    public RecursiveType? Next { get; set; }
}
""";
        await Verify(source, out var compilation);
        await VerifyEndpoint(compilation, async client =>
        {
            await RecursiveTypeRespectsMaximumDepth(client);

            static async Task RecursiveTypeRespectsMaximumDepth(HttpClient client)
            {
                var payload = """
                {
                    "value": 1,
                    "next": {
                        "value": 2,
                        "next": {
                        "value": 3,
                        "next": {
                            "value": 4,
                            "next": {
                            "value": 5,
                            "next": {
                                "value": 6,
                                "next": {
                                "value": 7,
                                "next": {
                                    "value": 8
                                }
                                }
                            }
                            }
                        }
                        }
                    }
                }
                """;
                var response = await client.PostAsync("/recursive-type", new StringContent(payload, new MediaTypeHeaderValue("application/json")));
                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                var problemDetails = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();
                Assert.Collection(problemDetails.Errors,
                kvp =>
                {
                    Assert.Equal("Value", kvp.Key);
                    Assert.Equal("The field Value must be between 10 and 100.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("Next.Value", kvp.Key);
                    Assert.Equal("The field Value must be between 10 and 100.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("Next.Next.Value", kvp.Key);
                    Assert.Equal("The field Value must be between 10 and 100.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("Next.Next.Next.Value", kvp.Key);
                    Assert.Equal("The field Value must be between 10 and 100.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("Next.Next.Next.Next.Value", kvp.Key);
                    Assert.Equal("The field Value must be between 10 and 100.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("Next.Next.Next.Next.Next.Value", kvp.Key);
                    Assert.Equal("The field Value must be between 10 and 100.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("Next.Next.Next.Next.Next.Next.Value", kvp.Key);
                    Assert.Equal("The field Value must be between 10 and 100.", kvp.Value.Single());
                },
                kvp =>
                {
                    Assert.Equal("Next.Next.Next.Next.Next.Next.Next.Value", kvp.Key);
                    Assert.Equal("The field Value must be between 10 and 100.", kvp.Value.Single());
                });
            }
        });
    }
}
