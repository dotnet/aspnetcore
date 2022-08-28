// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

public class ProblemDetailsFactoryTest
{
    private readonly ProblemDetailsFactory Factory = GetProblemDetails();

    [Fact]
    public void CreateProblemDetails_DefaultValues()
    {
        // Act
        var problemDetails = Factory.CreateProblemDetails(GetHttpContext());

        // Assert
        Assert.Equal(500, problemDetails.Status);
        Assert.Equal("An error occurred while processing your request.", problemDetails.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.6.1", problemDetails.Type);
        Assert.Null(problemDetails.Instance);
        Assert.Null(problemDetails.Detail);
        Assert.Collection(
            problemDetails.Extensions,
            kvp =>
            {
                Assert.Equal("traceId", kvp.Key);
                Assert.Equal("some-trace", kvp.Value);
            });
    }

    [Fact]
    public void CreateProblemDetails_WithStatusCode()
    {
        // Act
        var problemDetails = Factory.CreateProblemDetails(GetHttpContext(), statusCode: 406);

        // Assert
        Assert.Equal(406, problemDetails.Status);
        Assert.Equal("Not Acceptable", problemDetails.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.7", problemDetails.Type);
        Assert.Null(problemDetails.Instance);
        Assert.Null(problemDetails.Detail);
        Assert.Collection(
            problemDetails.Extensions,
            kvp =>
            {
                Assert.Equal("traceId", kvp.Key);
                Assert.Equal("some-trace", kvp.Value);
            });
    }

    [Fact]
    public void CreateProblemDetails_WithDetailAndTitle()
    {
        // Act
        var title = "Some title";
        var detail = "some detail";
        var problemDetails = Factory.CreateProblemDetails(GetHttpContext(), statusCode: 406, title: title, detail: detail);

        // Assert
        Assert.Equal(406, problemDetails.Status);
        Assert.Equal(title, problemDetails.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.7", problemDetails.Type);
        Assert.Null(problemDetails.Instance);
        Assert.Equal(detail, problemDetails.Detail);
        Assert.Collection(
            problemDetails.Extensions,
            kvp =>
            {
                Assert.Equal("traceId", kvp.Key);
                Assert.Equal("some-trace", kvp.Value);
            });
    }

    [Fact]
    public void CreateValidationProblemDetails_DefaultValues()
    {
        // Act
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("some-key", "some-value");
        var problemDetails = Factory.CreateValidationProblemDetails(GetHttpContext(), modelState);

        // Assert
        Assert.Equal(400, problemDetails.Status);
        Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", problemDetails.Type);
        Assert.Null(problemDetails.Instance);
        Assert.Null(problemDetails.Detail);
        Assert.Collection(
            problemDetails.Extensions,
            kvp =>
            {
                Assert.Equal("traceId", kvp.Key);
                Assert.Equal("some-trace", kvp.Value);
            });
        Assert.Collection(
           problemDetails.Errors,
           kvp =>
           {
               Assert.Equal("some-key", kvp.Key);
               Assert.Equal(new[] { "some-value" }, kvp.Value);
           });
    }

    [Fact]
    public void CreateValidationProblemDetails_WithStatusCode()
    {
        // Act
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("some-key", "some-value");
        var problemDetails = Factory.CreateValidationProblemDetails(GetHttpContext(), modelState, 422);

        // Assert
        Assert.Equal(422, problemDetails.Status);
        Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc4918#section-11.2", problemDetails.Type);
        Assert.Null(problemDetails.Instance);
        Assert.Null(problemDetails.Detail);
        Assert.Collection(
            problemDetails.Extensions,
            kvp =>
            {
                Assert.Equal("traceId", kvp.Key);
                Assert.Equal("some-trace", kvp.Value);
            });
        Assert.Collection(
           problemDetails.Errors,
           kvp =>
           {
               Assert.Equal("some-key", kvp.Key);
               Assert.Equal(new[] { "some-value" }, kvp.Value);
           });
    }

    [Fact]
    public void CreateValidationProblemDetails_WithTitleAndInstance()
    {
        // Act
        var title = "Some title";
        var instance = "some instance";
        var modelState = new ModelStateDictionary();
        modelState.AddModelError("some-key", "some-value");
        var problemDetails = Factory.CreateValidationProblemDetails(GetHttpContext(), modelState, title: title, instance: instance);

        // Assert
        Assert.Equal(400, problemDetails.Status);
        Assert.Equal(title, problemDetails.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", problemDetails.Type);
        Assert.Equal(instance, problemDetails.Instance);
        Assert.Null(problemDetails.Detail);
        Assert.Collection(
            problemDetails.Extensions,
            kvp =>
            {
                Assert.Equal("traceId", kvp.Key);
                Assert.Equal("some-trace", kvp.Value);
            });
        Assert.Collection(
           problemDetails.Errors,
           kvp =>
           {
               Assert.Equal("some-key", kvp.Key);
               Assert.Equal(new[] { "some-value" }, kvp.Value);
           });
    }

    private static DefaultHttpContext GetHttpContext()
    {
        return new DefaultHttpContext
        {
            TraceIdentifier = "some-trace",
        };
    }

    private static ProblemDetailsFactory GetProblemDetails()
    {
        var options = new ApiBehaviorOptions();
        new ApiBehaviorOptionsSetup().Configure(options);
        return new DefaultProblemDetailsFactory(Options.Create(options));
    }
}
