// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class ResultsTests
{
    [Fact]
    public void Accepted_WithUrlAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var uri = "https://example.org";
        var value = new { };

        // Act
        var result = Results.Accepted(uri, value) as Accepted<object>;

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(uri, result.Location);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Accepted_WithUrl_ResultHasCorrectValues()
    {
        // Arrange
        var uri = "https://example.org";

        // Act
        var result = Results.Accepted(uri) as Accepted;

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(uri, result.Location);
    }

    [Fact]
    public void Accepted_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = Results.Accepted() as Accepted;

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Null(result.Location);
    }

    [Fact]
    public void AcceptedAtRoute_WithRouteNameAndRouteValuesAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        var routeValues = new { foo = 123 };
        var value = new { };

        // Act
        var result = Results.AcceptedAtRoute(routeName, routeValues, value) as AcceptedAtRoute<object>;

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(new RouteValueDictionary(routeValues), result.RouteValues);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void AcceptedAtRoute_WithRouteNameAndRouteValues_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        var routeValues = new { foo = 123 };

        // Act
        var result = Results.AcceptedAtRoute(routeName, routeValues) as AcceptedAtRoute;

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(new RouteValueDictionary(routeValues), result.RouteValues);
    }

    [Fact]
    public void AcceptedAtRoute_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = Results.AcceptedAtRoute() as AcceptedAtRoute;

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.NotNull(result.RouteValues);
    }

    [Fact]
    public void BadRequest_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        var value = new { };

        // Act
        var result = Results.BadRequest(value) as BadRequest<object>;

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void BadRequest_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = Results.BadRequest() as BadRequest;

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Bytes_ResultHasCorrectValues_Data))]
    public void Bytes_ResultHasCorrectValues(string contentType, string fileDownloadName, bool enableRangeProcessing, DateTimeOffset lastModified, EntityTagHeaderValue entityTag)
    {
        // Arrange
        var contents = new byte[0];

        // Act
        var result = Results.Bytes(contents, contentType, fileDownloadName, enableRangeProcessing, lastModified, entityTag) as FileContentHttpResult;

        // Assert
        Assert.Equal(contents, result.FileContents);
        Assert.Equal(contentType ?? "application/octet-stream", result.ContentType);
        Assert.Equal(fileDownloadName, result.FileDownloadName);
        Assert.Equal(enableRangeProcessing, result.EnableRangeProcessing);
        Assert.Equal(lastModified, result.LastModified);
        Assert.Equal(entityTag, result.EntityTag);
    }

    public static IEnumerable<object[]> Bytes_ResultHasCorrectValues_Data => new List<object[]>
    {
        new object[] { "text/plain", "testfile", true, new DateTimeOffset(2022, 1, 1, 0, 0, 1, TimeSpan.FromHours(-8)), EntityTagHeaderValue.Any },
        new object[] { default(string), default(string), default(bool), default(DateTimeOffset?), default(EntityTagHeaderValue) },
    };

    [Theory]
    [MemberData(nameof(Challenge_ResultHasCorrectValues_Data))]
    public void Challenge_ResultHasCorrectValues(AuthenticationProperties properties, IList<string> authenticationSchemes)
    {
        // Act
        var result = Results.Challenge(properties, authenticationSchemes) as ChallengeHttpResult;

        // Assert
        Assert.Equal(properties, result.Properties);
        Assert.Equal(authenticationSchemes ?? new ReadOnlyCollection<string>(new List<string>()), result.AuthenticationSchemes);
    }

    public static IEnumerable<object[]> Challenge_ResultHasCorrectValues_Data => new List<object[]>
    {
        new object[] { new AuthenticationProperties(), new List<string> { "TestScheme" } },
        new object[] { new AuthenticationProperties(), default(IList<string>) },
        new object[] { default(AuthenticationProperties), new List<string> { "TestScheme" } },
        new object[] { default(AuthenticationProperties), default(IList<string>) },
    };

    [Fact]
    public void Conflict_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        var value = new { };

        // Act
        var result = Results.Conflict(value) as Conflict<object>;

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Conflict_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = Results.Conflict() as Conflict;

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
    }

    [Fact]
    public void Content_WithContentAndMediaType_ResultHasCorrectValues()
    {
        // Arrange
        var content = "test content";
        var mediaType = MediaTypeHeaderValue.Parse("text/plain");

        // Act
        var result = Results.Content(content, mediaType) as ContentHttpResult;

        // Assert
        Assert.Null(result.StatusCode);
        Assert.Equal(content, result.ResponseContent);
        Assert.Equal(mediaType.ToString(), result.ContentType);
    }

    [Fact]
    public void Content_WithContentAndContentTypeAndEncoding_ResultHasCorrectValues()
    {
        // Arrange
        var content = "test content";
        var contentType = "text/plain";
        var encoding = Encoding.UTF8;

        // Act
        var result = Results.Content(content, contentType, null) as ContentHttpResult;

        // Assert
        Assert.Null(result.StatusCode);
        Assert.Equal(content, result.ResponseContent);
        Assert.Equal(contentType, result.ContentType);
    }

    [Fact]
    public void Created_WithStringUriAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var uri = "https://example.com/entity";
        var value = new { };

        // Act
        var result = Results.Created(uri, value) as Created<object>;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(uri, result.Location);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Created_WithStringUri_ResultHasCorrectValues()
    {
        // Arrange
        var uri = "https://example.com/entity";

        // Act
        var result = Results.Created(uri, null) as Created;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(uri, result.Location);
    }

    [Fact]
    public void Created_WithUriAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var uri = new Uri("https://example.com/entity");
        var value = new { };

        // Act
        var result = Results.Created(uri, value) as Created<object>;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(uri.ToString(), result.Location);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Created_WithUri_ResultHasCorrectValues()
    {
        // Arrange
        var uri = new Uri("https://example.com/entity");

        // Act
        var result = Results.Created(uri, null) as Created;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(uri.ToString(), result.Location);
    }

    [Fact]
    public void Created_WithNullStringUri_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("uri", () => Results.Created(default(string), null));
    }

    [Fact]
    public void Created_WithNullUri_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("uri", () => Results.Created(default(Uri), null));
    }

    [Theory]
    [MemberData(nameof(FactoryMethodsFromTuples))]
    public void FactoryMethod_ReturnsCorrectResultType(Expression<Func<IResult>> expression, Type expectedReturnType)
    {
        var method = expression.Compile();
        Assert.IsType(expectedReturnType, method());
    }

    [Fact]
    public void TestTheTests()
    {
        var testedMethods = new HashSet<string>(FactoryMethodsTuples.Select(t => GetMemberName(t.Item1.Body)));
        var actualMethods = new HashSet<string>(typeof(Results).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => !m.IsSpecialName)
            .Select(m => m.Name));

        // Ensure every static method on Results type is covered by at least the default case for its parameters
        Assert.All(actualMethods, name => Assert.Single(testedMethods, name));
    }

    private static string GetMemberName(Expression expression)
    {
        return expression switch
        {
            MethodCallExpression mce => mce.Method.Name,
            MemberExpression me => me.Member.Name,
            _ => throw new InvalidOperationException()
        };
    }

    private static IEnumerable<(Expression<Func<IResult>>, Type)> FactoryMethodsTuples { get; } = new List<(Expression<Func<IResult>>, Type)>
    {
        (() => Results.Accepted(null, null), typeof(Accepted)),
        (() => Results.Accepted(null, new()), typeof(Accepted<object>)),
        (() => Results.AcceptedAtRoute("routeName", null, null), typeof(AcceptedAtRoute)),
        (() => Results.AcceptedAtRoute("routeName", null, new()), typeof(AcceptedAtRoute<object>)),
        (() => Results.BadRequest(null), typeof(BadRequest)),
        (() => Results.BadRequest(new()), typeof(BadRequest<object>)),
        (() => Results.Bytes(new byte[0], null, null, false, null, null), typeof(FileContentHttpResult)),
        (() => Results.Challenge(null, null), typeof(ChallengeHttpResult)),
        (() => Results.Conflict(null), typeof(Conflict)),
        (() => Results.Conflict(new()), typeof(Conflict<object>)),
        (() => Results.Content("content", null, null), typeof(ContentHttpResult)),
        (() => Results.Created("/path", null), typeof(Created)),
        (() => Results.Created("/path", new()), typeof(Created<object>)),
        (() => Results.CreatedAtRoute("routeName", null, null), typeof(CreatedAtRoute)),
        (() => Results.CreatedAtRoute("routeName", null, new()), typeof(CreatedAtRoute<object>)),
        (() => Results.Empty, typeof(EmptyHttpResult)),
        (() => Results.File(new byte[0], null, null, false, null, null), typeof(FileContentHttpResult)),
        (() => Results.File(new MemoryStream(), null, null, null, null, false), typeof(FileStreamHttpResult)),
        (() => Results.File("C:\\path", null, null, null, null, false), typeof(PhysicalFileHttpResult)),
        (() => Results.File("path", null, null, null, null, false), typeof(VirtualFileHttpResult)),
        (() => Results.Forbid(null, null), typeof(ForbidHttpResult)),
        (() => Results.Json(new(), null, null, null), typeof(JsonHttpResult<object>)),
        (() => Results.NoContent(), typeof(NoContent)),
        (() => Results.NotFound(null), typeof(NotFound)),
        (() => Results.NotFound(new()), typeof(NotFound<object>)),
        (() => Results.Ok(null), typeof(Ok)),
        (() => Results.Ok(new()), typeof(Ok<object>)),
        (() => Results.Problem(new()), typeof(ProblemHttpResult)),
        (() => Results.Stream(new MemoryStream(), null, null, null, null, false), typeof(FileStreamHttpResult)),
        (() => Results.Stream(s => Task.CompletedTask, null, null, null, null), typeof(PushStreamHttpResult)),
        (() => Results.Text("content", null, null), typeof(ContentHttpResult)),
        (() => Results.Redirect("/path", false, false), typeof(RedirectHttpResult)),
        (() => Results.LocalRedirect("/path", false, false), typeof(RedirectHttpResult)),
        (() => Results.RedirectToRoute("routeName", null, false, false, null), typeof(RedirectToRouteHttpResult)),
        (() => Results.SignIn(new(), null, null), typeof(SignInHttpResult)),
        (() => Results.SignOut(new(), null), typeof(SignOutHttpResult)),
        (() => Results.StatusCode(200), typeof(StatusCodeHttpResult)),
        (() => Results.Unauthorized(), typeof(UnauthorizedHttpResult)),
        (() => Results.UnprocessableEntity(null), typeof(UnprocessableEntity)),
        (() => Results.UnprocessableEntity(new()), typeof(UnprocessableEntity<object>)),
        (() => Results.ValidationProblem(new Dictionary<string, string[]>(), null, null, null, null, null, null), typeof(ProblemHttpResult))
    };

    public static IEnumerable<object[]> FactoryMethodsFromTuples() => FactoryMethodsTuples.Select(t => new object[] { t.Item1, t.Item2 });
}
