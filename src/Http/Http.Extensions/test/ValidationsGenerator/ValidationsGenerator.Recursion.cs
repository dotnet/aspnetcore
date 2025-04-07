// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http.ValidationsGenerator.Tests;

public partial class ValidationsGeneratorTests : ValidationsGeneratorTestBase
{
    [Fact]
    public async Task CanValidateRecursiveTypes()
    {
        var source = """
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder();
builder.Services.AddValidation(options =>
{
    options.MaxDepth = 8;
});

var app = builder.Build();

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

        await VerifyEndpoint(compilation, "/recursive-type", async (endpoint, serviceProvider) =>
        {
            await ThrowsExceptionForDeeplyNestedType(endpoint);
            await ValidatesTypeWithLimitedNesting(endpoint);

            async Task ThrowsExceptionForDeeplyNestedType(Endpoint endpoint)
            {
                var httpContext = CreateHttpContextWithPayload("""
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
                                                "value": 8,
                                                "next": {
                                                    "value": 9,
                                                    "next": {
                                                        "value": 10
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                """, serviceProvider);

                var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await endpoint.RequestDelegate(httpContext));
            }

            async Task ValidatesTypeWithLimitedNesting(Endpoint endpoint)
            {
                var httpContext = CreateHttpContextWithPayload("""
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
                """, serviceProvider);

                await endpoint.RequestDelegate(httpContext);

                var problemDetails = await AssertBadRequest(httpContext);
                Assert.Collection(problemDetails.Errors,
                    error =>
                    {
                        Assert.Equal("Value", error.Key);
                        Assert.Equal("The field Value must be between 10 and 100.", error.Value.Single());
                    },
                    error =>
                    {
                        Assert.Equal("Next.Value", error.Key);
                        Assert.Equal("The field Value must be between 10 and 100.", error.Value.Single());
                    },
                    error =>
                    {
                        Assert.Equal("Next.Next.Value", error.Key);
                        Assert.Equal("The field Value must be between 10 and 100.", error.Value.Single());
                    },
                    error =>
                    {
                        Assert.Equal("Next.Next.Next.Value", error.Key);
                        Assert.Equal("The field Value must be between 10 and 100.", error.Value.Single());
                    },
                    error =>
                    {
                        Assert.Equal("Next.Next.Next.Next.Value", error.Key);
                        Assert.Equal("The field Value must be between 10 and 100.", error.Value.Single());
                    },
                    error =>
                    {
                        Assert.Equal("Next.Next.Next.Next.Next.Value", error.Key);
                        Assert.Equal("The field Value must be between 10 and 100.", error.Value.Single());
                    },
                    error =>
                    {
                        Assert.Equal("Next.Next.Next.Next.Next.Next.Value", error.Key);
                        Assert.Equal("The field Value must be between 10 and 100.", error.Value.Single());
                    },
                    error =>
                    {
                        Assert.Equal("Next.Next.Next.Next.Next.Next.Next.Value", error.Key);
                        Assert.Equal("The field Value must be between 10 and 100.", error.Value.Single());
                    });
            }
        });
    }
}
