// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.IO.Pipelines;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http.HttpResults;

public partial class ResultsTests
{
    [Fact]
    public void Accepted_WithUrlAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var uri = "https://example.org";
        object value = new { };

        // Act
        var result = Results.Accepted(uri, value) as Accepted<object>;

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(uri, result.Location);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void AcceptedOfT_WithUrlAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var uri = "https://example.org";
        var value = new Todo(1);

        // Act
        var result = Results.Accepted(uri, value) as Accepted<Todo>;

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
        object value = new { };

        // Act
        var result = Results.AcceptedAtRoute(routeName, routeValues, value) as AcceptedAtRoute<object>;

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(new RouteValueDictionary(routeValues), result.RouteValues);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void AcceptedAtRoute_WithRouteNameAndRouteValueDictionaryAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        var routeValues = new RouteValueDictionary { ["foo"] = 123 };
        object value = new { };

        // Act
        var result = Results.AcceptedAtRoute(routeName, routeValues, value) as AcceptedAtRoute<object>;

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(routeValues, result.RouteValues);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void AcceptedAtRoute_WithNullRouteNameAndRouteValues_ResultHasCorrectValues()
    {
        // Arrange

        // Act
        var result = Results.AcceptedAtRoute(null, null) as AcceptedAtRoute;

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
    }

    [Fact]
    public void AcceptedAtRouteOfT_WithRouteNameAndRouteValuesAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        var routeValues = new { foo = 123 };
        var value = new Todo(1);

        // Act
        var result = Results.AcceptedAtRoute(routeName, routeValues, value) as AcceptedAtRoute<Todo>;

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(new RouteValueDictionary(routeValues), result.RouteValues);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void AcceptedAtRouteOfT_WithRouteNameAndRouteValueDictionaryAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        var routeValues = new RouteValueDictionary { ["foo"] = 123 };
        var value = new Todo(1);

        // Act
        var result = Results.AcceptedAtRoute(routeName, routeValues, value) as AcceptedAtRoute<Todo>;

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(routeValues, result.RouteValues);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void AcceptedAtRouteOfT_WithNullRouteNameAndRouteValueDictionaryAndValue_ResultHasCorrectValues()
    {
        // Arrange

        // Act
        var result = Results.AcceptedAtRoute<object>(null, null, null) as AcceptedAtRoute;

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
    }

    [Fact]
    public void AcceptedAtRouteOfT_WithNullRouteNameAndRouteValuesAndValue_ResultHasCorrectValues()
    {
        // Arrange

        // Act
        var result = Results.AcceptedAtRoute(null, null, null) as AcceptedAtRoute;

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
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
        object value = new { };

        // Act
        var result = Results.BadRequest(value) as BadRequest<object>;

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void BadRequestOfT_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        var value = new Todo(1);

        // Act
        var result = Results.BadRequest(value) as BadRequest<Todo>;

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
    [MemberData(nameof(BytesOrFile_ResultHasCorrectValues_Data))]
    public void BytesOrFile_ResultHasCorrectValues(int bytesOrFile, string contentType, string fileDownloadName, bool enableRangeProcessing, DateTimeOffset lastModified, EntityTagHeaderValue entityTag)
    {
        // Arrange
        var contents = new byte[0];

        // Act
        var result = bytesOrFile switch
        {
            0 => Results.Bytes(contents, contentType, fileDownloadName, enableRangeProcessing, lastModified, entityTag),
            _ => Results.File(contents, contentType, fileDownloadName, enableRangeProcessing, lastModified, entityTag)
        } as FileContentHttpResult;

        // Assert
        Assert.Equal(contents, result.FileContents);
        Assert.Equal(contentType ?? "application/octet-stream", result.ContentType);
        Assert.Equal(fileDownloadName, result.FileDownloadName);
        Assert.Equal(enableRangeProcessing, result.EnableRangeProcessing);
        Assert.Equal(lastModified, result.LastModified);
        Assert.Equal(entityTag, result.EntityTag);
    }

    public static IEnumerable<object[]> BytesOrFile_ResultHasCorrectValues_Data => new List<object[]>
    {
        new object[] { 0, "text/plain", "testfile", true, new DateTimeOffset(2022, 1, 1, 0, 0, 1, TimeSpan.FromHours(-8)), EntityTagHeaderValue.Any },
        new object[] { 0, default(string), default(string), default(bool), default(DateTimeOffset?), default(EntityTagHeaderValue) },
        new object[] { 1, "text/plain", "testfile", true, new DateTimeOffset(2022, 1, 1, 0, 0, 1, TimeSpan.FromHours(-8)), EntityTagHeaderValue.Any },
        new object[] { 1, default(string), default(string), default(bool), default(DateTimeOffset?), default(EntityTagHeaderValue) }
    };

    [Theory]
    [MemberData(nameof(Stream_ResultHasCorrectValues_Data))]
    public void Stream_ResultHasCorrectValues(int overload, string contentType, string fileDownloadName, bool enableRangeProcessing, DateTimeOffset lastModified, EntityTagHeaderValue entityTag)
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        var result = overload switch
        {
            0 => Results.Stream(stream, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing),
            1 => Results.Stream(PipeReader.Create(stream), contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing),
            _ => Results.Stream((s) => Task.CompletedTask, contentType, fileDownloadName, lastModified, entityTag)
        };

        // Assert
        switch (overload)
        {
            case <= 1:
                var fileStreamResult = result as FileStreamHttpResult;
                Assert.NotNull(fileStreamResult.FileStream);
                Assert.Equal(contentType ?? "application/octet-stream", fileStreamResult.ContentType);
                Assert.Equal(fileDownloadName, fileStreamResult.FileDownloadName);
                Assert.Equal(enableRangeProcessing, fileStreamResult.EnableRangeProcessing);
                Assert.Equal(lastModified, fileStreamResult.LastModified);
                Assert.Equal(entityTag, fileStreamResult.EntityTag);
                break;

            default:
                var pushStreamResult = result as PushStreamHttpResult;
                Assert.Equal(contentType ?? "application/octet-stream", pushStreamResult.ContentType);
                Assert.Equal(fileDownloadName, pushStreamResult.FileDownloadName);
                Assert.False(pushStreamResult.EnableRangeProcessing);
                Assert.Equal(lastModified, pushStreamResult.LastModified);
                Assert.Equal(entityTag, pushStreamResult.EntityTag);
                break;
        }

    }

    public static IEnumerable<object[]> Stream_ResultHasCorrectValues_Data => new List<object[]>
    {
        new object[] { 0, "text/plain", "testfile", true, new DateTimeOffset(2022, 1, 1, 0, 0, 1, TimeSpan.FromHours(-8)), EntityTagHeaderValue.Any },
        new object[] { 0, default(string), default(string), default(bool), default(DateTimeOffset?), default(EntityTagHeaderValue) },
        new object[] { 1, "text/plain", "testfile", true, new DateTimeOffset(2022, 1, 1, 0, 0, 1, TimeSpan.FromHours(-8)), EntityTagHeaderValue.Any },
        new object[] { 1, default(string), default(string), default(bool), default(DateTimeOffset?), default(EntityTagHeaderValue) },
        new object[] { 2, "text/plain", "testfile", true, new DateTimeOffset(2022, 1, 1, 0, 0, 1, TimeSpan.FromHours(-8)), EntityTagHeaderValue.Any },
        new object[] { 2, default(string), default(string), default(bool), default(DateTimeOffset?), default(EntityTagHeaderValue) }
    };

    [Fact]
    public void Bytes_WithNullContents_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("contents", () => Results.Bytes(null));
    }

    [Fact]
    public void File_WithNullContents_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("fileContents", () => Results.File(default(byte[])));
    }

    [Fact]
    public void File_WithNullStream_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("fileStream", () => Results.File(default(Stream)));
    }

    [Fact]
    public void Stream_WithNullStream_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("stream", () => Results.Stream(default(Stream)));
    }

    [Fact]
    public void Stream_WithNullPipeReader_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("pipeReader", () => Results.Stream(default(PipeReader)));
    }

    [Fact]
    public void Stream_WithNullCallback_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("streamWriterCallback", () => TypedResults.Stream(default(Func<Stream, Task>)));
    }

    [Theory]
    [MemberData(nameof(ChallengeForbidSignInOut_ResultHasCorrectValues_Data))]
    public void Challenge_ResultHasCorrectValues(AuthenticationProperties properties, IList<string> authenticationSchemes)
    {
        // Act
        var result = Results.Challenge(properties, authenticationSchemes) as ChallengeHttpResult;

        // Assert
        Assert.Equal(properties, result.Properties);
        Assert.Equal(authenticationSchemes ?? new ReadOnlyCollection<string>(new List<string>()), result.AuthenticationSchemes);
    }

    [Theory]
    [MemberData(nameof(ChallengeForbidSignInOut_ResultHasCorrectValues_Data))]
    public void Forbid_ResultHasCorrectValues(AuthenticationProperties properties, IList<string> authenticationSchemes)
    {
        // Act
        var result = Results.Forbid(properties, authenticationSchemes) as ForbidHttpResult;

        // Assert
        Assert.Equal(properties, result.Properties);
        Assert.Equal(authenticationSchemes ?? new ReadOnlyCollection<string>(new List<string>()), result.AuthenticationSchemes);
    }

    [Fact]
    public void InternalServerError_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        object value = new { };

        // Act
        var result = Results.InternalServerError(value) as InternalServerError<object>;

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void InternalServerErrorOfT_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        var value = new Todo(1);

        // Act
        var result = Results.InternalServerError(value) as InternalServerError<Todo>;

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void InternalServerError_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = Results.InternalServerError() as InternalServerError;

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ChallengeForbidSignInOut_ResultHasCorrectValues_Data))]
    public void SignOut_ResultHasCorrectValues(AuthenticationProperties properties, IList<string> authenticationSchemes)
    {
        // Act
        var result = Results.SignOut(properties, authenticationSchemes) as SignOutHttpResult;

        // Assert
        Assert.Equal(properties, result.Properties);
        Assert.Equal(authenticationSchemes ?? new ReadOnlyCollection<string>(new List<string>()), result.AuthenticationSchemes);
    }

    [Theory]
    [MemberData(nameof(ChallengeForbidSignInOut_ResultHasCorrectValues_Data))]
    public void SignIn_ResultHasCorrectValues(AuthenticationProperties properties, IList<string> authenticationSchemes)
    {
        // Arrange
        var principal = new ClaimsPrincipal();

        // Act
        var result = Results.SignIn(principal, properties, authenticationSchemes?.First()) as SignInHttpResult;

        // Assert
        Assert.Equal(principal, result.Principal);
        Assert.Equal(properties, result.Properties);
        Assert.Equal(authenticationSchemes?.First(), result.AuthenticationScheme);
    }

    public static IEnumerable<object[]> ChallengeForbidSignInOut_ResultHasCorrectValues_Data => new List<object[]>
    {
        new object[] { new AuthenticationProperties(), new List<string> { "TestScheme" } },
        new object[] { new AuthenticationProperties(), default(IList<string>) },
        new object[] { default(AuthenticationProperties), new List<string> { "TestScheme" } },
        new object[] { default(AuthenticationProperties), default(IList<string>) },
    };

    [Fact]
    public void SignIn_WithNullPrincipal_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("principal", () => Results.SignIn(null));
    }

    [Fact]
    public void Conflict_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        object value = new { };

        // Act
        var result = Results.Conflict(value) as Conflict<object>;

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void ConflictOfT_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        var value = new Todo(1);

        // Act
        var result = Results.Conflict(value) as Conflict<Todo>;

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
        var result = Results.Content(content, contentType, encoding) as ContentHttpResult;

        // Assert
        Assert.Null(result.StatusCode);
        Assert.Equal(content, result.ResponseContent);
        Assert.Equal("text/plain; charset=utf-8", result.ContentType);
    }

    [Fact]
    public void Content_WithContentAndContentTypeAndEncodingAndStatusCode_ResultHasCorrectValues()
    {
        // Arrange
        var content = "test content";
        var contentType = "text/plain";
        var encoding = Encoding.UTF8;
        var statusCode = 201;

        // Act
        var result = Results.Content(content, contentType, encoding, statusCode) as ContentHttpResult;

        // Assert
        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(content, result.ResponseContent);
        Assert.Equal("text/plain; charset=utf-8", result.ContentType);
    }

    [Fact]
    public void Created_WithNoArgs_SetsLocationNull()
    {
        //Act
        var result = Results.Created() as Created;

        //Assert
        Assert.Null(result.Location);
    }

    [Fact]
    public void Created_WithStringUriAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var uri = "https://example.com/entity";
        object value = new { };

        // Act
        var result = Results.Created(uri, value) as Created<object>;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(uri, result.Location);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void CreatedOfT_WithStringUriAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var uri = "https://example.com/entity";
        var value = new Todo(1);

        // Act
        var result = Results.Created(uri, value) as Created<Todo>;

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
        object value = new { };

        // Act
        var result = Results.Created(uri, value) as Created<object>;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(uri.ToString(), result.Location);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void CreatedOfT_WithUriAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var uri = new Uri("https://example.com/entity");
        var value = new Todo(1);

        // Act
        var result = Results.Created(uri, value) as Created<Todo>;

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
    public void Created_WithNullStringUri_SetsLocationNull()
    {
        //Act
        var result = Results.Created(default(string), null) as Created;

        //Assert
        Assert.Null(result.Location);
    }

    [Fact]
    public void Created_WithEmptyStringUri_SetsLocationEmpty()
    {
        //Act
        var result = Results.Created(string.Empty, null) as Created;

        //Assert
        Assert.Empty(result.Location);
    }

    [Fact]
    public void Created_WithNullUri_SetsLocationNull()
    {
        // Act
        var result = Results.Created(default(Uri), null) as Created;

        //Assert
        Assert.Null(result.Location);
    }

    [Fact]
    public void Created_WithNullStringUriAndValue_SetsLocationNull()
    {
        //Arrange
        object value = new { };

        // Act
        var result = Results.Created(default(string), value) as Created<object>;

        //Assert
        Assert.Null(result.Location);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Created_WithEmptyStringUriAndValue_SetsLocationEmpty()
    {
        //Arrange
        object value = new { };

        // Act
        var result = Results.Created(string.Empty, value) as Created<object>;

        //Assert
        Assert.Empty(result.Location);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Created_WithNullUriAndValue_SetsLocationNull()
    {
        //Arrange
        object value = new { };

        // Act
        var result = Results.Created(default(Uri), value) as Created<object>;

        //Assert
        Assert.Null(result.Location);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void CreatedAtRoute_WithRouteNameAndRouteValuesAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        var routeValues = new { foo = 123 };
        object value = new { };

        // Act
        var result = Results.CreatedAtRoute(routeName, routeValues, value) as CreatedAtRoute<object>;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(new RouteValueDictionary(routeValues), result.RouteValues);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void CreatedAtRoute_WithRouteNameAndRouteValueDictionaryAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        var routeValues = new RouteValueDictionary { ["foo"] = 123 };
        object value = new { };

        // Act
        var result = Results.CreatedAtRoute(routeName, routeValues, value) as CreatedAtRoute<object>;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(routeValues, result.RouteValues);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void CreatedAtRoute_WithNullRouteNameAndRouteValues_ResultHasCorrectValues()
    {
        // Arrange

        // Act
        var result = Results.CreatedAtRoute(null, null) as CreatedAtRoute;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
    }

    [Fact]
    public void CreatedAtRoute_WithNullRouteNameAndRouteValuesAndValue_ResultHasCorrectValues()
    {
        // Arrange

        // Act
        var result = Results.CreatedAtRoute(null, null, null) as CreatedAtRoute;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
    }

    [Fact]
    public void CreatedAtRouteOfT_WithRouteNameAndRouteValuesAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        var routeValues = new { foo = 123 };
        var value = new Todo(1);

        // Act
        var result = Results.CreatedAtRoute(routeName, routeValues, value) as CreatedAtRoute<Todo>;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(new RouteValueDictionary(routeValues), result.RouteValues);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void CreatedAtRouteOfT_WithRouteNameAndRouteValueDictionaryAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        var routeValues = new RouteValueDictionary { ["foo"] = 123 };
        var value = new Todo(1);

        // Act
        var result = Results.CreatedAtRoute(routeName, routeValues, value) as CreatedAtRoute<Todo>;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(routeValues, result.RouteValues);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void CreatedAtRoute_WithRouteNameAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        object value = new { };

        // Act
        var result = Results.CreatedAtRoute(routeName, null, value) as CreatedAtRoute<object>;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void CreatedAtRouteOfT_WithRouteNameAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        var value = new Todo(1);

        // Act
        var result = Results.CreatedAtRoute(routeName, null, value) as CreatedAtRoute<Todo>;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void CreatedAtRouteOfT_WithNullRouteNameAndRouteValues_ResultHasCorrectValues()
    {
        // Arrange

        // Act
        var result = Results.CreatedAtRoute<object>(null, null, null) as CreatedAtRoute;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
    }

    [Fact]
    public void CreatedAtRoute_WithRouteName_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";

        // Act
        var result = Results.CreatedAtRoute(routeName, null, null) as CreatedAtRoute;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
    }

    [Fact]
    public void CreatedAtRoute_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = Results.CreatedAtRoute() as CreatedAtRoute;

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
    }

    [Fact]
    public void Empty_IsEmptyInstance()
    {
        // Act
        var result = Results.Empty as EmptyHttpResult;

        // Assert
        Assert.Equal(EmptyHttpResult.Instance, result);
    }

    [Fact]
    public void Json_WithAllArgs_ResultHasCorrectValues()
    {
        // Arrange
        object data = new { };
        var options = new JsonSerializerOptions();
        var contentType = "application/custom+json";
        var statusCode = StatusCodes.Status208AlreadyReported;

        // Act
        var result = Results.Json(data, options, contentType, statusCode) as JsonHttpResult<object>;

        // Assert
        Assert.Equal(data, result.Value);
        Assert.Equal(options, result.JsonSerializerOptions);
        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(statusCode, result.StatusCode);
    }

    [Fact]
    public void JsonOfT_WithAllArgs_ResultHasCorrectValues()
    {
        // Arrange
        var data = new Todo(1);
        var options = new JsonSerializerOptions();
        var contentType = "application/custom+json";
        var statusCode = StatusCodes.Status208AlreadyReported;

        // Act
        var result = Results.Json(data, options, contentType, statusCode) as JsonHttpResult<Todo>;

        // Assert
        Assert.Equal(data, result.Value);
        Assert.Equal(options, result.JsonSerializerOptions);
        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(statusCode, result.StatusCode);
    }

    [Fact]
    public void Json_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = Results.Json(null) as JsonHttpResult<object>;

        // Assert
        Assert.Null(result.Value);
        Assert.Null(result.JsonSerializerOptions);
        Assert.Null(result.ContentType);
        Assert.Null(result.StatusCode);
    }

    [Fact]
    public void Json_WithTypeInfo_ResultHasCorrectValues()
    {
        // Act
        var result = Results.Json(null, StringJsonContext.Default.String as JsonTypeInfo) as JsonHttpResult<object>;

        // Assert
        Assert.Null(result.Value);
        Assert.Null(result.JsonSerializerOptions);
        Assert.Null(result.ContentType);
        Assert.Null(result.StatusCode);
        Assert.Equal(StringJsonContext.Default.String, result.JsonTypeInfo);
    }

    [Fact]
    public void Json_WithJsonContext_ResultHasCorrectValues()
    {
        // Act
        var result = Results.Json(null, typeof(string), StringJsonContext.Default) as JsonHttpResult<object>;

        // Assert
        Assert.Null(result.Value);
        Assert.Null(result.JsonSerializerOptions);
        Assert.Null(result.ContentType);
        Assert.Null(result.StatusCode);
        Assert.IsAssignableFrom<JsonTypeInfo<string>>(result.JsonTypeInfo);
    }

    [Fact]
    public void JsonOfT_WithTypeInfo_ResultHasCorrectValues()
    {
        // Act
        var result = Results.Json(null, StringJsonContext.Default.String) as JsonHttpResult<string>;

        // Assert
        Assert.Null(result.Value);
        Assert.Null(result.JsonSerializerOptions);
        Assert.Null(result.ContentType);
        Assert.Null(result.StatusCode);
        Assert.Equal(StringJsonContext.Default.String, result.JsonTypeInfo);
    }

    [Fact]
    public void JsonOfT_WithJsonContext_ResultHasCorrectValues()
    {
        // Act
        var result = Results.Json<string>(null, StringJsonContext.Default) as JsonHttpResult<string>;

        // Assert
        Assert.Null(result.Value);
        Assert.Null(result.JsonSerializerOptions);
        Assert.Null(result.ContentType);
        Assert.Null(result.StatusCode);
        Assert.IsAssignableFrom<JsonTypeInfo<string>>(result.JsonTypeInfo);
    }

    [Fact]
    public void JsonOfT_WithNullSerializerContext_ThrowsArgException()
    {
        Assert.Throws<ArgumentNullException>("context", () => Results.Json<object>(null, context: null));
    }

    [Fact]
    public void Json_WithNullSerializerContext_ThrowsArgException()
    {
        Assert.Throws<ArgumentNullException>("context", () => Results.Json(null, type: typeof(object), context: null));
    }

    [Fact]
    public void Json_WithInvalidSerializerContext_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Results.Json(null, type: typeof(Todo), context: StringJsonContext.Default));
        Assert.Equal(ex.Message, $"Unable to obtain the JsonTypeInfo for type 'Microsoft.AspNetCore.Http.HttpResults.ResultsTests+Todo' from the context '{typeof(StringJsonContext).FullName}'.");
    }

    [Fact]
    public void JsonOfT_WithInvalidSerializerContext_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => Results.Json<Todo>(null, context: StringJsonContext.Default));
        Assert.Equal(ex.Message, $"Unable to obtain the JsonTypeInfo for type 'Microsoft.AspNetCore.Http.HttpResults.ResultsTests+Todo' from the context '{typeof(StringJsonContext).FullName}'.");
    }

    [Fact]
    public void Json_WithNullTypeInfo_ThrowsArgException()
    {
        Assert.Throws<ArgumentNullException>("jsonTypeInfo", () => Results.Json(null, jsonTypeInfo: null));
    }

    [Fact]
    public void JsonOfT_WithNullTypeInfo_ThrowsArgException()
    {
        Assert.Throws<ArgumentNullException>("jsonTypeInfo", () => Results.Json<object>(null, jsonTypeInfo: null));
    }

    [Fact]
    public void LocalRedirect_WithNullStringUrl_ThrowsArgException()
    {
        Assert.Throws<ArgumentNullException>("localUrl", () => Results.LocalRedirect(default(string)));
    }

    [Fact]
    public void LocalRedirect_WithEmptyStringUrl_ThrowsArgException()
    {
        Assert.Throws<ArgumentException>("localUrl", () => Results.LocalRedirect(string.Empty));
    }

    [Fact]
    public void LocalRedirect_WithUrl_ResultHasCorrectValues()
    {
        // Arrange
        var localUrl = "test/path";

        // Act
        var result = Results.LocalRedirect(localUrl) as RedirectHttpResult;

        // Assert
        Assert.Equal(localUrl, result.Url);
        Assert.True(result.AcceptLocalUrlOnly);
        Assert.False(result.Permanent);
        Assert.False(result.PreserveMethod);
    }

    [Fact]
    public void LocalRedirect_WithUrlAndPermanentTrue_ResultHasCorrectValues()
    {
        // Arrange
        var localUrl = "test/path";
        var permanent = true;

        // Act
        var result = Results.LocalRedirect(localUrl, permanent) as RedirectHttpResult;

        // Assert
        Assert.Equal(localUrl, result.Url);
        Assert.True(result.AcceptLocalUrlOnly);
        Assert.True(result.Permanent);
        Assert.False(result.PreserveMethod);
    }

    [Fact]
    public void LocalRedirect_WithUrlAndPermanentTrueAndPreserveTrue_ResultHasCorrectValues()
    {
        // Arrange
        var localUrl = "test/path";
        var permanent = true;
        var preserveMethod = true;

        // Act
        var result = Results.LocalRedirect(localUrl, permanent, preserveMethod) as RedirectHttpResult;

        // Assert
        Assert.Equal(localUrl, result.Url);
        Assert.True(result.AcceptLocalUrlOnly);
        Assert.True(result.Permanent);
        Assert.True(result.PreserveMethod);
    }

    [Fact]
    public void LocalRedirect_WithNonLocalUrlAndPermanentTrueAndPreserveTrue_ResultHasCorrectValues()
    {
        // Arrange
        var localUrl = "https://example.com/non-local-url/example";
        var permanent = true;
        var preserveMethod = true;

        // Act
        var result = Results.LocalRedirect(localUrl, permanent, preserveMethod) as RedirectHttpResult;

        // Assert
        Assert.Equal(localUrl, result.Url);
        Assert.True(result.AcceptLocalUrlOnly);
        Assert.True(result.Permanent);
        Assert.True(result.PreserveMethod);
    }

    [Fact]
    public void NoContent_ResultHasCorrectValues()
    {
        // Act
        var result = Results.NoContent() as NoContent;

        // Assert
        Assert.Equal(StatusCodes.Status204NoContent, result.StatusCode);
    }

    [Fact]
    public void NotFound_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        object value = new { };

        // Act
        var result = Results.NotFound(value) as NotFound<object>;

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void NotFoundOfT_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        var value = new Todo(1);

        // Act
        var result = Results.NotFound(value) as NotFound<Todo>;

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void NotFound_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = Results.NotFound() as NotFound;

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
    }

    [Fact]
    public void Ok_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        object value = new { };

        // Act
        var result = Results.Ok(value) as Ok<object>;

        // Assert
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void OkOfT_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        var value = new Todo(1);

        // Act
        var result = Results.Ok(value) as Ok<Todo>;

        // Assert
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Ok_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = Results.Ok() as Ok;

        // Assert
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    }

    [Fact]
    public void Problem_WithNullProblem_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("problemDetails", () => Results.Problem(default(ProblemDetails)));
    }

    [Fact]
    public void Problem_WithArgs_ResultHasCorrectValues()
    {
        // Arrange
        var detail = "test detail";
        var instance = "test instance";
        var statusCode = StatusCodes.Status409Conflict;
        var title = "test title";
        var type = "test type";
        var extensions = new Dictionary<string, object> { { "test", "value" } };

        // Act
        var result = Results.Problem(detail, instance, statusCode, title, type, extensions) as ProblemHttpResult;

        // Assert
        Assert.Equal(detail, result.ProblemDetails.Detail);
        Assert.Equal(instance, result.ProblemDetails.Instance);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(title, result.ProblemDetails.Title);
        Assert.Equal(type, result.ProblemDetails.Type);
        Assert.Equal(extensions, result.ProblemDetails.Extensions);
    }

    [Fact]
    public void Problem_ResultHasCorrectValues()
    {
        // Arrange
        var detail = "test detail";
        var instance = "test instance";
        var statusCode = StatusCodes.Status409Conflict;
        var title = "test title";
        var type = "test type";
        var extensions = new List<KeyValuePair<string, object>> { new("test", "value") };

        // Act
        var result = Results.Problem(detail, instance, statusCode, title, type, extensions) as ProblemHttpResult;

        // Assert
        Assert.Equal(detail, result.ProblemDetails.Detail);
        Assert.Equal(instance, result.ProblemDetails.Instance);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(title, result.ProblemDetails.Title);
        Assert.Equal(type, result.ProblemDetails.Type);
        Assert.Equal(extensions, result.ProblemDetails.Extensions);
    }

    [Fact]
    public void Problem_WithReadOnlyDictionary_ResultHasCorrectValues()
    {
        // Arrange
        var detail = "test detail";
        var instance = "test instance";
        var statusCode = StatusCodes.Status409Conflict;
        var title = "test title";
        var type = "test type";
        var extensions = (IReadOnlyDictionary<string, object>)new Dictionary<string, object> { ["test"] = "value" };

        // Act
        var result = Results.Problem(detail, instance, statusCode, title, type, extensions) as ProblemHttpResult;

        // Assert
        Assert.Equal(detail, result.ProblemDetails.Detail);
        Assert.Equal(instance, result.ProblemDetails.Instance);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(title, result.ProblemDetails.Title);
        Assert.Equal(type, result.ProblemDetails.Type);
        Assert.Equal(extensions, result.ProblemDetails.Extensions);
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest, "Bad Request", "https://tools.ietf.org/html/rfc9110#section-15.5.1")]
    [InlineData(StatusCodes.Status418ImATeapot, "I'm a teapot", null)]
    [InlineData(498, null, null)]
    public void Problem_WithOnlyHttpStatus_ResultHasCorrectValues(
        int statusCode,
        string title,
        string type)
    {
        // Act
        var result = Results.Problem(statusCode: statusCode) as ProblemHttpResult;
        // Assert
        Assert.Null(result.ProblemDetails.Detail);
        Assert.Null(result.ProblemDetails.Instance);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(title, result.ProblemDetails.Title);
        Assert.Equal(type, result.ProblemDetails.Type);
        Assert.NotNull(result.ProblemDetails.Extensions);
        Assert.Empty(result.ProblemDetails.Extensions);
    }

    [Fact]
    public void Problem_WithNoArgs_ResultHasCorrectValues()
    {
        /// Act
        var result = Results.Problem() as ProblemHttpResult;

        // Assert
        Assert.Null(result.ProblemDetails.Detail);
        Assert.Null(result.ProblemDetails.Instance);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Equal("An error occurred while processing your request.", result.ProblemDetails.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc9110#section-15.6.1", result.ProblemDetails.Type);
        Assert.Empty(result.ProblemDetails.Extensions);
    }

    [Fact]
    public void Problem_WithProblemArg_ResultHasCorrectValues()
    {
        // Arrange
        var problem = new ProblemDetails { Title = "Test title" };

        // Act
        var result = Results.Problem(problem) as ProblemHttpResult;

        // Assert
        Assert.Equal(problem, result.ProblemDetails);
        Assert.Equal("Test title", result.ProblemDetails.Title);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
    }

    [Fact]
    public void Problem_WithValidationProblemArg_ResultHasCorrectValues()
    {
        // Arrange
        var problem = new HttpValidationProblemDetails { Title = "Test title" };

        // Act
        var result = Results.Problem(problem) as ProblemHttpResult;

        // Assert
        Assert.Equal(problem, result.ProblemDetails);
        Assert.Equal("Test title", result.ProblemDetails.Title);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Fact]
    public void ValidationProblem_WithNullErrors_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("errors", () => Results.ValidationProblem(default(IDictionary<string, string[]>)));
    }

    [Fact]
    public void ValidationProblem_WithValidationProblemArg_ResultHasCorrectValues()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>() { { "testField", new[] { "test error" } } };
        var detail = "test detail";
        var instance = "test instance";
        var statusCode = StatusCodes.Status412PreconditionFailed; // obscure for the test on purpose
        var title = "test title";
        var type = "test type";
        var extensions = new Dictionary<string, object>() { { "testExtension", "test value" } };

        // Act
        // Note: Results.ValidationProblem returns ProblemHttpResult instead of ValidationProblem by design as
        //       as ValidationProblem doesn't allow setting a custom status code so that it can accurately report
        //       a single status code in endpoint metadata via its implementation of IEndpointMetadataProvider
        var result = Results.ValidationProblem(errors, detail, instance, statusCode, title, type, extensions) as ProblemHttpResult;

        // Assert
        Assert.IsType<HttpValidationProblemDetails>(result.ProblemDetails);
        Assert.Equal(errors, ((HttpValidationProblemDetails)result.ProblemDetails).Errors);
        Assert.Equal(detail, result.ProblemDetails.Detail);
        Assert.Equal(instance, result.ProblemDetails.Instance);
        Assert.Equal(statusCode, result.ProblemDetails.Status);
        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(title, result.ProblemDetails.Title);
        Assert.Equal(type, result.ProblemDetails.Type);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal(extensions, result.ProblemDetails.Extensions);
    }

    [Fact]
    public void ValidationProblem_ResultHasCorrectValues()
    {
        // Arrange
        var errors = new List<KeyValuePair<string, string[]>> { new("testField", new[] { "test error" }) };
        var detail = "test detail";
        var instance = "test instance";
        var statusCode = StatusCodes.Status412PreconditionFailed; // obscure for the test on purpose
        var title = "test title";
        var type = "test type";
        var extensions = new List<KeyValuePair<string, object>> { new("testField", "test value") };

        // Act
        // Note: Results.ValidationProblem returns ProblemHttpResult instead of ValidationProblem by design as
        //       as ValidationProblem doesn't allow setting a custom status code so that it can accurately report
        //       a single status code in endpoint metadata via its implementation of IEndpointMetadataProvider
        var result = Results.ValidationProblem(errors, detail, instance, statusCode, title, type, extensions) as ProblemHttpResult;

        // Assert
        Assert.IsType<HttpValidationProblemDetails>(result.ProblemDetails);
        Assert.Equal(errors, ((HttpValidationProblemDetails)result.ProblemDetails).Errors);
        Assert.Equal(detail, result.ProblemDetails.Detail);
        Assert.Equal(instance, result.ProblemDetails.Instance);
        Assert.Equal(statusCode, result.ProblemDetails.Status);
        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(title, result.ProblemDetails.Title);
        Assert.Equal(type, result.ProblemDetails.Type);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal(extensions, result.ProblemDetails.Extensions);
    }

    [Fact]
    public void Redirect_WithNullStringUrl_ThrowsArgException()
    {
        Assert.Throws<ArgumentNullException>("url", () => Results.Redirect(default(string)));
    }

    [Fact]
    public void Redirect_WithEmptyStringUrl_ThrowsArgException()
    {
        Assert.Throws<ArgumentException>("url", () => Results.Redirect(string.Empty));
    }

    [Fact]
    public void Redirect_WithDefaults_ResultHasCorrectValues()
    {
        // Arrange
        var url = "https://example.com";

        // Act
        var result = Results.Redirect(url) as RedirectHttpResult;

        // Assert
        Assert.Equal(url, result.Url);
        Assert.False(result.PreserveMethod);
        Assert.False(result.Permanent);
        Assert.False(result.AcceptLocalUrlOnly);
    }

    [Fact]
    public void Redirect_WithPermanentTrue_ResultHasCorrectValues()
    {
        // Arrange
        var url = "https://example.com";

        // Act
        var result = Results.Redirect(url, true) as RedirectHttpResult;

        // Assert
        Assert.Equal(url, result.Url);
        Assert.False(result.PreserveMethod);
        Assert.True(result.Permanent);
        Assert.False(result.AcceptLocalUrlOnly);
    }

    [Fact]
    public void Redirect_WithPreserveMethodTrue_ResultHasCorrectValues()
    {
        // Arrange
        var url = "https://example.com";

        // Act
        var result = Results.Redirect(url, false, true) as RedirectHttpResult;

        // Assert
        Assert.Equal(url, result.Url);
        Assert.True(result.PreserveMethod);
        Assert.False(result.Permanent);
        Assert.False(result.AcceptLocalUrlOnly);
    }

    [Fact]
    public void RedirectToRoute_WithRouteNameAndRouteValuesAndFragment_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        var routeValues = new { foo = 123 };
        var fragment = "test";

        // Act
        var result = Results.RedirectToRoute(routeName, routeValues, true, true, fragment) as RedirectToRouteHttpResult;

        // Assert
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(new RouteValueDictionary(routeValues), result.RouteValues);
        Assert.True(result.Permanent);
        Assert.True(result.PreserveMethod);
        Assert.Equal(fragment, result.Fragment);
    }

    [Fact]
    public void RedirectToRoute_WithRouteNameAndRouteValueDictionaryAndFragment_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        var routeValues = new RouteValueDictionary { ["foo"] = 123 };
        var fragment = "test";

        // Act
        var result = Results.RedirectToRoute(routeName, routeValues, true, true, fragment) as RedirectToRouteHttpResult;

        // Assert
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(routeValues, result.RouteValues);
        Assert.True(result.Permanent);
        Assert.True(result.PreserveMethod);
        Assert.Equal(fragment, result.Fragment);
    }

    [Fact]
    public void RedirectToRoute_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = Results.RedirectToRoute() as RedirectToRouteHttpResult;

        // Assert
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
    }

    [Fact]
    public void RedirectToRoute_WithNoArgs_RouteValueDictionary_ResultHasCorrectValues()
    {
        // Act
        var result = Results.RedirectToRoute(null, null) as RedirectToRouteHttpResult;

        // Assert
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
    }

    [Fact]
    public void StatusCode_ResultHasCorrectValues()
    {
        // Arrange
        var statusCode = StatusCodes.Status412PreconditionFailed;

        // Act
        var result = Results.StatusCode(statusCode) as StatusCodeHttpResult;

        // Assert
        Assert.Equal(statusCode, result.StatusCode);
    }

    [Fact]
    public void Text_WithContentAndContentType_ResultHasCorrectValues()
    {
        // Arrange
        var content = "test content";
        var contentType = "text/plain";

        // Act
        var result = Results.Text(content, contentType) as ContentHttpResult;

        // Assert
        Assert.Null(result.StatusCode);
        Assert.Equal(content, result.ResponseContent);
        Assert.Equal(contentType, result.ContentType);
    }

    [Fact]
    public void Text_WithUtf8ContentAndContentType_ResultHasCorrectValues()
    {
        // Arrange
        var content = "test content"u8.ToArray();
        var contentType = "text/plain";

        // Act
        var result = Results.Text(content, contentType) as Utf8ContentHttpResult;

        // Assert
        Assert.Null(result.StatusCode);
        Assert.Equal(content, result.ResponseContent.ToArray());
        Assert.Equal(contentType, result.ContentType);
    }

    [Fact]
    public void Text_WithUtf8ContentAndContentTypeAndStatusCode_ResultHasCorrectValues()
    {
        // Arrange
        var content = "test content"u8.ToArray();
        var contentType = "text/plain";
        var statusCode = 201;

        // Act
        var result = Results.Text(content, contentType, statusCode) as Utf8ContentHttpResult;

        // Assert
        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(content, result.ResponseContent.ToArray());
        Assert.Equal(contentType, result.ContentType);
    }

    [Fact]
    public void Text_WithContentAndContentTypeAndEncoding_ResultHasCorrectValues()
    {
        // Arrange
        var content = "test content";
        var contentType = "text/plain";
        var encoding = Encoding.ASCII;

        // Act
        var result = Results.Text(content, contentType, encoding) as ContentHttpResult;

        // Assert
        Assert.Null(result.StatusCode);
        Assert.Equal(content, result.ResponseContent);
        var expectedMediaType = MediaTypeHeaderValue.Parse(contentType);
        expectedMediaType.Encoding = encoding;
        Assert.Equal(expectedMediaType.ToString(), result.ContentType);
    }

    [Fact]
    public void Text_WithContentAndContentTypeAndEncodingAndStatusCode_ResultHasCorrectValues()
    {
        // Arrange
        var content = "test content";
        var contentType = "text/plain";
        var encoding = Encoding.ASCII;
        var statusCode = 201;

        // Act
        var result = Results.Text(content, contentType, encoding, statusCode) as ContentHttpResult;

        // Assert
        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(content, result.ResponseContent);
        var expectedMediaType = MediaTypeHeaderValue.Parse(contentType);
        expectedMediaType.Encoding = encoding;
        Assert.Equal(expectedMediaType.ToString(), result.ContentType);
    }

    [Fact]
    public void Unauthorized_ResultHasCorrectValues()
    {
        // Act
        var result = Results.Unauthorized() as UnauthorizedHttpResult;

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public void UnprocessableEntity_ResultHasCorrectValues()
    {
        // Act
        var result = Results.UnprocessableEntity() as UnprocessableEntity;

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
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
        (() => Results.AcceptedAtRoute("routeName", (object)null, null), typeof(AcceptedAtRoute)),
        (() => Results.AcceptedAtRoute("routeName", (object)null, new()), typeof(AcceptedAtRoute<object>)),
        (() => Results.AcceptedAtRoute("routeName", null, null), typeof(AcceptedAtRoute)),
        (() => Results.AcceptedAtRoute("routeName", null, new()), typeof(AcceptedAtRoute<object>)),
        (() => Results.BadRequest(null), typeof(BadRequest)),
        (() => Results.BadRequest(new()), typeof(BadRequest<object>)),
        (() => Results.Bytes(new byte[0], null, null, false, null, null), typeof(FileContentHttpResult)),
        (() => Results.Challenge(null, null), typeof(ChallengeHttpResult)),
        (() => Results.Conflict(null), typeof(Conflict)),
        (() => Results.Conflict(new()), typeof(Conflict<object>)),
        (() => Results.Content("content", null, null), typeof(ContentHttpResult)),
        (() => Results.Content("content", null, null, null), typeof(ContentHttpResult)),
        (() => Results.Created("/path", null), typeof(Created)),
        (() => Results.Created(), typeof(Created)),
        (() => Results.Created("/path", new()), typeof(Created<object>)),
        (() => Results.CreatedAtRoute("routeName", (object)null, null), typeof(CreatedAtRoute)),
        (() => Results.CreatedAtRoute("routeName", (object)null, new()), typeof(CreatedAtRoute<object>)),
        (() => Results.CreatedAtRoute("routeName", null, null), typeof(CreatedAtRoute)),
        (() => Results.CreatedAtRoute("routeName", null, new()), typeof(CreatedAtRoute<object>)),
        (() => Results.Empty, typeof(EmptyHttpResult)),
        (() => Results.File(new byte[0], null, null, false, null, null), typeof(FileContentHttpResult)),
        (() => Results.File(new MemoryStream(), null, null, null, null, false), typeof(FileStreamHttpResult)),
        (() => Results.File(Path.Join(Path.DirectorySeparatorChar.ToString(), "rooted", "path"), null, null, null, null, false), typeof(PhysicalFileHttpResult)),
        (() => Results.File("path", null, null, null, null, false), typeof(VirtualFileHttpResult)),
        (() => Results.Forbid(null, null), typeof(ForbidHttpResult)),
        (() => Results.InternalServerError(), typeof(InternalServerError)),
        (() => Results.InternalServerError<object>(new()), typeof(InternalServerError<object>)),
        (() => Results.Json(new(), (JsonSerializerOptions)null, null, null), typeof(JsonHttpResult<object>)),
        (() => Results.NoContent(), typeof(NoContent)),
        (() => Results.NotFound(null), typeof(NotFound)),
        (() => Results.NotFound(new()), typeof(NotFound<object>)),
        (() => Results.Ok(null), typeof(Ok)),
        (() => Results.Ok(new()), typeof(Ok<object>)),
        (() => Results.Problem(new()), typeof(ProblemHttpResult)),
        (() => Results.Stream(new MemoryStream(), null, null, null, null, false), typeof(FileStreamHttpResult)),
        (() => Results.Stream(s => Task.CompletedTask, null, null, null, null), typeof(PushStreamHttpResult)),
        (() => Results.Text("content", null, null), typeof(ContentHttpResult)),
        (() => Results.Text("content", null, null, null), typeof(ContentHttpResult)),
        (() => Results.Redirect("/path", false, false), typeof(RedirectHttpResult)),
        (() => Results.LocalRedirect("/path", false, false), typeof(RedirectHttpResult)),
        (() => Results.RedirectToRoute("routeName", (object)null, false, false, null), typeof(RedirectToRouteHttpResult)),
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

    private record Todo(int Id);

    [JsonSerializable(typeof(string))]
    private partial class StringJsonContext : JsonSerializerContext
    { }
}
