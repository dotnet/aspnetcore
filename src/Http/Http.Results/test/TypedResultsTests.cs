// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.IO.Pipelines;
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
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Http.HttpResults;

public partial class TypedResultsTests
{
    [Fact]
    public void Accepted_WithStringUrlAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var uri = "https://example.org";
        var value = new { };

        // Act
        var result = TypedResults.Accepted(uri, value);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(uri, result.Location);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Accepted_WithStringUrl_ResultHasCorrectValues()
    {
        // Arrange
        var uri = "https://example.org";

        // Act
        var result = TypedResults.Accepted(uri);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(uri, result.Location);
    }

    [Fact]
    public void Accepted_WithNullStringUrl_ResultHasCorrectValues()
    {
        // Arrange
        var uri = default(string);

        // Act
        var result = TypedResults.Accepted(uri);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(uri, result.Location);
    }

    [Fact]
    public void Accepted_WithUriAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var uri = new Uri("https://example.org");
        var value = new { };

        // Act
        var result = TypedResults.Accepted(uri, value);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(uri.ToString(), result.Location);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Accepted_WithUri_ResultHasCorrectValues()
    {
        // Arrange
        var uri = new Uri("https://example.org");

        // Act
        var result = TypedResults.Accepted(uri);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(uri.ToString(), result.Location);
    }

    [Fact]
    public void Accepted_WithNullUri_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("uri", () => TypedResults.Accepted(default(Uri)));
    }

    [Fact]
    public void Accepted_WithNullUriAndValue_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>("uri", () => TypedResults.Accepted(default(Uri), default(object)));
    }

    [Fact]
    public void AcceptedAtRoute_WithRouteNameAndRouteValuesAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        var routeValues = new { foo = 123 };
        var value = new { };

        // Act
        var result = TypedResults.AcceptedAtRoute(value, routeName, routeValues);

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
        var result = TypedResults.AcceptedAtRoute(routeName, routeValues);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(new RouteValueDictionary(routeValues), result.RouteValues);
    }

    [Fact]
    public void AcceptedAtRoute_WithNullRouteNameAndRouteValues_ResultHasCorrectValues()
    {
        // Arrange

        // Act
        var result = TypedResults.AcceptedAtRoute(null, null);

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
        var result = TypedResults.AcceptedAtRoute<object>(null, null, null);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
    }

    [Fact]
    public void AcceptedAtRoute_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = TypedResults.AcceptedAtRoute();

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.NotNull(result.RouteValues);
    }

    [Fact]
    public void AcceptedAtRoute_WithRouteValues_ResultHasCorrectValues()
    {
        // Arrange
        var routeValues = new { foo = 123 };

        // Act
        var result = TypedResults.AcceptedAtRoute(routeValues: routeValues);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(routeValues), result.RouteValues);
    }

    [Fact]
    public void AcceptedAtRoute_WithRouteValuesAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var routeValues = new { foo = 123 };
        var value = new { };

        // Act
        var result = TypedResults.AcceptedAtRoute(value: value, routeValues: routeValues);

        // Assert
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(routeValues), result.RouteValues);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void BadRequest_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        var value = new { };

        // Act
        var result = TypedResults.BadRequest(value);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void BadRequest_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = TypedResults.BadRequest();

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
            0 => TypedResults.Bytes(contents, contentType, fileDownloadName, enableRangeProcessing, lastModified, entityTag),
            _ => TypedResults.File(contents, contentType, fileDownloadName, enableRangeProcessing, lastModified, entityTag)
        };

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
    [MemberData(nameof(PhysicalOrVirtualFile_ResultHasCorrectValues_Data))]
    public void PhysicalFile_ResultHasCorrectValues(string contentType, string fileDownloadName, bool enableRangeProcessing, DateTimeOffset lastModified, EntityTagHeaderValue entityTag)
    {
        // Arrange
        var path = "path";

        // Act
        var result = TypedResults.PhysicalFile(path, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing);

        // Assert
        Assert.Equal(path, result.FileName);
        Assert.Equal(contentType ?? "application/octet-stream", result.ContentType);
        Assert.Equal(fileDownloadName, result.FileDownloadName);
        Assert.Equal(enableRangeProcessing, result.EnableRangeProcessing);
        Assert.Equal(lastModified, result.LastModified);
        Assert.Equal(entityTag, result.EntityTag);
    }

    [Theory]
    [MemberData(nameof(PhysicalOrVirtualFile_ResultHasCorrectValues_Data))]
    public void VirtualFile_ResultHasCorrectValues(string contentType, string fileDownloadName, bool enableRangeProcessing, DateTimeOffset lastModified, EntityTagHeaderValue entityTag)
    {
        // Arrange
        var path = "path";

        // Act
        var result = TypedResults.VirtualFile(path, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing);

        // Assert
        Assert.Equal(path, result.FileName);
        Assert.Equal(contentType ?? "application/octet-stream", result.ContentType);
        Assert.Equal(fileDownloadName, result.FileDownloadName);
        Assert.Equal(enableRangeProcessing, result.EnableRangeProcessing);
        Assert.Equal(lastModified, result.LastModified);
        Assert.Equal(entityTag, result.EntityTag);
    }

    public static IEnumerable<object[]> PhysicalOrVirtualFile_ResultHasCorrectValues_Data => new List<object[]>
    {
        new object[] { "text/plain", "testfile", true, new DateTimeOffset(2022, 1, 1, 0, 0, 1, TimeSpan.FromHours(-8)), EntityTagHeaderValue.Any },
        new object[] { default(string), default(string), default(bool), default(DateTimeOffset?), default(EntityTagHeaderValue) },
        new object[] { "text/plain", "testfile", true, new DateTimeOffset(2022, 1, 1, 0, 0, 1, TimeSpan.FromHours(-8)), EntityTagHeaderValue.Any },
        new object[] { default(string), default(string), default(bool), default(DateTimeOffset?), default(EntityTagHeaderValue) }
    };

    [Fact]
    public void Bytes_WithNullContents_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("contents", () => TypedResults.Bytes(null));
    }

    [Fact]
    public void File_WithNullContents_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("fileContents", () => TypedResults.File(default(byte[])));
    }

    [Fact]
    public void File_WithNullStream_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("fileStream", () => TypedResults.File(default(Stream)));
    }

    [Fact]
    public void Stream_WithNullStream_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("stream", () => TypedResults.Stream(default(Stream)));
    }

    [Fact]
    public void Stream_WithNullPipeReader_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("pipeReader", () => TypedResults.Stream(default(PipeReader)));
    }

    [Fact]
    public void Stream_WithNullCallback_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("streamWriterCallback", () => TypedResults.Stream(default(Func<Stream, Task>)));
    }

    [Fact]
    public void PhysicalFile_WithNullPath_ThrowsArgException()
    {
        Assert.Throws<ArgumentNullException>("path", () => TypedResults.PhysicalFile(default(string)));
    }

    [Fact]
    public void PhysicalFile_WithEmptyPath_ThrowsArgException()
    {
        Assert.Throws<ArgumentException>("path", () => TypedResults.PhysicalFile(string.Empty));
    }

    [Fact]
    public void VirtualFile_WithNullPath_ThrowsArgException()
    {
        Assert.Throws<ArgumentNullException>("path", () => TypedResults.VirtualFile(default(string)));
    }

    [Fact]
    public void VirtualFile_WithEmptyPath_ThrowsArgException()
    {
        Assert.Throws<ArgumentException>("path", () => TypedResults.VirtualFile(string.Empty));
    }

    [Theory]
    [MemberData(nameof(Stream_ResultHasCorrectValues_Data))]
    public void Stream_ResultHasCorrectValues(int overload, string contentType, string fileDownloadName, bool enableRangeProcessing, DateTimeOffset lastModified, EntityTagHeaderValue entityTag)
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        var result = overload switch
        {
            0 => TypedResults.Stream(stream, contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing),
            1 => TypedResults.Stream(PipeReader.Create(stream), contentType, fileDownloadName, lastModified, entityTag, enableRangeProcessing),
            _ => (IResult)TypedResults.Stream((s) => Task.CompletedTask, contentType, fileDownloadName, lastModified, entityTag)
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

    [Theory]
    [MemberData(nameof(ChallengeForbidSignInOut_ResultHasCorrectValues_Data))]
    public void Challenge_ResultHasCorrectValues(AuthenticationProperties properties, IList<string> authenticationSchemes)
    {
        // Act
        var result = TypedResults.Challenge(properties, authenticationSchemes);

        // Assert
        Assert.Equal(properties, result.Properties);
        Assert.Equal(authenticationSchemes ?? new ReadOnlyCollection<string>(new List<string>()), result.AuthenticationSchemes);
    }

    [Theory]
    [MemberData(nameof(ChallengeForbidSignInOut_ResultHasCorrectValues_Data))]
    public void Forbid_ResultHasCorrectValues(AuthenticationProperties properties, IList<string> authenticationSchemes)
    {
        // Act
        var result = TypedResults.Forbid(properties, authenticationSchemes);

        // Assert
        Assert.Equal(properties, result.Properties);
        Assert.Equal(authenticationSchemes ?? new ReadOnlyCollection<string>(new List<string>()), result.AuthenticationSchemes);
    }

    [Theory]
    [MemberData(nameof(ChallengeForbidSignInOut_ResultHasCorrectValues_Data))]
    public void SignOut_ResultHasCorrectValues(AuthenticationProperties properties, IList<string> authenticationSchemes)
    {
        // Act
        var result = TypedResults.SignOut(properties, authenticationSchemes);

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
        var result = TypedResults.SignIn(principal, properties, authenticationSchemes?.First());

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
        Assert.Throws<ArgumentNullException>("principal", () => TypedResults.SignIn(null));
    }

    [Fact]
    public void Conflict_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        var value = new { };

        // Act
        var result = TypedResults.Conflict(value);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Conflict_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = TypedResults.Conflict();

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
        var result = TypedResults.Content(content, mediaType);

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
        var result = TypedResults.Content(content, contentType, encoding);

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
        var result = TypedResults.Content(content, contentType, encoding, statusCode);

        // Assert
        Assert.Equal(statusCode, result.StatusCode);
        Assert.Equal(content, result.ResponseContent);
        Assert.Equal("text/plain; charset=utf-8", result.ContentType);
    }

    [Fact]
    public void Created_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = TypedResults.Created();

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Null(result.Location);
    }

    [Fact]
    public void Created_WithStringUriAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var uri = "https://example.com/entity";
        var value = new { };

        // Act
        var result = TypedResults.Created(uri, value);

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
        var result = TypedResults.Created(uri);

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
        var result = TypedResults.Created(uri, value);

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
        var result = TypedResults.Created(uri);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(uri.ToString(), result.Location);
    }

    [Fact]
    public void Created_WithNullStringUri_SetsLocationNull()
    {
        // Act
        var result = TypedResults.Created(default(string));

        // Assert
        Assert.Null(result.Location);
    }

    [Fact]
    public void Created_WithEmptyStringUri_SetsLocationEmpty()
    {
        var result = TypedResults.Created(string.Empty);
        Assert.Empty(result.Location);
    }

    [Fact]
    public void Created_WithNullUri_SetsLocationNull()
    {
        // Act
        var result = TypedResults.Created(default(Uri));
        Assert.Null(result.Location);
    }

    [Fact]
    public void CreatedOfT_WithNullStringUri_SetsLocationNull()
    {
        // Act
        var result = TypedResults.Created(default(string), default(object));

        // Assert
        Assert.Null(result.Location);
    }

    [Fact]
    public void CreatedOfT_WithEmptyStringUri_SetsLocationEmpty()
    {
        // Act
        var result = TypedResults.Created(string.Empty, default(object));

        // Assert
        Assert.Empty(result.Location);
    }

    [Fact]
    public void CreatedOfT_WithNullUri_SetsLocationNull()
    {
        // Act
        var result = TypedResults.Created(default(Uri), default(object));

        // Assert
        Assert.Null(result.Location);
    }

    [Fact]
    public void CreatedAtRoute_WithRouteNameAndRouteValuesAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        var routeValues = new { foo = 123 };
        var value = new { };

        // Act
        var result = TypedResults.CreatedAtRoute(value, routeName, routeValues);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(new RouteValueDictionary(routeValues), result.RouteValues);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void CreatedAtRoute_WithRouteNameAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";
        var value = new { };

        // Act
        var result = TypedResults.CreatedAtRoute(value, routeName, null);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void CreatedAtRoute_WithRouteName_ResultHasCorrectValues()
    {
        // Arrange
        var routeName = "routeName";

        // Act
        var result = TypedResults.CreatedAtRoute(routeName, default(object));

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
    }

    [Fact]
    public void CreatedAtRoute_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = TypedResults.CreatedAtRoute();

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
    }

    [Fact]
    public void CreatedAtRoute_WithRouteValues_ResultHasCorrectValues()
    {
        // Arrange
        var routeValues = new { foo = 123 };

        // Act
        var result = TypedResults.CreatedAtRoute(routeValues: routeValues);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(routeValues), result.RouteValues);
    }

    [Fact]
    public void CreatedAtRoute_WithRouteValuesAndValue_ResultHasCorrectValues()
    {
        // Arrange
        var routeValues = new { foo = 123 };
        var value = new { };

        // Act
        var result = TypedResults.CreatedAtRoute(value: value, routeValues: routeValues);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(routeValues), result.RouteValues);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void CreatedAtRoute_WithNullRouteNameAndRouteValues_ResultHasCorrectValues()
    {
        // Arrange

        // Act
        var result = TypedResults.CreatedAtRoute(null, null);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
    }

    [Fact]
    public void CreatedAtRouteOfT_WithNullRouteNameAndRouteValuesAndValue_ResultHasCorrectValues()
    {
        // Arrange

        // Act
        var result = TypedResults.CreatedAtRoute<object>(null, null, null);

        // Assert
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
    }

    [Fact]
    public void Empty_IsEmptyInstance()
    {
        // Act
        var result = TypedResults.Empty;

        // Assert
        Assert.Equal(EmptyHttpResult.Instance, result);
    }

    [Fact]
    public void Json_WithAllArgs_ResultHasCorrectValues()
    {
        // Arrange
        var data = new { };
        var options = new JsonSerializerOptions();
        var contentType = "application/custom+json";
        var statusCode = StatusCodes.Status208AlreadyReported;

        // Act
        var result = TypedResults.Json(data, options, contentType, statusCode);

        // Assert
        Assert.Equal(data, result.Value);
        Assert.Equal(options, result.JsonSerializerOptions);
        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(statusCode, result.StatusCode);
    }

    [Fact]
    public void Json_WithNoArgs_ResultHasCorrectValues()
    {
        // Arrange
        var data = default(object);

        // Act
        var result = TypedResults.Json(data);

        // Assert
        Assert.Null(result.Value);
        Assert.Null(result.JsonSerializerOptions);
        Assert.Null(result.ContentType);
        Assert.Null(result.StatusCode);
    }

    [Fact]
    public void Json_WithTypeInfo_ResultHasCorrectValues()
    {
        // Arrange
        var data = default(object);

        // Act
        var result = TypedResults.Json(data, ObjectJsonContext.Default.Object);

        // Assert
        Assert.Null(result.Value);
        Assert.Null(result.JsonSerializerOptions);
        Assert.Null(result.ContentType);
        Assert.Null(result.StatusCode);
        Assert.Equal(ObjectJsonContext.Default.Object, result.JsonTypeInfo);
    }

    [Fact]
    public void Json_WithJsonContext_ResultHasCorrectValues()
    {
        // Arrange
        var data = default(object);

        // Act
        var result = TypedResults.Json(data, ObjectJsonContext.Default);

        // Assert
        Assert.Null(result.Value);
        Assert.Null(result.JsonSerializerOptions);
        Assert.Null(result.ContentType);
        Assert.Null(result.StatusCode);
        Assert.IsAssignableFrom<JsonTypeInfo<object>>(result.JsonTypeInfo);
    }

    [Fact]
    public void Json_WithNullSerializerContext_ThrowsArgException()
    {
        // Arrange
        var data = default(object);

        Assert.Throws<ArgumentNullException>("context", () => TypedResults.Json(data, context: null));
    }

    [Fact]
    public void Json_WithInvalidSerializerContext_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => TypedResults.Json(string.Empty, context: ObjectJsonContext.Default));
        Assert.Equal(ex.Message, $"Unable to obtain the JsonTypeInfo for type 'System.String' from the context '{typeof(ObjectJsonContext).FullName}'.");
    }

    [Fact]
    public void Json_WithNullTypeInfo_ThrowsArgException()
    {
        // Arrange
        var data = default(object);

        Assert.Throws<ArgumentNullException>("jsonTypeInfo", () => TypedResults.Json(data, jsonTypeInfo: null));
    }

    [Fact]
    public void LocalRedirect_WithNullStringUrl_ThrowsArgException()
    {
        Assert.Throws<ArgumentNullException>("localUrl", () => TypedResults.LocalRedirect(default(string)));
    }

    [Fact]
    public void LocalRedirect_WithEmptyStringUrl_ThrowsArgException()
    {
        Assert.Throws<ArgumentException>("localUrl", () => TypedResults.LocalRedirect(string.Empty));
    }

    [Fact]
    public void LocalRedirect_WithUrl_ResultHasCorrectValues()
    {
        // Arrange
        var localUrl = "test/path";

        // Act
        var result = TypedResults.LocalRedirect(localUrl);

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
        var result = TypedResults.LocalRedirect(localUrl, permanent);

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
        var result = TypedResults.LocalRedirect(localUrl, permanent, preserveMethod);

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
        var result = TypedResults.LocalRedirect(localUrl, permanent, preserveMethod);

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
        var result = TypedResults.NoContent();

        // Assert
        Assert.Equal(StatusCodes.Status204NoContent, result.StatusCode);
    }

    [Fact]
    public void NotFound_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        var value = new { };

        // Act
        var result = TypedResults.NotFound(value);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void NotFound_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = TypedResults.NotFound();

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
    }

    [Fact]
    public void Ok_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        var value = new { };

        // Act
        var result = TypedResults.Ok(value);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Ok_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = TypedResults.Ok();

        // Assert
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    }

    [Fact]
    public void Problem_WithNullProblem_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("problemDetails", () => TypedResults.Problem(default(ProblemDetails)));
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
        var result = TypedResults.Problem(detail, instance, statusCode, title, type, extensions);

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
        var result = TypedResults.Problem(detail, instance, statusCode, title, type, extensions);

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
    public void Problem_WithOnlyHttpStatus_ResultHasCorrectValues(
        int statusCode,
        string title,
        string type)
    {
        // Act
        var result = TypedResults.Problem(statusCode: statusCode);

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
        var result = TypedResults.Problem();

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
        var result = TypedResults.Problem(problem);

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
        var result = TypedResults.Problem(problem);

        // Assert
        Assert.Equal(problem, result.ProblemDetails);
        Assert.Equal("Test title", result.ProblemDetails.Title);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Fact]
    public void ValidationProblem_WithNullErrors_ThrowsArgNullException()
    {
        Assert.Throws<ArgumentNullException>("errors", () => TypedResults.ValidationProblem(default(IDictionary<string, string[]>)));
    }

    [Fact]
    public void ValidationProblem_WithValidationProblemArg_ResultHasCorrectValues()
    {
        // Arrange
        var errors = new Dictionary<string, string[]>() { { "testField", new[] { "test error" } } };
        var detail = "test detail";
        var instance = "test instance";
        var title = "test title";
        var type = "test type";
        var extensions = new Dictionary<string, object>() { { "testExtension", "test value" } };

        // Act
        var result = TypedResults.ValidationProblem(errors, detail, instance, title, type, extensions);

        // Assert
        Assert.Equal(errors, result.ProblemDetails.Errors);
        Assert.Equal(detail, result.ProblemDetails.Detail);
        Assert.Equal(instance, result.ProblemDetails.Instance);
        Assert.Equal(StatusCodes.Status400BadRequest, result.ProblemDetails.Status);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
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
        var title = "test title";
        var type = "test type";
        var extensions = new List<KeyValuePair<string, object>> { new("testField", "test value") };

        // Act
        var result = TypedResults.ValidationProblem(errors, detail, instance, title, type, extensions);

        // Assert
        Assert.Equal(errors, result.ProblemDetails.Errors);
        Assert.Equal(detail, result.ProblemDetails.Detail);
        Assert.Equal(instance, result.ProblemDetails.Instance);
        Assert.Equal(StatusCodes.Status400BadRequest, result.ProblemDetails.Status);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal(title, result.ProblemDetails.Title);
        Assert.Equal(type, result.ProblemDetails.Type);
        Assert.Equal("application/problem+json", result.ContentType);
        Assert.Equal(extensions, result.ProblemDetails.Extensions);
    }

    [Fact]
    public void Redirect_WithNullStringUrl_ThrowsArgException()
    {
        Assert.Throws<ArgumentNullException>("url", () => TypedResults.Redirect(default(string)));
    }

    [Fact]
    public void Redirect_WithEmptyStringUrl_ThrowsArgException()
    {
        Assert.Throws<ArgumentException>("url", () => TypedResults.Redirect(string.Empty));
    }

    [Fact]
    public void Redirect_WithDefaults_ResultHasCorrectValues()
    {
        // Arrange
        var url = "https://example.com";

        // Act
        var result = TypedResults.Redirect(url);

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
        var result = TypedResults.Redirect(url, true);

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
        var result = TypedResults.Redirect(url, false, true);

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
        var result = TypedResults.RedirectToRoute(routeName, routeValues, true, true, fragment);

        // Assert
        Assert.Equal(routeName, result.RouteName);
        Assert.Equal(new RouteValueDictionary(routeValues), result.RouteValues);
        Assert.True(result.Permanent);
        Assert.True(result.PreserveMethod);
        Assert.Equal(fragment, result.Fragment);
    }

    [Fact]
    public void RedirectToRoute_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = TypedResults.RedirectToRoute();

        // Assert
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(), result.RouteValues);
    }

    [Fact]
    public void RedirectToRoute_WithRouteValues_ResultHasCorrectValues()
    {
        // Arrange
        var routeValues = new { foo = 123 };

        // Act
        var result = TypedResults.RedirectToRoute(routeValues: routeValues);

        // Assert
        Assert.Null(result.RouteName);
        Assert.Equal(new RouteValueDictionary(routeValues), result.RouteValues);
    }

    [Fact]
    public void RedirectToRoute_WithNullRouteNameAndRouteValues_ResultHasCorrectValues()
    {
        // Arrange

        // Act
        var result = TypedResults.RedirectToRoute(null, null);

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
        var result = TypedResults.StatusCode(statusCode);

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
        var result = TypedResults.Text(content, contentType);

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
        var result = TypedResults.Text(content, contentType);

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
        var result = TypedResults.Text(content, contentType, statusCode);

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
        var result = TypedResults.Text(content, contentType, encoding);

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
        var result = TypedResults.Text(content, contentType, encoding, statusCode);

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
        var result = TypedResults.Unauthorized();

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
    }

    [Fact]
    public void UnprocessableEntity_ResultHasCorrectValues()
    {
        // Act
        var result = TypedResults.UnprocessableEntity();

        // Assert
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, result.StatusCode);
    }

    [Fact]
    public void InternalServerError_WithValue_ResultHasCorrectValues()
    {
        // Arrange
        var value = new { };

        // Act
        var result = TypedResults.InternalServerError(value);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void InternalServerError_WithNoArgs_ResultHasCorrectValues()
    {
        // Act
        var result = TypedResults.InternalServerError();

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
    }

    [JsonSerializable(typeof(object))]
    private partial class ObjectJsonContext : JsonSerializerContext
    { }
}
