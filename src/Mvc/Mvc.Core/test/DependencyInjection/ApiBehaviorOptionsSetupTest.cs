// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace Microsoft.Extensions.DependencyInjection;

public class ApiBehaviorOptionsSetupTest
{
    [Fact]
    public void Configure_AddsClientErrorMappings()
    {
        // Arrange
        var expected = new[] { 400, 401, 403, 404, 405, 406, 407, 408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 421, 422, 426, 500, 501, 502, 503, 504, 505 };
        var optionsSetup = new ApiBehaviorOptionsSetup();
        var options = new ApiBehaviorOptions();

        // Act
        optionsSetup.Configure(options);

        // Assert
        Assert.Equal(expected, options.ClientErrorMapping.Keys);
    }

    [Fact]
    public void ProblemDetailsInvalidModelStateResponse_ReturnsBadRequestWithProblemDetails()
    {
        // Arrange
        var actionContext = GetActionContext();
        var factory = GetProblemDetailsFactory();

        // Act
        var result = ApiBehaviorOptionsSetup.ProblemDetailsInvalidModelStateResponse(factory, actionContext);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(new[] { "application/problem+json", "application/problem+xml" }, badRequest.ContentTypes.OrderBy(c => c));

        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal(400, problemDetails.Status);
        Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.5.1", problemDetails.Type);
    }

    [Fact]
    public void ProblemDetailsInvalidModelStateResponse_UsesUserConfiguredLink()
    {
        // Arrange
        var link = "http://mylink";
        var actionContext = GetActionContext();

        var factory = GetProblemDetailsFactory(options => options.ClientErrorMapping[400].Link = link);

        // Act
        var result = ApiBehaviorOptionsSetup.ProblemDetailsInvalidModelStateResponse(factory, actionContext);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(new[] { "application/problem+json", "application/problem+xml" }, badRequest.ContentTypes.OrderBy(c => c));

        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal(400, problemDetails.Status);
        Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
        Assert.Equal(link, problemDetails.Type);
    }

    [Fact]
    public void ProblemDetailsInvalidModelStateResponse_UsesProblemDetailsFactory()
    {
        // Arrange
        var actionContext = GetActionContext();
        var factory = Mock.Of<ProblemDetailsFactory>(m => m.CreateValidationProblemDetails(It.IsAny<HttpContext>(), It.IsAny<ModelStateDictionary>(), null, null, null, null, null) == new ValidationProblemDetails
        {
            Status = 422,
        });

        // Act
        var result = ApiBehaviorOptionsSetup.ProblemDetailsInvalidModelStateResponse(factory, actionContext);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(422, objectResult.StatusCode);
        Assert.Equal(new[] { "application/problem+json", "application/problem+xml" }, objectResult.ContentTypes.OrderBy(c => c));

        var problemDetails = Assert.IsType<ValidationProblemDetails>(objectResult.Value);
        Assert.Equal(422, problemDetails.Status);
        Assert.Equal("One or more validation errors occurred.", problemDetails.Title);
    }

    [Fact]
    public void ProblemDetailsInvalidModelStateResponse_SetsTraceId()
    {
        // Arrange
        using (new ActivityReplacer())
        {
            var actionContext = GetActionContext();
            var factory = GetProblemDetailsFactory();

            // Act
            var result = ApiBehaviorOptionsSetup.ProblemDetailsInvalidModelStateResponse(factory, actionContext);

            // Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
            Assert.Equal(Activity.Current.Id, problemDetails.Extensions["traceId"]);
        }
    }

    [Fact]
    public void ProblemDetailsInvalidModelStateResponse_SetsTraceIdFromRequest_IfActivityIsNull()
    {
        // Arrange
        var actionContext = GetActionContext();
        var factory = GetProblemDetailsFactory();

        // Act
        var result = ApiBehaviorOptionsSetup.ProblemDetailsInvalidModelStateResponse(factory, actionContext);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var problemDetails = Assert.IsType<ValidationProblemDetails>(badRequest.Value);
        Assert.Equal("42", problemDetails.Extensions["traceId"]);
    }

    private static ProblemDetailsFactory GetProblemDetailsFactory(Action<ApiBehaviorOptions> configure = null)
    {
        var options = new ApiBehaviorOptions();
        var setup = new ApiBehaviorOptionsSetup();

        setup.Configure(options);
        if (configure != null)
        {
            configure(options);
        }

        return new DefaultProblemDetailsFactory(Options.Options.Create(options));
    }

    private static ActionContext GetActionContext()
    {
        return new ActionContext
        {
            HttpContext = new DefaultHttpContext { TraceIdentifier = "42" },
        };
    }
}
