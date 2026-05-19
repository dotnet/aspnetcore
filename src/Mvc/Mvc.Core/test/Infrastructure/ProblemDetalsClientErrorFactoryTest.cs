// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Infrastructure;

public class ProblemDetailsClientErrorFactoryTest
{
    [Fact]
    public void GetClientError_ReturnsProblemDetails_IfNoMappingWasFound()
    {
        // Arrange
        var clientError = new UnsupportedMediaTypeResult();
        var problemDetailsFactory = new DefaultProblemDetailsFactory(Options.Create(new ApiBehaviorOptions
        {
            ClientErrorMapping =
                {
                    [405] = new ClientErrorData { Link = "Some link", Title = "Summary" },
                },
        }));
        var factory = new ProblemDetailsClientErrorFactory(problemDetailsFactory);

        // Act
        var result = factory.GetClientError(GetActionContext(), clientError);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(new[] { "application/problem+json", "application/problem+xml" }, objectResult.ContentTypes);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(415, problemDetails.Status);
        Assert.Null(problemDetails.Title);
        Assert.Null(problemDetails.Detail);
        Assert.Null(problemDetails.Instance);
    }

    [Fact]
    public void GetClientError_ReturnsProblemDetails()
    {
        // Arrange
        var clientError = new UnsupportedMediaTypeResult();
        var problemDetailsFactory = new DefaultProblemDetailsFactory(Options.Create(new ApiBehaviorOptions
        {
            ClientErrorMapping =
                {
                    [415] = new ClientErrorData { Link = "Some link", Title = "Summary" },
                },
        }));
        var factory = new ProblemDetailsClientErrorFactory(problemDetailsFactory);

        // Act
        var result = factory.GetClientError(GetActionContext(), clientError);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(new[] { "application/problem+json", "application/problem+xml" }, objectResult.ContentTypes);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(415, problemDetails.Status);
        Assert.Equal("Some link", problemDetails.Type);
        Assert.Equal("Summary", problemDetails.Title);
        Assert.Null(problemDetails.Detail);
        Assert.Null(problemDetails.Instance);
    }

    [Fact]
    public void GetClientError_UsesActivityId_ToSetTraceId()
    {
        // Arrange
        using (new ActivityReplacer())
        {
            var clientError = new UnsupportedMediaTypeResult();
            var problemDetailsFactory = new DefaultProblemDetailsFactory(Options.Create(new ApiBehaviorOptions
            {
                ClientErrorMapping =
                {
                    [405] = new ClientErrorData { Link = "Some link", Title = "Summary" },
                },
            }));
            var factory = new ProblemDetailsClientErrorFactory(problemDetailsFactory);

            // Act
            var result = factory.GetClientError(GetActionContext(), clientError);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(new[] { "application/problem+json", "application/problem+xml" }, objectResult.ContentTypes);
            var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);

            Assert.Equal(Activity.Current.Id, problemDetails.Extensions["traceId"]);
        }
    }

    [Fact]
    public void GetClientError_UsesHttpContext_ToSetTraceIdIfActivityIdIsNotSet()
    {
        // Arrange
        var clientError = new UnsupportedMediaTypeResult();
        var problemDetailsFactory = new DefaultProblemDetailsFactory(Options.Create(new ApiBehaviorOptions
        {
            ClientErrorMapping =
                {
                    [405] = new ClientErrorData { Link = "Some link", Title = "Summary" },
                },
        }));
        var factory = new ProblemDetailsClientErrorFactory(problemDetailsFactory);

        // Act
        var result = factory.GetClientError(GetActionContext(), clientError);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(new[] { "application/problem+json", "application/problem+xml" }, objectResult.ContentTypes);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);

        Assert.Equal("42", problemDetails.Extensions["traceId"]);
    }

    private static ActionContext GetActionContext()
    {
        return new ActionContext
        {
            HttpContext = new DefaultHttpContext
            {
                TraceIdentifier = "42",
            }
        };
    }
}
