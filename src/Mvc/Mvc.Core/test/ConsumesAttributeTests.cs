// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;
using Moq;

namespace Microsoft.AspNetCore.Mvc;

public class ConsumesAttributeTests
{
    [Theory]
    [InlineData("application")]
    [InlineData("")]
    public void Constructor_ForInvalidContentType_Throws(string contentType)
    {
        // Arrange
        var expectedMessage = $"The header contains invalid values at index 0: '{contentType}'";

        // Act & Assert
        var exception = Assert.Throws<FormatException>(() => new ConsumesAttribute(contentType));
        Assert.Equal(expectedMessage, exception.Message);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("application/xml,, application/json", "")]
    [InlineData(", application/json", "")]
    [InlineData("invalid", "invalid")]
    [InlineData("application/xml,invalid, application/json", "invalid")]
    [InlineData("invalid, application/json", "invalid")]
    public void Constructor_UnparsableContentType_Throws(string content, string invalidContentType)
    {
        // Act
        var contentTypes = content.Split(',').Select(contentType => contentType.Trim()).ToArray();

        // Assert
        var ex = Assert.Throws<FormatException>(
                   () => new ConsumesAttribute(contentTypes[0], contentTypes.Skip(1).ToArray()));
        Assert.Equal("The header contains invalid values at index 0: '" + (invalidContentType ?? "<null>") + "'",
                     ex.Message);
    }

    [Theory]
    [InlineData("application/*", "application/*")]
    [InlineData("application/xml, application/*, application/json", "application/*")]
    [InlineData("application/*, application/json", "application/*")]

    [InlineData("*/*", "*/*")]
    [InlineData("application/xml, */*, application/json", "*/*")]
    [InlineData("*/*, application/json", "*/*")]
    public void Constructor_InvalidContentType_Throws(string content, string invalidContentType)
    {
        // Act
        var contentTypes = content.Split(',').Select(contentType => contentType.Trim()).ToArray();

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(
                   () => new ConsumesAttribute(contentTypes[0], contentTypes.Skip(1).ToArray()));

        Assert.Equal(
            string.Format(
                CultureInfo.CurrentCulture,
                "The argument '{0}' is invalid. " +
                "Media types which match all types or match all subtypes are not supported.",
                invalidContentType),
            ex.Message);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/json;Parameter1=12")]
    [InlineData("text/xml")]
    public void ActionConstraint_Accept_MatchesForMatchingRequestContentType(string contentType)
    {
        // Arrange
        var constraint = new ConsumesAttribute("application/json", "text/xml");
        var action = new ActionDescriptor()
        {
            FilterDescriptors =
                new List<FilterDescriptor>() { new FilterDescriptor(constraint, FilterScope.Action) }
        };

        var context = new ActionConstraintContext();
        context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(action, new [] { constraint }),
            };

        context.CurrentCandidate = context.Candidates[0];
        context.RouteContext = CreateRouteContext(contentType: contentType);

        // Act & Assert
        Assert.True(constraint.Accept(context));
    }

    [Fact]
    public void ActionConstraint_Accept_TheFirstCandidateReturnsFalse_IfALaterOneMatches()
    {
        // Arrange
        var constraint1 = new ConsumesAttribute("application/json", "text/xml");
        var action1 = new ActionDescriptor()
        {
            FilterDescriptors =
                new List<FilterDescriptor>() { new FilterDescriptor(constraint1, FilterScope.Action) }
        };

        var constraint2 = new Mock<ITestActionConsumeConstraint>();
        var action2 = new ActionDescriptor()
        {
            FilterDescriptors =
                new List<FilterDescriptor>() { new FilterDescriptor(constraint2.Object, FilterScope.Action) }
        };

        constraint2.Setup(o => o.Accept(It.IsAny<ActionConstraintContext>()))
                   .Returns(true);

        var context = new ActionConstraintContext();
        context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(action1, new [] { constraint1 }),
                new ActionSelectorCandidate(action2, new [] { constraint2.Object }),
            };

        context.CurrentCandidate = context.Candidates[0];
        context.RouteContext = CreateRouteContext(contentType: "application/custom");

        // Act & Assert
        Assert.False(constraint1.Accept(context));
    }

    [Theory]
    [InlineData("application/custom")]
    [InlineData("")]
    [InlineData(null)]
    public void ActionConstraint_Accept_ForNoMatchingCandidates_SelectsTheFirstCandidate(string contentType)
    {
        // Arrange
        var constraint1 = new ConsumesAttribute("application/json", "text/xml");
        var action1 = new ActionDescriptor()
        {
            FilterDescriptors =
                new List<FilterDescriptor>() { new FilterDescriptor(constraint1, FilterScope.Action) }
        };

        var constraint2 = new Mock<ITestActionConsumeConstraint>();
        var action2 = new ActionDescriptor()
        {
            FilterDescriptors =
                new List<FilterDescriptor>() { new FilterDescriptor(constraint2.Object, FilterScope.Action) }
        };

        constraint2.Setup(o => o.Accept(It.IsAny<ActionConstraintContext>()))
                   .Returns(false);

        var context = new ActionConstraintContext();
        context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(action1, new [] { constraint1 }),
                new ActionSelectorCandidate(action2, new [] { constraint2.Object }),
            };

        context.CurrentCandidate = context.Candidates[0];
        context.RouteContext = CreateRouteContext(contentType: contentType);

        // Act & Assert
        Assert.True(constraint1.Accept(context));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ActionConstraint_Accept_ForNoRequestType_SelectsTheCandidateWithoutConstraintIfPresent(string contentType)
    {
        // Arrange
        var constraint1 = new ConsumesAttribute("application/json");
        var actionWithConstraint = new ActionDescriptor()
        {
            FilterDescriptors =
                new List<FilterDescriptor>() { new FilterDescriptor(constraint1, FilterScope.Action) }
        };

        var constraint2 = new ConsumesAttribute("text/xml");
        var actionWithConstraint2 = new ActionDescriptor()
        {
            FilterDescriptors =
                new List<FilterDescriptor>() { new FilterDescriptor(constraint2, FilterScope.Action) }
        };

        var actionWithoutConstraint = new ActionDescriptor();

        var context = new ActionConstraintContext();
        context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(actionWithConstraint, new [] { constraint1 }),
                new ActionSelectorCandidate(actionWithConstraint2, new [] { constraint2 }),
                new ActionSelectorCandidate(actionWithoutConstraint, new List<IActionConstraint>()),
            };

        context.RouteContext = CreateRouteContext(contentType: contentType);

        // Act & Assert
        context.CurrentCandidate = context.Candidates[0];
        Assert.False(constraint1.Accept(context));
        context.CurrentCandidate = context.Candidates[1];
        Assert.False(constraint2.Accept(context));
    }

    [Theory]
    [InlineData("application/xml")]
    [InlineData("application/custom")]
    [InlineData("invalid/invalid")]
    public void ActionConstraint_Accept_UnrecognizedMediaType_SelectsTheCandidateWithoutConstraintIfPresent(string contentType)
    {
        // Arrange
        var actionWithoutConstraint = new ActionDescriptor();
        var constraint1 = new ConsumesAttribute("application/json");
        var actionWithConstraint = new ActionDescriptor()
        {
            FilterDescriptors =
                new List<FilterDescriptor>() { new FilterDescriptor(constraint1, FilterScope.Action) }
        };

        var constraint2 = new ConsumesAttribute("text/xml");
        var actionWithConstraint2 = new ActionDescriptor()
        {
            FilterDescriptors =
                new List<FilterDescriptor>() { new FilterDescriptor(constraint2, FilterScope.Action) }
        };

        var context = new ActionConstraintContext();
        context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(actionWithConstraint, new [] { constraint1 }),
                new ActionSelectorCandidate(actionWithConstraint2, new [] { constraint2 }),
                new ActionSelectorCandidate(actionWithoutConstraint, new List<IActionConstraint>()),
            };

        context.RouteContext = CreateRouteContext(contentType: contentType);

        // Act & Assert
        context.CurrentCandidate = context.Candidates[0];
        Assert.False(constraint1.Accept(context));

        context.CurrentCandidate = context.Candidates[1];
        Assert.False(constraint2.Accept(context));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ActionConstraint_Accept_ForNoRequestType_ReturnsTrueForAllConstraints(string contentType)
    {
        // Arrange
        var constraint1 = new ConsumesAttribute("application/json");
        var actionWithConstraint = new ActionDescriptor()
        {
            FilterDescriptors =
                new List<FilterDescriptor>() { new FilterDescriptor(constraint1, FilterScope.Action) }
        };

        var constraint2 = new ConsumesAttribute("text/xml");
        var actionWithConstraint2 = new ActionDescriptor()
        {
            FilterDescriptors =
                new List<FilterDescriptor>() { new FilterDescriptor(constraint2, FilterScope.Action) }
        };

        var actionWithoutConstraint = new ActionDescriptor();

        var context = new ActionConstraintContext();
        context.Candidates = new List<ActionSelectorCandidate>()
            {
                new ActionSelectorCandidate(actionWithConstraint, new [] { constraint1 }),
                new ActionSelectorCandidate(actionWithConstraint2, new [] { constraint2 }),
            };

        context.RouteContext = CreateRouteContext(contentType: contentType);

        // Act & Assert
        context.CurrentCandidate = context.Candidates[0];
        Assert.True(constraint1.Accept(context));
        context.CurrentCandidate = context.Candidates[1];
        Assert.True(constraint2.Accept(context));
    }

    [Theory]
    [InlineData("application/xml")]
    [InlineData("application/custom")]
    public void OnResourceExecuting_ForNoContentTypeMatch_SetsUnsupportedMediaTypeResult(string contentType)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = contentType;
        var consumesFilter = new ConsumesAttribute("application/json");
        var actionWithConstraint = new ActionDescriptor()
        {
            ActionConstraints = new List<IActionConstraintMetadata>() { consumesFilter },
            FilterDescriptors =
                new List<FilterDescriptor>() { new FilterDescriptor(consumesFilter, FilterScope.Action) }
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionWithConstraint);

        var resourceExecutingContext = new ResourceExecutingContext(
            actionContext,
            new[] { consumesFilter },
            new List<IValueProviderFactory>());

        // Act
        consumesFilter.OnResourceExecuting(resourceExecutingContext);

        // Assert
        Assert.NotNull(resourceExecutingContext.Result);
        Assert.IsType<UnsupportedMediaTypeResult>(resourceExecutingContext.Result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void OnResourceExecuting_NullOrEmptyRequestContentType_IsNoOp(string contentType)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = contentType;
        var consumesFilter = new ConsumesAttribute("application/json");
        var actionWithConstraint = new ActionDescriptor()
        {
            ActionConstraints = new List<IActionConstraintMetadata>() { consumesFilter },
            FilterDescriptors =
                new List<FilterDescriptor>() { new FilterDescriptor(consumesFilter, FilterScope.Action) }
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionWithConstraint);

        var resourceExecutingContext = new ResourceExecutingContext(
            actionContext,
            new[] { consumesFilter },
            new List<IValueProviderFactory>());

        // Act
        consumesFilter.OnResourceExecuting(resourceExecutingContext);

        // Assert
        Assert.Null(resourceExecutingContext.Result);
    }

    [Theory]
    [InlineData("application/xml")]
    [InlineData("application/json")]
    public void OnResourceExecuting_ForAContentTypeMatch_IsNoOp(string contentType)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = contentType;
        var consumesFilter = new ConsumesAttribute("application/json", "application/xml");
        var actionWithConstraint = new ActionDescriptor()
        {
            ActionConstraints = new List<IActionConstraintMetadata>() { consumesFilter },
            FilterDescriptors =
                new List<FilterDescriptor>() { new FilterDescriptor(consumesFilter, FilterScope.Action) }
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), actionWithConstraint);
        var resourceExecutingContext = new ResourceExecutingContext(
            actionContext,
            new[] { consumesFilter },
            new List<IValueProviderFactory>());

        // Act
        consumesFilter.OnResourceExecuting(resourceExecutingContext);

        // Assert
        Assert.Null(resourceExecutingContext.Result);
    }

    [Fact]
    public void SetContentTypes_ClearsAndSetsContentTypes()
    {
        // Arrange
        var attribute = new ConsumesAttribute("application/json", "text/json");

        var contentTypes = new MediaTypeCollection()
            {
                MediaTypeHeaderValue.Parse("application/xml"),
                MediaTypeHeaderValue.Parse("text/xml"),
            };

        // Act
        attribute.SetContentTypes(contentTypes);

        // Assert
        Assert.Collection(
            contentTypes.OrderBy(t => t),
            t => Assert.Equal("application/json", t),
            t => Assert.Equal("text/json", t));
    }

    private static RouteContext CreateRouteContext(string contentType = null, object routeValues = null)
    {
        var httpContext = CreateHttpContext(contentType);

        var routeContext = new RouteContext(httpContext);
        routeContext.RouteData = new RouteData();

        foreach (var kvp in new RouteValueDictionary(routeValues))
        {
            routeContext.RouteData.Values.Add(kvp.Key, kvp.Value);
        }

        return routeContext;
    }

    private static HttpContext CreateHttpContext(string contentType = null, object routeValues = null)
    {
        var httpContext = new DefaultHttpContext();
        if (contentType != null)
        {
            httpContext.Request.ContentType = contentType;
        }

        return httpContext;
    }

    internal interface ITestActionConsumeConstraint : IConsumesActionConstraint, IResourceFilter
    {
    }
}
