// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// A typed factory for <see cref="IResult"/> types in <see cref="Microsoft.AspNetCore.Http.HttpResults"/>.
/// </summary>
public static class TypedResults
{
    /// <summary>
    /// Creates a <see cref="ChallengeHttpResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.ChallengeAsync(HttpContext, string?, AuthenticationProperties?)" />.
    /// <para>
    /// The behavior of this method depends on the <see cref="IAuthenticationService"/> in use.
    /// <see cref="StatusCodes.Status401Unauthorized"/> and <see cref="StatusCodes.Status403Forbidden"/>
    /// are among likely status results.
    /// </para>
    /// </summary>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
    /// challenge.</param>
    /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
    /// <returns>The created <see cref="ChallengeHttpResult"/> for the response.</returns>
    public static ChallengeHttpResult Challenge(
        AuthenticationProperties? properties = null,
        IList<string>? authenticationSchemes = null)
        => new(authenticationSchemes: authenticationSchemes ?? Array.Empty<string>(), properties);

    /// <summary>
    /// Creates a <see cref="ForbidHttpResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.ForbidAsync(HttpContext, string?, AuthenticationProperties?)"/>.
    /// <para>
    /// By default, executing this result returns a <see cref="StatusCodes.Status403Forbidden"/>. Some authentication schemes, such as cookies,
    /// will convert <see cref="StatusCodes.Status403Forbidden"/> to a redirect to show a login page.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Some authentication schemes, such as cookies, will convert <see cref="StatusCodes.Status403Forbidden"/> to
    /// a redirect to show a login page.
    /// </remarks>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
    /// challenge.</param>
    /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
    /// <returns>The created <see cref="ForbidHttpResult"/> for the response.</returns>
    public static ForbidHttpResult Forbid(AuthenticationProperties? properties = null, IList<string>? authenticationSchemes = null)
        => new(authenticationSchemes: authenticationSchemes ?? Array.Empty<string>(), properties);

    /// <summary>
    /// Creates a <see cref="SignInHttpResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.SignInAsync(HttpContext, string?, ClaimsPrincipal, AuthenticationProperties?)" />.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> containing the user claims.</param>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-in operation.</param>
    /// <param name="authenticationScheme">The authentication scheme to use for the sign-in operation.</param>
    /// <returns>The created <see cref="SignInHttpResult"/> for the response.</returns>
    public static SignInHttpResult SignIn(
        ClaimsPrincipal principal,
        AuthenticationProperties? properties = null,
        string? authenticationScheme = null)
    {
        ArgumentNullException.ThrowIfNull(principal);

        return new(principal, authenticationScheme, properties);
    }

    /// <summary>
    /// Creates a <see cref="SignOutHttpResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.SignOutAsync(HttpContext, string?, AuthenticationProperties?)" />.
    /// </summary>
    /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-out operation.</param>
    /// <param name="authenticationSchemes">The authentication scheme to use for the sign-out operation.</param>
    /// <returns>The created <see cref="SignOutHttpResult"/> for the response.</returns>
    public static SignOutHttpResult SignOut(AuthenticationProperties? properties = null, IList<string>? authenticationSchemes = null)
        => new(authenticationSchemes ?? Array.Empty<string>(), properties);

    /// <summary>
    /// Writes the <paramref name="content"/> string to the HTTP response.
    /// <para>
    /// This is equivalent to <see cref="Text(string?, string?, Encoding?)"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// If encoding is provided by both the 'charset' and the <paramref name="contentEncoding"/> parameters, then
    /// the <paramref name="contentEncoding"/> parameter is chosen as the final encoding.
    /// </remarks>
    /// <param name="content">The content to write to the response.</param>
    /// <param name="contentType">The content type (MIME type).</param>
    /// <param name="contentEncoding">The content encoding.</param>
    /// <returns>The created <see cref="ContentHttpResult"/> object for the response.</returns>
    public static ContentHttpResult Content(string? content, string? contentType, Encoding? contentEncoding)
        => Content(content, contentType, contentEncoding, null);

    /// <summary>
    /// Writes the <paramref name="content"/> string to the HTTP response.
    /// <para>
    /// This is equivalent to <see cref="Text(string?, string?, Encoding?, int?)"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// If encoding is provided by both the 'charset' and the <paramref name="contentEncoding"/> parameters, then
    /// the <paramref name="contentEncoding"/> parameter is chosen as the final encoding.
    /// </remarks>
    /// <param name="content">The content to write to the response.</param>
    /// <param name="contentType">The content type (MIME type).</param>
    /// <param name="contentEncoding">The content encoding.</param>
    /// <param name="statusCode">The status code to return.</param>
    /// <returns>The created <see cref="ContentHttpResult"/> object for the response.</returns>
    public static ContentHttpResult Content(string? content, string? contentType = null, Encoding? contentEncoding = null, int? statusCode = null)
        => Text(content, contentType, contentEncoding, statusCode);

    /// <summary>
    /// Writes the <paramref name="content"/> string to the HTTP response.
    /// <para>
    /// This is an alias for <see cref="Content(string?, string?, Encoding?)"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// If encoding is provided by both the 'charset' and the <paramref name="contentEncoding"/> parameters, then
    /// the <paramref name="contentEncoding"/> parameter is chosen as the final encoding.
    /// </remarks>
    /// <param name="content">The content to write to the response.</param>
    /// <param name="contentType">The content type (MIME type).</param>
    /// <param name="contentEncoding">The content encoding.</param>
    /// <returns>The created <see cref="ContentHttpResult"/> object for the response.</returns>
    public static ContentHttpResult Text(string? content, string? contentType, Encoding? contentEncoding)
        => Text(content, contentType, contentEncoding, null);

    /// <summary>
    /// Writes the <paramref name="utf8Content"/> UTF8 text content to the HTTP response.
    /// </summary>
    /// <param name="utf8Content">The content to write to the response.</param>
    /// <param name="contentType">The content type (MIME type).</param>
    /// <param name="statusCode">The status code to return.</param>
    /// <returns>The created <see cref="Utf8ContentHttpResult"/> object for the response.</returns>
    public static Utf8ContentHttpResult Text(ReadOnlySpan<byte> utf8Content, string? contentType = null, int? statusCode = null)
        => new Utf8ContentHttpResult(utf8Content, contentType, statusCode);

    /// <summary>
    /// Writes the <paramref name="content"/> string to the HTTP response.
    /// <para>
    /// This is an alias for <see cref="Content(string?, string?, Encoding?, int?)"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// If encoding is provided by both the 'charset' and the <paramref name="contentEncoding"/> parameters, then
    /// the <paramref name="contentEncoding"/> parameter is chosen as the final encoding.
    /// </remarks>
    /// <param name="content">The content to write to the response.</param>
    /// <param name="contentType">The content type (MIME type).</param>
    /// <param name="contentEncoding">The content encoding.</param>
    /// <param name="statusCode">The status code to return.</param>
    /// <returns>The created <see cref="ContentHttpResult"/> object for the response.</returns>
    public static ContentHttpResult Text(string? content, string? contentType = null, Encoding? contentEncoding = null, int? statusCode = null)
    {
        MediaTypeHeaderValue? mediaTypeHeaderValue = null;
        if (contentType is not null)
        {
            mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(contentType);
            mediaTypeHeaderValue.Encoding = contentEncoding ?? mediaTypeHeaderValue.Encoding;
        }

        return new(content, mediaTypeHeaderValue?.ToString(), statusCode);
    }

    /// <summary>
    /// Writes the <paramref name="content"/> string to the HTTP response.
    /// </summary>
    /// <param name="content">The content to write to the response.</param>
    /// <param name="contentType">The content type (MIME type).</param>
    /// <returns>The created <see cref="ContentHttpResult"/> object for the response.</returns>
    public static ContentHttpResult Content(string? content, MediaTypeHeaderValue contentType)
        => new(content, contentType.ToString());

    /// <summary>
    /// Creates a <see cref="JsonHttpResult{TValue}"/> that serializes the specified <paramref name="data"/> object to JSON.
    /// </summary>
    /// <remarks>Callers should cache an instance of serializer settings to avoid
    /// recreating cached data with each call.</remarks>
    /// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
    /// <param name="data">The object to write as JSON.</param>
    /// <param name="options">The serializer options to use when serializing the value.</param>
    /// <param name="contentType">The content-type to set on the response.</param>
    /// <param name="statusCode">The status code to set on the response.</param>
    /// <returns>The created <see cref="JsonHttpResult{TValue}"/> that serializes the specified <paramref name="data"/>
    /// as JSON format for the response.</returns>
    [RequiresUnreferencedCode(JsonHttpResultTrimmerWarning.SerializationUnreferencedCodeMessage)]
    [RequiresDynamicCode(JsonHttpResultTrimmerWarning.SerializationRequiresDynamicCodeMessage)]
    public static JsonHttpResult<TValue> Json<TValue>(TValue? data, JsonSerializerOptions? options = null, string? contentType = null, int? statusCode = null)
        => new(data, options, statusCode, contentType);

    /// <summary>
    /// Creates a <see cref="JsonHttpResult{TValue}"/> that serializes the specified <paramref name="data"/> object to JSON.
    /// </summary>
    /// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
    /// <param name="data">The object to write as JSON.</param>
    /// <param name="jsonTypeInfo">Metadata about the type to convert.</param>
    /// <param name="contentType">The content-type to set on the response.</param>
    /// <param name="statusCode">The status code to set on the response.</param>
    /// <returns>The created <see cref="JsonHttpResult{TValue}"/> that serializes the specified <paramref name="data"/>
    /// as JSON format for the response.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static JsonHttpResult<TValue> Json<TValue>(TValue? data, JsonTypeInfo<TValue> jsonTypeInfo, string? contentType = null, int? statusCode = null)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
    {
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);
        return new(data, statusCode, contentType) { JsonTypeInfo = jsonTypeInfo };
    }

    /// <summary>
    /// Creates a <see cref="JsonHttpResult{TValue}"/> that serializes the specified <paramref name="data"/> object to JSON.
    /// </summary>
    /// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
    /// <param name="data">The object to write as JSON.</param>
    /// <param name="context">A metadata provider for serializable types.</param>
    /// <param name="contentType">The content-type to set on the response.</param>
    /// <param name="statusCode">The status code to set on the response.</param>
    /// <returns>The created <see cref="JsonHttpResult{TValue}"/> that serializes the specified <paramref name="data"/>
    /// as JSON format for the response.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static JsonHttpResult<TValue> Json<TValue>(TValue? data, JsonSerializerContext context, string? contentType = null, int? statusCode = null)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
    {
        ArgumentNullException.ThrowIfNull(context);
        return new(data, statusCode, contentType)
        {
            JsonTypeInfo = context.GetRequiredTypeInfo(typeof(TValue))
        };
    }

    /// <summary>
    /// Writes the byte-array content to the response.
    /// <para>
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </para>
    /// <para>
    /// This API is an alias for <see cref="Bytes(byte[], string, string?, bool, DateTimeOffset?, EntityTagHeaderValue?)"/>.</para>
    /// </summary>
    /// <param name="fileContents">The file contents.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <returns>The created <see cref="FileContentHttpResult"/> for the response.</returns>
    public static FileContentHttpResult File(
        byte[] fileContents,
        string? contentType = null,
        string? fileDownloadName = null,
        bool enableRangeProcessing = false,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue? entityTag = null)
    {
        ArgumentNullException.ThrowIfNull(fileContents);

        return new(fileContents, contentType)
        {
            FileDownloadName = fileDownloadName,
            EnableRangeProcessing = enableRangeProcessing,
            LastModified = lastModified,
            EntityTag = entityTag,
        };
    }

    /// <summary>
    /// Writes the byte-array content to the response.
    /// <para>
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </para>
    /// <para>
    /// This API is an alias for <see cref="File(byte[], string, string?, bool, DateTimeOffset?, EntityTagHeaderValue?)"/>.</para>
    /// </summary>
    /// <param name="contents">The file contents.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <returns>The created <see cref="FileContentHttpResult"/> for the response.</returns>
    public static FileContentHttpResult Bytes(
        byte[] contents,
        string? contentType = null,
        string? fileDownloadName = null,
        bool enableRangeProcessing = false,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue? entityTag = null)
    {
        ArgumentNullException.ThrowIfNull(contents);

        return new(contents, contentType)
        {
            FileDownloadName = fileDownloadName,
            EnableRangeProcessing = enableRangeProcessing,
            LastModified = lastModified,
            EntityTag = entityTag,
        };
    }

    /// <summary>
    /// Writes the byte-array content to the response.
    /// <para>
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </para>
    /// </summary>
    /// <param name="contents">The file contents.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <returns>The created <see cref="FileContentHttpResult"/> for the response.</returns>
    public static FileContentHttpResult Bytes(
        ReadOnlyMemory<byte> contents,
        string? contentType = null,
        string? fileDownloadName = null,
        bool enableRangeProcessing = false,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue? entityTag = null)
        => new(contents, contentType)
        {
            FileDownloadName = fileDownloadName,
            EnableRangeProcessing = enableRangeProcessing,
            LastModified = lastModified,
            EntityTag = entityTag,
        };

    /// <summary>
    /// Writes the specified <see cref="System.IO.Stream"/> to the response.
    /// <para>
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </para>
    /// <para>
    /// This API is an alias for <see cref="Stream(Stream, string, string?, DateTimeOffset?, EntityTagHeaderValue?, bool)"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
    /// </remarks>
    /// <param name="fileStream">The <see cref="System.IO.Stream"/> with the contents of the file.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The file name to be used in the <c>Content-Disposition</c> header.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.
    /// Used to configure the <c>Last-Modified</c> response header and perform conditional range requests.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> to be configure the <c>ETag</c> response header
    /// and perform conditional requests.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="FileStreamHttpResult"/> for the response.</returns>
    public static FileStreamHttpResult File(
        Stream fileStream,
        string? contentType = null,
        string? fileDownloadName = null,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue? entityTag = null,
        bool enableRangeProcessing = false)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        return new(fileStream, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            FileDownloadName = fileDownloadName,
            EnableRangeProcessing = enableRangeProcessing,
        };
    }

    /// <summary>
    /// Writes the specified <see cref="System.IO.Stream"/> to the response.
    /// <para>
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </para>
    /// <para>
    /// This API is an alias for <see cref="File(Stream, string, string?, DateTimeOffset?, EntityTagHeaderValue?, bool)"/>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// The <paramref name="stream" /> parameter is disposed after the response is sent.
    /// </remarks>
    /// <param name="stream">The <see cref="System.IO.Stream"/> to write to the response.</param>
    /// <param name="contentType">The <c>Content-Type</c> of the response. Defaults to <c>application/octet-stream</c>.</param>
    /// <param name="fileDownloadName">The file name to be used in the <c>Content-Disposition</c> header.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.
    /// Used to configure the <c>Last-Modified</c> response header and perform conditional range requests.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> to be configure the <c>ETag</c> response header
    /// and perform conditional requests.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="FileStreamHttpResult"/> for the response.</returns>
    public static FileStreamHttpResult Stream(
        Stream stream,
        string? contentType = null,
        string? fileDownloadName = null,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue? entityTag = null,
        bool enableRangeProcessing = false)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return new(stream, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            FileDownloadName = fileDownloadName,
            EnableRangeProcessing = enableRangeProcessing,
        };
    }

    /// <summary>
    /// Writes the contents of the specified <see cref="System.IO.Pipelines.PipeReader"/> to the response.
    /// <para>
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </para>
    /// </summary>
    /// <remarks>
    /// The <paramref name="pipeReader" /> parameter is completed after the response is sent.
    /// </remarks>
    /// <param name="pipeReader">The <see cref="System.IO.Pipelines.PipeReader"/> to write to the response.</param>
    /// <param name="contentType">The <c>Content-Type</c> of the response. Defaults to <c>application/octet-stream</c>.</param>
    /// <param name="fileDownloadName">The file name to be used in the <c>Content-Disposition</c> header.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.
    /// Used to configure the <c>Last-Modified</c> response header and perform conditional range requests.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> to be configure the <c>ETag</c> response header
    /// and perform conditional requests.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="FileStreamHttpResult"/> for the response.</returns>
    public static FileStreamHttpResult Stream(
        PipeReader pipeReader,
        string? contentType = null,
        string? fileDownloadName = null,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue? entityTag = null,
        bool enableRangeProcessing = false)
    {
        ArgumentNullException.ThrowIfNull(pipeReader);

        return new(pipeReader.AsStream(), contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            FileDownloadName = fileDownloadName,
            EnableRangeProcessing = enableRangeProcessing,
        };
    }

    /// <summary>
    /// Allows writing directly to the response body.
    /// </summary>
    /// <param name="streamWriterCallback">The callback that allows users to write directly to the response body.</param>
    /// <param name="contentType">The <c>Content-Type</c> of the response. Defaults to <c>application/octet-stream</c>.</param>
    /// <param name="fileDownloadName">The file name to be used in the <c>Content-Disposition</c> header.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.
    /// Used to configure the <c>Last-Modified</c> response header and perform conditional range requests.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> to be configure the <c>ETag</c> response header
    /// and perform conditional requests.</param>
    /// <returns>The created <see cref="PushStreamHttpResult"/> for the response.</returns>
    public static PushStreamHttpResult Stream(
        Func<Stream, Task> streamWriterCallback,
        string? contentType = null,
        string? fileDownloadName = null,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue? entityTag = null)
    {
        ArgumentNullException.ThrowIfNull(streamWriterCallback);

        return new(streamWriterCallback, contentType)
        {
            LastModified = lastModified,
            EntityTag = entityTag,
            FileDownloadName = fileDownloadName,
        };
    }

    /// <summary>
    /// Writes the file at the specified <paramref name="path"/> to the response.
    /// <para>
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </para>
    /// </summary>
    /// <param name="path">The path to the file. When not rooted, resolves the path relative to <see cref="IWebHostEnvironment.WebRootFileProvider"/>.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="PhysicalFileHttpResult"/> for the response.</returns>
    public static PhysicalFileHttpResult PhysicalFile(
        string path,
        string? contentType = null,
        string? fileDownloadName = null,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue? entityTag = null,
        bool enableRangeProcessing = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        return new(path, contentType)
        {
            FileDownloadName = fileDownloadName,
            LastModified = lastModified,
            EntityTag = entityTag,
            EnableRangeProcessing = enableRangeProcessing,
        };
    }

    /// <summary>
    /// Writes the file at the specified <paramref name="path"/> to the response.
    /// <para>
    /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
    /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
    /// </para>
    /// </summary>
    /// <param name="path">The path to the file. When not rooted, resolves the path relative to <see cref="IWebHostEnvironment.WebRootFileProvider"/>.</param>
    /// <param name="contentType">The Content-Type of the file.</param>
    /// <param name="fileDownloadName">The suggested file name.</param>
    /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
    /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
    /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
    /// <returns>The created <see cref="VirtualFileHttpResult"/> for the response.</returns>
    public static VirtualFileHttpResult VirtualFile(
        string path,
        string? contentType = null,
        string? fileDownloadName = null,
        DateTimeOffset? lastModified = null,
        EntityTagHeaderValue? entityTag = null,
        bool enableRangeProcessing = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        return new(path, contentType)
        {
            FileDownloadName = fileDownloadName,
            LastModified = lastModified,
            EntityTag = entityTag,
            EnableRangeProcessing = enableRangeProcessing,
        };
    }

    /// <summary>
    /// Redirects to the specified <paramref name="url"/>.
    /// <list type="bullet">
    /// <item><description>When <paramref name="permanent"/> and <paramref name="preserveMethod"/> are set, sets the <see cref="StatusCodes.Status308PermanentRedirect"/> status code.</description></item>
    /// <item><description>When <paramref name="preserveMethod"/> is set, sets the <see cref="StatusCodes.Status307TemporaryRedirect"/> status code.</description></item>
    /// <item><description>When <paramref name="permanent"/> is set, sets the <see cref="StatusCodes.Status301MovedPermanently"/> status code.</description></item>
    /// <item>Otherwise, configures <see cref="StatusCodes.Status302Found"/>.</item>
    /// </list>
    /// </summary>
    /// <param name="url">The URL to redirect to.</param>
    /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
    /// <param name="preserveMethod">If set to true, make the temporary redirect (307) or permanent redirect (308) preserve the initial request method.</param>
    /// <returns>The created <see cref="RedirectHttpResult"/> for the response.</returns>
    public static RedirectHttpResult Redirect([StringSyntax(StringSyntaxAttribute.Uri)] string url, bool permanent = false, bool preserveMethod = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(url);

        return new(url, permanent, preserveMethod);
    }

    /// <summary>
    /// Redirects to the specified <paramref name="localUrl"/>.
    /// <list type="bullet">
    /// <item><description>When <paramref name="permanent"/> and <paramref name="preserveMethod"/> are set, sets the <see cref="StatusCodes.Status308PermanentRedirect"/> status code.</description></item>
    /// <item><description>When <paramref name="preserveMethod"/> is set, sets the <see cref="StatusCodes.Status307TemporaryRedirect"/> status code.</description></item>
    /// <item><description>When <paramref name="permanent"/> is set, sets the <see cref="StatusCodes.Status301MovedPermanently"/> status code.</description></item>
    /// <item>Otherwise, configures <see cref="StatusCodes.Status302Found"/>.</item>
    /// </list>
    /// </summary>
    /// <param name="localUrl">The local URL to redirect to.</param>
    /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
    /// <param name="preserveMethod">If set to true, make the temporary redirect (307) or permanent redirect (308) preserve the initial request method.</param>
    /// <returns>The created <see cref="RedirectHttpResult"/> for the response.</returns>
    public static RedirectHttpResult LocalRedirect([StringSyntax(StringSyntaxAttribute.Uri, UriKind.Relative)] string localUrl, bool permanent = false, bool preserveMethod = false)
    {
        ArgumentException.ThrowIfNullOrEmpty(localUrl);

        return new(localUrl, acceptLocalUrlOnly: true, permanent, preserveMethod);
    }

    /// <summary>
    /// Redirects to the specified route.
    /// <list type="bullet">
    /// <item><description>When <paramref name="permanent"/> and <paramref name="preserveMethod"/> are set, sets the <see cref="StatusCodes.Status308PermanentRedirect"/> status code.</description></item>
    /// <item><description>When <paramref name="preserveMethod"/> is set, sets the <see cref="StatusCodes.Status307TemporaryRedirect"/> status code.</description></item>
    /// <item><description>When <paramref name="permanent"/> is set, sets the <see cref="StatusCodes.Status301MovedPermanently"/> status code.</description></item>
    /// <item><description>Otherwise, configures <see cref="StatusCodes.Status302Found"/>.</description></item>
    /// </list>
    /// </summary>
    /// <param name="routeName">The name of the route.</param>
    /// <param name="routeValues">The parameters for a route.</param>
    /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
    /// <param name="preserveMethod">If set to true, make the temporary redirect (307) or permanent redirect (308) preserve the initial request method.</param>
    /// <param name="fragment">The fragment to add to the URL.</param>
    /// <returns>The created <see cref="RedirectToRouteHttpResult"/> for the response.</returns>
    [RequiresUnreferencedCode(RouteValueDictionaryTrimmerWarning.Warning)]
    public static RedirectToRouteHttpResult RedirectToRoute(string? routeName = null, object? routeValues = null, bool permanent = false, bool preserveMethod = false, string? fragment = null)
        => new(
            routeName: routeName,
            routeValues: routeValues,
            permanent: permanent,
            preserveMethod: preserveMethod,
            fragment: fragment);

    /// <summary>
    /// Redirects to the specified route.
    /// <list type="bullet">
    /// <item><description>When <paramref name="permanent"/> and <paramref name="preserveMethod"/> are set, sets the <see cref="StatusCodes.Status308PermanentRedirect"/> status code.</description></item>
    /// <item><description>When <paramref name="preserveMethod"/> is set, sets the <see cref="StatusCodes.Status307TemporaryRedirect"/> status code.</description></item>
    /// <item><description>When <paramref name="permanent"/> is set, sets the <see cref="StatusCodes.Status301MovedPermanently"/> status code.</description></item>
    /// <item><description>Otherwise, configures <see cref="StatusCodes.Status302Found"/>.</description></item>
    /// </list>
    /// </summary>
    /// <param name="routeName">The name of the route.</param>
    /// <param name="routeValues">The parameters for a route.</param>
    /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
    /// <param name="preserveMethod">If set to true, make the temporary redirect (307) or permanent redirect (308) preserve the initial request method.</param>
    /// <param name="fragment">The fragment to add to the URL.</param>
    /// <returns>The created <see cref="RedirectToRouteHttpResult"/> for the response.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static RedirectToRouteHttpResult RedirectToRoute(string? routeName, RouteValueDictionary? routeValues, bool permanent = false, bool preserveMethod = false, string? fragment = null)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        => new(
            routeName: routeName,
            routeValues: routeValues,
            permanent: permanent,
            preserveMethod: preserveMethod,
            fragment: fragment);

    /// <summary>
    /// Creates a <see cref="StatusCodeHttpResult"/> object by specifying a <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The status code to set on the response.</param>
    /// <returns>The created <see cref="StatusCodeHttpResult"/> object for the response.</returns>
    public static StatusCodeHttpResult StatusCode(int statusCode)
        => ResultsCache.StatusCode(statusCode);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status404NotFound"/> response.
    /// </summary>
    /// <returns>The created <see cref="HttpResults.NotFound"/> for the response.</returns>
    public static NotFound NotFound() => ResultsCache.NotFound;

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status404NotFound"/> response.
    /// </summary>
    /// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
    /// <param name="value">The value to be included in the HTTP response body.</param>
    /// <returns>The created <see cref="HttpResults.NotFound{TValue}"/> for the response.</returns>
    public static NotFound<TValue> NotFound<TValue>(TValue? value) => new(value);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status401Unauthorized"/> response.
    /// </summary>
    /// <returns>The created <see cref="UnauthorizedHttpResult"/> for the response.</returns>
    public static UnauthorizedHttpResult Unauthorized() => ResultsCache.Unauthorized;

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status400BadRequest"/> response.
    /// </summary>
    /// <returns>The created <see cref="HttpResults.BadRequest"/> for the response.</returns>
    public static BadRequest BadRequest() => ResultsCache.BadRequest;

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status400BadRequest"/> response.
    /// </summary>
    /// <typeparam name="TValue">The type of error object that will be JSON serialized to the response body.</typeparam>
    /// <param name="error">The value to be included in the HTTP response body.</param>
    /// <returns>The created <see cref="HttpResults.BadRequest{TValue}"/> for the response.</returns>
    public static BadRequest<TValue> BadRequest<TValue>(TValue? error) => new(error);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status409Conflict"/> response.
    /// </summary>
    /// <returns>The created <see cref="HttpResults.Conflict"/> for the response.</returns>
    public static Conflict Conflict() => ResultsCache.Conflict;

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status409Conflict"/> response.
    /// </summary>
    /// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
    /// <param name="error">The value to be included in the HTTP response body.</param>
    /// <returns>The created <see cref="HttpResults.Conflict{TValue}"/> for the response.</returns>
    public static Conflict<TValue> Conflict<TValue>(TValue? error) => new(error);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status204NoContent"/> response.
    /// </summary>
    /// <returns>The created <see cref="HttpResults.NoContent"/> for the response.</returns>
    public static NoContent NoContent() => ResultsCache.NoContent;

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status200OK"/> response.
    /// </summary>
    /// <returns>The created <see cref="HttpResults.Ok"/> for the response.</returns>
    public static Ok Ok() => ResultsCache.Ok;

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status200OK"/> response.
    /// </summary>
    /// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
    /// <param name="value">The value to be included in the HTTP response body.</param>
    /// <returns>The created <see cref="HttpResults.Ok{TValue}"/> for the response.</returns>
    public static Ok<TValue> Ok<TValue>(TValue? value) => new(value);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status422UnprocessableEntity"/> response.
    /// </summary>
    /// <returns>The created <see cref="HttpResults.UnprocessableEntity"/> for the response.</returns>
    public static UnprocessableEntity UnprocessableEntity() => ResultsCache.UnprocessableEntity;

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status422UnprocessableEntity"/> response.
    /// </summary>
    /// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
    /// <param name="error">The value to be included in the HTTP response body.</param>
    /// <returns>The created <see cref="HttpResults.UnprocessableEntity{TValue}"/> for the response.</returns>
    public static UnprocessableEntity<TValue> UnprocessableEntity<TValue>(TValue? error) => new(error);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status500InternalServerError"/> response.
    /// </summary>
    /// <returns>The created <see cref="HttpResults.InternalServerError"/> for the response.</returns>
    public static InternalServerError InternalServerError() => ResultsCache.InternalServerError;

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status500InternalServerError"/> response.
    /// </summary>
    /// <typeparam name="TValue">The type of error object that will be JSON serialized to the response body.</typeparam>
    /// <param name="error">The value to be included in the HTTP response body.</param>
    /// <returns>The created <see cref="HttpResults.InternalServerError{TValue}"/> for the response.</returns>
    public static InternalServerError<TValue> InternalServerError<TValue>(TValue? error) => new(error);

    /// <summary>
    /// Produces a <see cref="ProblemDetails"/> response.
    /// </summary>
    /// <param name="statusCode">The value for <see cref="ProblemDetails.Status" />.</param>
    /// <param name="detail">The value for <see cref="ProblemDetails.Detail" />.</param>
    /// <param name="instance">The value for <see cref="ProblemDetails.Instance" />.</param>
    /// <param name="title">The value for <see cref="ProblemDetails.Title" />.</param>
    /// <param name="type">The value for <see cref="ProblemDetails.Type" />.</param>
    /// <param name="extensions">The value for <see cref="ProblemDetails.Extensions" />.</param>
    /// <returns>The created <see cref="ProblemHttpResult"/> for the response.</returns>
    public static ProblemHttpResult Problem(
        string? detail,
        string? instance,
        int? statusCode,
        string? title,
        string? type,
        IDictionary<string, object?>? extensions)
    {
        return Problem(detail, instance, statusCode, title, type, (IEnumerable<KeyValuePair<string, object?>>?)extensions);
    }

    /// <summary>
    /// Produces a <see cref="ProblemDetails"/> response.
    /// </summary>
    /// <param name="statusCode">The value for <see cref="ProblemDetails.Status" />.</param>
    /// <param name="detail">The value for <see cref="ProblemDetails.Detail" />.</param>
    /// <param name="instance">The value for <see cref="ProblemDetails.Instance" />.</param>
    /// <param name="title">The value for <see cref="ProblemDetails.Title" />.</param>
    /// <param name="type">The value for <see cref="ProblemDetails.Type" />.</param>
    /// <param name="extensions">The value for <see cref="ProblemDetails.Extensions" />.</param>
    /// <returns>The created <see cref="ProblemHttpResult"/> for the response.</returns>
#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
    public static ProblemHttpResult Problem(
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
        string? detail = null,
        string? instance = null,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        IEnumerable<KeyValuePair<string, object?>>? extensions = null)
    {
        var problemDetails = new ProblemDetails
        {
            Detail = detail,
            Instance = instance,
            Status = statusCode,
            Title = title,
            Type = type,
        };

        CopyExtensions(extensions, problemDetails);

        return new(problemDetails);
    }

    /// <summary>
    /// Produces a <see cref="ProblemDetails"/> response.
    /// </summary>
    /// <param name="problemDetails">The <see cref="ProblemDetails"/>  object to produce a response from.</param>
    /// <returns>The created <see cref="ProblemHttpResult"/> for the response.</returns>
    public static ProblemHttpResult Problem(ProblemDetails problemDetails)
    {
        ArgumentNullException.ThrowIfNull(problemDetails);

        return new(problemDetails);
    }

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status400BadRequest"/> response with an <see cref="HttpValidationProblemDetails"/> value.
    /// </summary>
    /// <param name="errors">One or more validation errors.</param>
    /// <param name="detail">The value for <see cref="ProblemDetails.Detail" />.</param>
    /// <param name="instance">The value for <see cref="ProblemDetails.Instance" />.</param>
    /// <param name="title">The value for <see cref="ProblemDetails.Title" />. Defaults to "One or more validation errors occurred."</param>
    /// <param name="type">The value for <see cref="ProblemDetails.Type" />.</param>
    /// <param name="extensions">The value for <see cref="ProblemDetails.Extensions" />.</param>
    /// <returns>The created <see cref="HttpResults.ValidationProblem"/> for the response.</returns>
    public static ValidationProblem ValidationProblem(
        IDictionary<string, string[]> errors,
        string? detail,
        string? instance,
        string? title,
        string? type,
        IDictionary<string, object?>? extensions)
    {
        return ValidationProblem(errors, detail, instance, title, type, (IEnumerable<KeyValuePair<string, object?>>?)extensions);
    }

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status400BadRequest"/> response with an <see cref="HttpValidationProblemDetails"/> value.
    /// </summary>
    /// <param name="errors">One or more validation errors.</param>
    /// <param name="detail">The value for <see cref="ProblemDetails.Detail" />.</param>
    /// <param name="instance">The value for <see cref="ProblemDetails.Instance" />.</param>
    /// <param name="title">The value for <see cref="ProblemDetails.Title" />. Defaults to "One or more validation errors occurred."</param>
    /// <param name="type">The value for <see cref="ProblemDetails.Type" />.</param>
    /// <param name="extensions">The value for <see cref="ProblemDetails.Extensions" />.</param>
    /// <returns>The created <see cref="HttpResults.ValidationProblem"/> for the response.</returns>
#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
    public static ValidationProblem ValidationProblem(
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
        IEnumerable<KeyValuePair<string, string[]>> errors,
        string? detail = null,
        string? instance = null,
        string? title = null,
        string? type = null,
        IEnumerable<KeyValuePair<string, object?>>? extensions = null)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var problemDetails = new HttpValidationProblemDetails(errors)
        {
            Detail = detail,
            Instance = instance,
            Type = type,
        };

        problemDetails.Title = title ?? problemDetails.Title;

        CopyExtensions(extensions, problemDetails);

        return new(problemDetails);
    }

    private static void CopyExtensions(IEnumerable<KeyValuePair<string, object?>>? extensions, ProblemDetails problemDetails)
    {
        if (extensions is not null)
        {
            foreach (var extension in extensions)
            {
                problemDetails.Extensions.Add(extension);
            }
        }
    }

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status201Created"/> response.
    /// </summary>
    /// <returns>The created <see cref="HttpResults.Created"/> for the response.</returns>
    public static Created Created()
    {
        return new(default(string));
    }

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status201Created"/> response.
    /// </summary>
    /// <param name="uri">The URI at which the content has been created.</param>
    /// <returns>The created <see cref="HttpResults.Created"/> for the response.</returns>
    public static Created Created(string? uri)
    {
        return new(uri);
    }

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status201Created"/> response.
    /// </summary>
    /// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
    /// <param name="uri">The URI at which the content has been created.</param>
    /// <param name="value">The value to be included in the HTTP response body.</param>
    /// <returns>The created <see cref="HttpResults.Created{TValue}"/> for the response.</returns>
    public static Created<TValue> Created<TValue>(string? uri, TValue? value)
    {
        return new(uri, value);
    }

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status201Created"/> response.
    /// </summary>
    /// <param name="uri">The URI at which the content has been created.</param>
    /// <returns>The created <see cref="HttpResults.Created"/> for the response.</returns>
    public static Created Created(Uri? uri)
    {
        return new(uri);
    }

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status201Created"/> response.
    /// </summary>
    /// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
    /// <param name="uri">The URI at which the content has been created.</param>
    /// <param name="value">The value to be included in the HTTP response body.</param>
    /// <returns>The created <see cref="HttpResults.Created{TValue}"/> for the response.</returns>
    public static Created<TValue> Created<TValue>(Uri? uri, TValue? value)
    {
        return new(uri, value);
    }

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status201Created"/> response.
    /// </summary>
    /// <param name="routeName">The name of the route to use for generating the URL.</param>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <returns>The created <see cref="HttpResults.CreatedAtRoute"/> for the response.</returns>
    [RequiresUnreferencedCode(RouteValueDictionaryTrimmerWarning.Warning)]
#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
    public static CreatedAtRoute CreatedAtRoute(string? routeName = null, object? routeValues = null)
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
        => new(routeName, routeValues);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status201Created"/> response.
    /// </summary>
    /// <param name="routeName">The name of the route to use for generating the URL.</param>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <returns>The created <see cref="HttpResults.CreatedAtRoute"/> for the response.</returns>
    public static CreatedAtRoute CreatedAtRoute(string? routeName, RouteValueDictionary? routeValues)
        => new(routeName, routeValues);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status201Created"/> response.
    /// </summary>
    /// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
    /// <param name="routeName">The name of the route to use for generating the URL.</param>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <param name="value">The value to be included in the HTTP response body.</param>
    /// <returns>The created <see cref="HttpResults.CreatedAtRoute{TValue}"/> for the response.</returns>
    [RequiresUnreferencedCode(RouteValueDictionaryTrimmerWarning.Warning)]
#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
    public static CreatedAtRoute<TValue> CreatedAtRoute<TValue>(TValue? value, string? routeName = null, object? routeValues = null)
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
        => new(routeName, routeValues, value);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status201Created"/> response.
    /// </summary>
    /// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
    /// <param name="routeName">The name of the route to use for generating the URL.</param>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <param name="value">The value to be included in the HTTP response body.</param>
    /// <returns>The created <see cref="HttpResults.CreatedAtRoute{TValue}"/> for the response.</returns>
    public static CreatedAtRoute<TValue> CreatedAtRoute<TValue>(TValue? value, string? routeName, RouteValueDictionary? routeValues)
        => new(routeName, routeValues, value);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
    /// </summary>
    /// <param name="uri">The URI with the location at which the status of requested content can be monitored.</param>
    /// <returns>The created <see cref="HttpResults.Accepted"/> for the response.</returns>
    public static Accepted Accepted(string? uri)
        => new(uri);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
    /// </summary>
    /// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
    /// <param name="uri">The URI with the location at which the status of requested content can be monitored.</param>
    /// <param name="value">The value to be included in the HTTP response body.</param>
    /// <returns>The created <see cref="HttpResults.Accepted{TValue}"/> for the response.</returns>
    public static Accepted<TValue> Accepted<TValue>(string? uri, TValue? value)
        => new(uri, value);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
    /// </summary>
    /// <param name="uri">The URI with the location at which the status of requested content can be monitored.</param>
    /// <returns>The created <see cref="HttpResults.Accepted"/> for the response.</returns>
    public static Accepted Accepted(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return new(uri);
    }

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
    /// </summary>
    /// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
    /// <param name="uri">The URI with the location at which the status of requested content can be monitored.</param>
    /// <param name="value">The value to be included in the HTTP response body.</param>
    /// <returns>The created <see cref="HttpResults.Accepted{TValue}"/> for the response.</returns>
    public static Accepted<TValue> Accepted<TValue>(Uri uri, TValue? value)
    {
        ArgumentNullException.ThrowIfNull(uri);

        return new(uri, value);
    }

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
    /// </summary>
    /// <param name="routeName">The name of the route to use for generating the URL.</param>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <returns>The created <see cref="HttpResults.AcceptedAtRoute"/> for the response.</returns>
    [RequiresUnreferencedCode(RouteValueDictionaryTrimmerWarning.Warning)]
#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
    public static AcceptedAtRoute AcceptedAtRoute(string? routeName = null, object? routeValues = null)
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
        => new(routeName, routeValues);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
    /// </summary>
    /// <param name="routeName">The name of the route to use for generating the URL.</param>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <returns>The created <see cref="HttpResults.AcceptedAtRoute"/> for the response.</returns>
    public static AcceptedAtRoute AcceptedAtRoute(string? routeName, RouteValueDictionary? routeValues)
        => new(routeName, routeValues);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
    /// </summary>
    /// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
    /// <param name="routeName">The name of the route to use for generating the URL.</param>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <param name="value">The value to be included in the HTTP response body.</param>
    /// <returns>The created <see cref="HttpResults.AcceptedAtRoute{TValue}"/> for the response.</returns>
    [RequiresUnreferencedCode(RouteValueDictionaryTrimmerWarning.Warning)]
#pragma warning disable RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
    public static AcceptedAtRoute<TValue> AcceptedAtRoute<TValue>(TValue? value, string? routeName = null, object? routeValues = null)
#pragma warning restore RS0027 // Public API with optional parameter(s) should have the most parameters amongst its public overloads
        => new(routeName, routeValues, value);

    /// <summary>
    /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
    /// </summary>
    /// <typeparam name="TValue">The type of object that will be JSON serialized to the response body.</typeparam>
    /// <param name="routeName">The name of the route to use for generating the URL.</param>
    /// <param name="routeValues">The route data to use for generating the URL.</param>
    /// <param name="value">The value to be included in the HTTP response body.</param>
    /// <returns>The created <see cref="HttpResults.AcceptedAtRoute{TValue}"/> for the response.</returns>
    public static AcceptedAtRoute<TValue> AcceptedAtRoute<TValue>(TValue? value, string? routeName, RouteValueDictionary? routeValues)
        => new(routeName, routeValues, value);

    /// <summary>
    /// Produces an empty result response, that when executed will do nothing.
    /// </summary>
    public static EmptyHttpResult Empty { get; } = EmptyHttpResult.Instance;

    /// <summary>
    /// Provides a container for external libraries to extend
    /// the default `TypedResults` set with their own samples.
    /// </summary>
    public static IResultExtensions Extensions { get; } = new ResultExtensions();
}
