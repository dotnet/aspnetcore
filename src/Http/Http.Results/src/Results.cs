// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Result;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// A factory for <see cref="IResult"/>.
    /// </summary>
    public static class Results
    {
        #region SignIn / SignOut / Challenge

        /// <summary>
        /// Creates an <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.ChallengeAsync(HttpContext)"/>.
        /// <para>
        /// The behavior of this method depends on the <see cref="IAuthenticationService"/> in use.
        /// <see cref="StatusCodes.Status401Unauthorized"/> and <see cref="StatusCodes.Status403Forbidden"/>
        /// are among likely status results.
        /// </para>
        /// </summary>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Challenge()
            => new ChallengeResult();

        /// <summary>
        /// Creates an <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.ChallengeAsync(HttpContext, string?)"/>.
        /// <para>
        /// The behavior of this method depends on the <see cref="IAuthenticationService"/> in use.
        /// <see cref="StatusCodes.Status401Unauthorized"/> and <see cref="StatusCodes.Status403Forbidden"/>
        /// are among likely status results.
        /// </para>
        /// </summary>
        /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Challenge(params string[] authenticationSchemes)
            => new ChallengeResult { AuthenticationSchemes = authenticationSchemes };

        /// <summary>
        /// Creates an <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.ChallengeAsync(HttpContext, AuthenticationProperties?)" />.
        /// <para>
        /// The behavior of this method depends on the <see cref="IAuthenticationService"/> in use.
        /// <see cref="StatusCodes.Status401Unauthorized"/> and <see cref="StatusCodes.Status403Forbidden"/>
        /// are among likely status results.
        /// </para>
        /// </summary>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Challenge(AuthenticationProperties properties)
            => new ChallengeResult { Properties = properties };

        /// <summary>
        /// Creates an <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.ChallengeAsync(HttpContext, string?, AuthenticationProperties?)" />.
        /// <para>
        /// The behavior of this method depends on the <see cref="IAuthenticationService"/> in use.
        /// <see cref="StatusCodes.Status401Unauthorized"/> and <see cref="StatusCodes.Status403Forbidden"/>
        /// are among likely status results.
        /// </para>
        /// </summary>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Challenge(
            AuthenticationProperties properties,
            params string[] authenticationSchemes)
            => new ChallengeResult { AuthenticationSchemes = authenticationSchemes, Properties = properties };

        /// <summary>
        /// Creates an <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.SignInAsync(HttpContext, ClaimsPrincipal)"/>.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/> containing the user claims.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult SignIn(ClaimsPrincipal principal)
            => new SignInResult(principal);

        /// <summary>
        /// Creates an <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.SignInAsync(HttpContext, string?, ClaimsPrincipal)" />.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/> containing the user claims.</param>
        /// <param name="authenticationScheme">The authentication scheme to use for the sign-in operation.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult SignIn(ClaimsPrincipal principal, string authenticationScheme)
            => new SignInResult(authenticationScheme, principal);

        /// <summary>
        /// Creates an <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.SignInAsync(HttpContext, ClaimsPrincipal, AuthenticationProperties?)" />.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/> containing the user claims.</param>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-in operation.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult SignIn(
            ClaimsPrincipal principal,
            AuthenticationProperties properties)
            => new SignInResult(principal, properties);

        /// <summary>
        /// Creates an <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.SignInAsync(HttpContext, string?, ClaimsPrincipal, AuthenticationProperties?)" />.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/> containing the user claims.</param>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-in operation.</param>
        /// <param name="authenticationScheme">The authentication scheme to use for the sign-in operation.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult SignIn(
            ClaimsPrincipal principal,
            AuthenticationProperties properties,
            string authenticationScheme)
            => new SignInResult(authenticationScheme, principal, properties);

        /// <summary>
        /// Creates an <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.SignOutAsync(HttpContext)" />.
        /// </summary>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult SignOut()
            => new SignOutResult();

        /// <summary>
        /// Creates an <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.SignOutAsync(HttpContext, AuthenticationProperties?)" />.
        /// </summary>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-out operation.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult SignOut(AuthenticationProperties properties)
            => new SignOutResult(properties);

        /// <summary>
        /// Creates an <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.SignOutAsync(HttpContext, string?)" />.
        /// </summary>
        /// <param name="authenticationSchemes">The authentication schemes to use for the sign-out operation.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult SignOut(params string[] authenticationSchemes)
            => new SignOutResult(authenticationSchemes);

        /// <summary>
        /// Creates an <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.SignOutAsync(HttpContext, string?, AuthenticationProperties?)" />.
        /// </summary>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-out operation.</param>
        /// <param name="authenticationSchemes">The authentication scheme to use for the sign-out operation.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult SignOut(AuthenticationProperties properties, params string[] authenticationSchemes)
            => new SignOutResult(authenticationSchemes, properties);
        #endregion

        #region ContentResult
        /// <summary>
        /// Writes the <paramref name="content"/> string to the HTTP response.
        /// </summary>
        /// <param name="content">The content to write to the response.</param>
        /// <returns>The created <see cref="IResult"/> object for the response.</returns>
        public static IResult Content(string content)
            => Content(content, (MediaTypeHeaderValue?)null);

        /// <summary>
        /// Writes the <paramref name="content"/> string to the HTTP response.
        /// </summary>
        /// <param name="content">The content to write to the response.</param>
        /// <param name="contentType">The content type (MIME type).</param>
        /// <returns>The created <see cref="IResult"/> object for the response.</returns>
        public static IResult Content(string content, string contentType)
            => Content(content, MediaTypeHeaderValue.Parse(contentType));

        /// <summary>
        /// Writes the <paramref name="content"/> string to the HTTP response.
        /// </summary>
        /// <param name="content">The content to write to the response.</param>
        /// <param name="contentType">The content type (MIME type).</param>
        /// <param name="contentEncoding">The content encoding.</param>
        /// <returns>The created <see cref="IResult"/> object for the response.</returns>
        /// <remarks>
        /// If encoding is provided by both the 'charset' and the <paramref name="contentEncoding"/> parameters, then
        /// the <paramref name="contentEncoding"/> parameter is chosen as the final encoding.
        /// </remarks>
        public static IResult Content(string content, string contentType, Encoding contentEncoding)
        {
            var mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(contentType);
            mediaTypeHeaderValue.Encoding = contentEncoding ?? mediaTypeHeaderValue.Encoding;
            return Content(content, mediaTypeHeaderValue);
        }

        /// <summary>
        /// Writes the <paramref name="content"/> string to the HTTP response.
        /// </summary>
        /// <param name="content">The content to write to the response.</param>
        /// <param name="contentType">The content type (MIME type).</param>
        /// <returns>The created <see cref="IResult"/> object for the response.</returns>
        public static IResult Content(string content, MediaTypeHeaderValue? contentType)
        {
            return new ContentResult
            {
                Content = content,
                ContentType = contentType?.ToString()
            };
        }
        #endregion

        #region ForbidResult
        /// <summary>
        /// Creates a <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.ForbidAsync(HttpContext)"/>.
        /// <para>
        /// By default, executing this result returns a <see cref="StatusCodes.Status403Forbidden"/>. Some authentication schemes, such as cookies,
        /// will convert <see cref="StatusCodes.Status403Forbidden"/> to a redirect to show a login page.
        /// </para>
        /// </summary>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Forbid()
            => new ForbidResult();

        /// <summary>
        /// Creates a <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.ForbidAsync(HttpContext, string?)"/>.
        /// <para>
        /// By default, executing this result returns a <see cref="StatusCodes.Status403Forbidden"/>. Some authentication schemes, such as cookies,
        /// will convert <see cref="StatusCodes.Status403Forbidden"/> to a redirect to show a login page.
        /// </para>
        /// </summary>
        /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        /// <remarks>
        /// Some authentication schemes, such as cookies, will convert <see cref="StatusCodes.Status403Forbidden"/> to
        /// a redirect to show a login page.
        /// </remarks>
        public static IResult Forbid(params string[] authenticationSchemes)
            => new ForbidResult { AuthenticationSchemes = authenticationSchemes };

        /// <summary>
        /// Creates a <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.ForbidAsync(HttpContext, AuthenticationProperties?)"/>.
        /// <para>
        /// By default, executing this result returns a <see cref="StatusCodes.Status403Forbidden"/>. Some authentication schemes, such as cookies,
        /// will convert <see cref="StatusCodes.Status403Forbidden"/> to a redirect to show a login page.
        /// </para>
        /// </summary>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        /// <remarks>
        /// Some authentication schemes, such as cookies, will convert <see cref="StatusCodes.Status403Forbidden"/> to
        /// a redirect to show a login page.
        /// </remarks>
        public static IResult Forbid(AuthenticationProperties properties)
            => new ForbidResult { Properties = properties };

        /// <summary>
        /// Creates a <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.ForbidAsync(HttpContext, string?, AuthenticationProperties?)"/>.
        /// <para>
        /// By default, executing this result returns a <see cref="StatusCodes.Status403Forbidden"/>. Some authentication schemes, such as cookies,
        /// will convert <see cref="StatusCodes.Status403Forbidden"/> to a redirect to show a login page.
        /// </para>
        /// </summary>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the authentication
        /// challenge.</param>
        /// <param name="authenticationSchemes">The authentication schemes to challenge.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        /// <remarks>
        /// Some authentication schemes, such as cookies, will convert <see cref="StatusCodes.Status403Forbidden"/> to
        /// a redirect to show a login page.
        /// </remarks>
        public static IResult Forbid(AuthenticationProperties properties, params string[] authenticationSchemes)
            => new ForbidResult { Properties = properties, AuthenticationSchemes = authenticationSchemes, };
        #endregion

        /// <summary>
        /// Creates a <see cref="IResult"/> that serializes the specified <paramref name="data"/> object to JSON.
        /// </summary>
        /// <param name="data">The object to write as JSON.</param>
        /// <param name="options">The serializer options use when serializing the value.</param>
        /// <param name="contentType">The content-type to set on the response.</param>
        /// <param name="statusCode">The status code to set on the response.</param>
        /// <returns>The created <see cref="JsonResult"/> that serializes the specified <paramref name="data"/>
        /// as JSON format for the response.</returns>
        /// <remarks>Callers should cache an instance of serializer settings to avoid
        /// recreating cached data with each call.</remarks>
        public static IResult Json(object? data, JsonSerializerOptions? options = null, string? contentType = null, int? statusCode = null)
        {
            return new JsonResult
            {
                Value = data,
                JsonSerializerOptions = options,
                ContentType = contentType,
                StatusCode = statusCode,
            };
        }

        #region FileContentResult
        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>),
        /// and the specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(byte[] fileContents, string contentType)
            => File(fileContents, contentType, fileDownloadName: null);

        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>),
        /// and the specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(byte[] fileContents, string contentType, bool enableRangeProcessing)
            => File(fileContents, contentType, fileDownloadName: null, enableRangeProcessing: enableRangeProcessing);

        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>), the
        /// specified <paramref name="contentType" /> as the Content-Type and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(byte[] fileContents, string contentType, string? fileDownloadName)
            => new FileContentResult(fileContents, contentType) { FileDownloadName = fileDownloadName };

        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>), the
        /// specified <paramref name="contentType" /> as the Content-Type and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(byte[] fileContents, string contentType, string? fileDownloadName, bool enableRangeProcessing)
            => new FileContentResult(fileContents, contentType)
            {
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };

        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>),
        /// and the specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(byte[] fileContents, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new FileContentResult(fileContents, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
            };
        }

        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>),
        /// and the specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(byte[] fileContents, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new FileContentResult(fileContents, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }

        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>), the
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(byte[] fileContents, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new FileContentResult(fileContents, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
            };
        }

        /// <summary>
        /// Returns a file with the specified <paramref name="fileContents" /> as content (<see cref="StatusCodes.Status200OK"/>), the
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileContents">The file contents.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(byte[] fileContents, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new FileContentResult(fileContents, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }
        #endregion

        #region FileStreamResult
        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>), with the
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>
        public static IResult File(Stream fileStream, string contentType)
            => File(fileStream, contentType, fileDownloadName: null);

        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>), with the
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>
        public static IResult File(Stream fileStream, string contentType, bool enableRangeProcessing)
            => File(fileStream, contentType, fileDownloadName: null, enableRangeProcessing: enableRangeProcessing);

        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type and the
        /// specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>
        public static IResult File(Stream fileStream, string contentType, string? fileDownloadName)
            => new FileStreamResult(fileStream, contentType) { FileDownloadName = fileDownloadName };

        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type and the
        /// specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>
        public static IResult File(Stream fileStream, string contentType, string? fileDownloadName, bool enableRangeProcessing)
            => new FileStreamResult(fileStream, contentType)
            {
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };

        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>),
        /// and the specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>
        public static IResult File(Stream fileStream, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new FileStreamResult(fileStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
            };
        }

        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>),
        /// and the specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>
        public static IResult File(Stream fileStream, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new FileStreamResult(fileStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }

        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>), the
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>
        public static IResult File(Stream fileStream, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new FileStreamResult(fileStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
            };
        }

        /// <summary>
        /// Returns a file in the specified <paramref name="fileStream" /> (<see cref="StatusCodes.Status200OK"/>), the
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>
        public static IResult File(Stream fileStream, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new FileStreamResult(fileStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }
        #endregion

        #region PhysicalFileResult
        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult PhysicalFile(string physicalPath, string contentType)
            => PhysicalFile(physicalPath, contentType, fileDownloadName: null);

        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult PhysicalFile(string physicalPath, string contentType, bool enableRangeProcessing)
            => PhysicalFile(physicalPath, contentType, fileDownloadName: null, enableRangeProcessing: enableRangeProcessing);

        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type and the
        /// specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult PhysicalFile(
            string physicalPath,
            string contentType,
            string? fileDownloadName)
            => new PhysicalFileResult(physicalPath, contentType) { FileDownloadName = fileDownloadName };

        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type and the
        /// specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult PhysicalFile(
            string physicalPath,
            string contentType,
            string? fileDownloadName,
            bool enableRangeProcessing)
            => new PhysicalFileResult(physicalPath, contentType)
            {
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };

        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>), and
        /// the specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult PhysicalFile(string physicalPath, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new PhysicalFileResult(physicalPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
            };
        }

        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>), and
        /// the specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult PhysicalFile(string physicalPath, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new PhysicalFileResult(physicalPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }

        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>), the
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult PhysicalFile(string physicalPath, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new PhysicalFileResult(physicalPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
            };
        }

        /// <summary>
        /// Returns the file specified by <paramref name="physicalPath" /> (<see cref="StatusCodes.Status200OK"/>), the
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="physicalPath">The path to the file. The path must be an absolute path.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult PhysicalFile(string physicalPath, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new PhysicalFileResult(physicalPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }
        #endregion

        #region VirtualFileResult
        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(string virtualPath, string contentType)
            => File(virtualPath, contentType, fileDownloadName: null);

        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(string virtualPath, string contentType, bool enableRangeProcessing)
            => File(virtualPath, contentType, fileDownloadName: null, enableRangeProcessing: enableRangeProcessing);

        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type and the
        /// specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(string virtualPath, string contentType, string? fileDownloadName)
            => new VirtualFileResult(virtualPath, contentType) { FileDownloadName = fileDownloadName };

        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>) with the
        /// specified <paramref name="contentType" /> as the Content-Type and the
        /// specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(string virtualPath, string contentType, string? fileDownloadName, bool enableRangeProcessing)
            => new VirtualFileResult(virtualPath, contentType)
            {
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };

        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>), and the 
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(string virtualPath, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new VirtualFileResult(virtualPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
            };
        }

        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>), and the 
        /// specified <paramref name="contentType" /> as the Content-Type.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(string virtualPath, string contentType, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new VirtualFileResult(virtualPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }

        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>), the 
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(string virtualPath, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag)
        {
            return new VirtualFileResult(virtualPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
            };
        }

        /// <summary>
        /// Returns the file specified by <paramref name="virtualPath" /> (<see cref="StatusCodes.Status200OK"/>), the 
        /// specified <paramref name="contentType" /> as the Content-Type, and the specified <paramref name="fileDownloadName" /> as the suggested file name.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// </summary>
        /// <param name="virtualPath">The virtual path of the file to be returned.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The suggested file name.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> associated with the file.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult File(string virtualPath, string contentType, string? fileDownloadName, DateTimeOffset? lastModified, EntityTagHeaderValue entityTag, bool enableRangeProcessing)
        {
            return new VirtualFileResult(virtualPath, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }
        #endregion

        #region RedirectResult variants
        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>)
        /// to the specified <paramref name="url"/>.
        /// </summary>
        /// <param name="url">The URL to redirect to.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Redirect(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Argument cannot be null or empty.", nameof(url));
            }

            return new RedirectResult(url);
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently" />)
        /// to the specified <paramref name="url"/>.
        /// </summary>
        /// <param name="url">The URL to redirect to.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult RedirectPermanent(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Argument cannot be null or empty.", nameof(url));
            }

            return new RedirectResult(url, permanent: true);
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status307TemporaryRedirect" />)
        /// to the specified <paramref name="url"/> preserving the HTTP method.
        /// </summary>
        /// <param name="url">The URL to redirect to.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult RedirectPreserveMethod(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Argument cannot be null or empty.", nameof(url));
            }

            return new RedirectResult(url: url, permanent: false, preserveMethod: true);
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status308PermanentRedirect" />)
        /// to the specified <paramref name="url"/> preserving the HTTP method.
        /// </summary>
        /// <param name="url">The URL to redirect to.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult RedirectPermanentPreserveMethod(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentException("Argument cannot be null or empty.", nameof(url));
            }

            return new RedirectResult(url: url, permanent: true, preserveMethod: true);
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>)
        /// to the specified <paramref name="localUrl"/>.
        /// </summary>
        /// <param name="localUrl">The local URL to redirect to.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult LocalRedirect(string localUrl)
        {
            if (string.IsNullOrEmpty(localUrl))
            {
                throw new ArgumentException("Argument cannot be null or empty.", nameof(localUrl));
            }

            return new LocalRedirectResult(localUrl);
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently" />)
        /// to the specified <paramref name="localUrl"/>.
        /// </summary>
        /// <param name="localUrl">The local URL to redirect to.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult LocalRedirectPermanent(string localUrl)
        {
            if (string.IsNullOrEmpty(localUrl))
            {
                throw new ArgumentException("Argument cannot be null or empty.", nameof(localUrl));
            }

            return new LocalRedirectResult(localUrl, permanent: true);
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status307TemporaryRedirect" />)
        /// to the specified <paramref name="localUrl"/> preserving the HTTP method.
        /// </summary>
        /// <param name="localUrl">The local URL to redirect to.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult LocalRedirectPreserveMethod(string localUrl)
        {
            if (string.IsNullOrEmpty(localUrl))
            {
                throw new ArgumentException("Argument cannot be null or empty.", nameof(localUrl));
            }

            return new LocalRedirectResult(localUrl: localUrl, permanent: false, preserveMethod: true);
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status308PermanentRedirect" />)
        /// to the specified <paramref name="localUrl"/> preserving the HTTP method.
        /// </summary>
        /// <param name="localUrl">The local URL to redirect to.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult LocalRedirectPermanentPreserveMethod(string localUrl)
        {
            if (string.IsNullOrEmpty(localUrl))
            {
                throw new ArgumentException("Argument cannot be null or empty.", nameof(localUrl));
            }

            return new LocalRedirectResult(localUrl: localUrl, permanent: true, preserveMethod: true);
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified route using the specified <paramref name="routeName"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult RedirectToRoute(string? routeName)
            => RedirectToRoute(routeName, routeValues: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified route using the specified <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult RedirectToRoute(object? routeValues)
            => RedirectToRoute(routeName: null, routeValues: routeValues);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified route using the specified
        /// <paramref name="routeName"/> and <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult RedirectToRoute(string? routeName, object? routeValues)
            => RedirectToRoute(routeName, routeValues, fragment: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified route using the specified
        /// <paramref name="routeName"/> and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult RedirectToRoute(string? routeName, string? fragment)
            => RedirectToRoute(routeName, routeValues: null, fragment: fragment);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status302Found"/>) to the specified route using the specified
        /// <paramref name="routeName"/>, <paramref name="routeValues"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult RedirectToRoute(
            string? routeName,
            object? routeValues,
            string? fragment)
        {
            return new RedirectToRouteResult(routeName, routeValues, fragment);
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status307TemporaryRedirect"/>) to the specified route with 
        /// <see cref="RedirectToRouteResult.Permanent"/> set to false and <see cref="RedirectToRouteResult.PreserveMethod"/>
        /// set to true, using the specified <paramref name="routeName"/>, <paramref name="routeValues"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>       
        public static IResult RedirectToRoutePreserveMethod(
            string? routeName = null,
            object? routeValues = null,
            string? fragment = null)
        {
            return new RedirectToRouteResult(
                routeName: routeName,
                routeValues: routeValues,
                permanent: false,
                preserveMethod: true,
                fragment: fragment);
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified route with 
        /// <see cref="RedirectToRouteResult.Permanent"/> set to true using the specified <paramref name="routeName"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult RedirectToRoutePermanent(string? routeName)
            => RedirectToRoutePermanent(routeName, routeValues: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified route with 
        /// <see cref="RedirectToRouteResult.Permanent"/> set to true using the specified <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult RedirectToRoutePermanent(object? routeValues)
            => RedirectToRoutePermanent(routeName: null, routeValues: routeValues);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified route with
        /// <see cref="RedirectToRouteResult.Permanent"/> set to true using the specified <paramref name="routeName"/>
        /// and <paramref name="routeValues"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult RedirectToRoutePermanent(string? routeName, object? routeValues)
            => RedirectToRoutePermanent(routeName, routeValues, fragment: null);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified route with 
        /// <see cref="RedirectToRouteResult.Permanent"/> set to true using the specified <paramref name="routeName"/>
        /// and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult RedirectToRoutePermanent(string? routeName, string? fragment)
            => RedirectToRoutePermanent(routeName, routeValues: null, fragment: fragment);

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status301MovedPermanently"/>) to the specified route with
        /// <see cref="RedirectToRouteResult.Permanent"/> set to true using the specified <paramref name="routeName"/>,
        /// <paramref name="routeValues"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult RedirectToRoutePermanent(
            string? routeName,
            object? routeValues,
            string? fragment)
        {
            return new RedirectToRouteResult(routeName, routeValues, permanent: true, fragment: fragment);
        }

        /// <summary>
        /// Redirects (<see cref="StatusCodes.Status308PermanentRedirect"/>) to the specified route with
        /// <see cref="RedirectToRouteResult.Permanent"/> set to true and <see cref="RedirectToRouteResult.PreserveMethod"/>
        /// set to true, using the specified <paramref name="routeName"/>, <paramref name="routeValues"/>, and <paramref name="fragment"/>.
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>       
        public static IResult RedirectToRoutePermanentPreserveMethod(
            string? routeName = null,
            object? routeValues = null,
            string? fragment = null)
        {
            return new RedirectToRouteResult(
                routeName: routeName,
                routeValues: routeValues,
                permanent: true,
                preserveMethod: true,
                fragment: fragment);
        }
        #endregion

        /// <summary>
        /// Creates a <see cref="StatusCodeResult"/> object by specifying a <paramref name="statusCode"/>.
        /// </summary>
        /// <param name="statusCode">The status code to set on the response.</param>
        /// <returns>The created <see cref="StatusCodeResult"/> object for the response.</returns>
        public static IResult StatusCode(int statusCode)
            => new StatusCodeResult(statusCode);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status404NotFound"/> response.
        /// </summary>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult NotFound()
            => new NotFoundResult();

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status404NotFound"/> response.
        /// </summary>
        /// <param name="value">The value to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult NotFound(object? value)
            => new NotFoundObjectResult(value);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status401Unauthorized"/> response.
        /// </summary>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Unauthorized()
            => new UnauthorizedResult();

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status400BadRequest"/> response.
        /// </summary>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult BadRequest()
            => new BadRequestResult();

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status400BadRequest"/> response.
        /// </summary>
        /// <param name="error">An error object to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult BadRequest(object? error)
            => new BadRequestObjectResult(error);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status409Conflict"/> response.
        /// </summary>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Conflict()
            => new ConflictResult();

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status409Conflict"/> response.
        /// </summary>
        /// <param name="error">An error object to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Conflict(object? error)
            => new ConflictObjectResult(error);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status204NoContent"/> response.
        /// </summary>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult NoContent()
            => new NoContentResult();

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status200OK"/> response.
        /// </summary>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Ok()
            => new OkResult();

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status200OK"/> response.
        /// </summary>
        /// <param name="value">The value to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Ok(object? value)
            => new OkObjectResult(value);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status422UnprocessableEntity"/> response.
        /// </summary>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult UnprocessableEntity()
            => new UnprocessableEntityResult();

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status422UnprocessableEntity"/> response.
        /// </summary>
        /// <param name="error">An error object to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult UnprocessableEntity(object? error)
            => new UnprocessableEntityObjectResult(error);

        /// <summary>
        /// Produces a <see cref="ProblemDetails"/> response.
        /// </summary>
        /// <param name="statusCode">The value for <see cref="ProblemDetails.Status" />.</param>
        /// <param name="detail">The value for <see cref="ProblemDetails.Detail" />.</param>
        /// <param name="instance">The value for <see cref="ProblemDetails.Instance" />.</param>
        /// <param name="title">The value for <see cref="ProblemDetails.Title" />.</param>
        /// <param name="type">The value for <see cref="ProblemDetails.Type" />.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Problem(
            string? detail = null,
            string? instance = null,
            int? statusCode = null,
            string? title = null,
            string? type = null)
        {
            return new ObjectResult(new ProblemDetails
            {
                Detail = detail,
                Instance = instance,
                Status = statusCode,
                Title = title,
                Type = type
            });
        }

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status400BadRequest"/> response
        /// with a <see cref="HttpValidationProblemDetails"/> value.
        /// </summary>
        /// <param name="errors">One or more validation errors.</param>
        /// <param name="detail">The value for <see cref="ProblemDetails.Detail" />.</param>
        /// <param name="instance">The value for <see cref="ProblemDetails.Instance" />.</param>
        /// <param name="statusCode">The status code.</param>
        /// <param name="title">The value for <see cref="ProblemDetails.Title" />.</param>
        /// <param name="type">The value for <see cref="ProblemDetails.Type" />.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult ValidationProblem(
            IDictionary<string, string[]> errors,
            string? detail = null,
            string? instance = null,
            int? statusCode = null,
            string? title = null,
            string? type = null)
        {
            var problemDetails = new HttpValidationProblemDetails(errors)
            {
                Detail = detail,
                Instance = instance,
                Title = title,
                Type = type,
                Status = statusCode,
            };

            return new ObjectResult(problemDetails);
        }

        #region CreatedResult
        /// <summary>
        /// Produces a <see cref="StatusCodes.Status201Created"/> response.
        /// </summary>
        /// <param name="uri">The URI at which the content has been created.</param>
        /// <param name="value">The value to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Created(string uri, object? value)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return new CreatedResult(uri, value);
        }

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status201Created"/> response.
        /// </summary>
        /// <param name="uri">The URI at which the content has been created.</param>
        /// <param name="value">The value to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Created(Uri uri, object? value)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return new CreatedResult(uri, value);
        }

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status201Created"/> response.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        /// <param name="value">The value to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult CreatedAtRoute(string? routeName, object? value)
            => CreatedAtRoute(routeName, routeValues: null, value: value);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status201Created"/> response.
        /// </summary>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The value to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult CreatedAtRoute(object? routeValues, object? value)
            => CreatedAtRoute(routeName: null, routeValues: routeValues, value: value);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status201Created"/> response.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The value to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult CreatedAtRoute(string? routeName, object? routeValues, object? value)
            => new CreatedAtRouteResult(routeName, routeValues, value);

        #endregion

        #region AcceptedResult
        /// <summary>
        /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Accepted()
            => new AcceptedResult();

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="value">The optional content value to format in the response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Accepted(object? value)
            => new AcceptedResult(location: null, value: value);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="uri">The optional URI with the location at which the status of requested content can be monitored.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Accepted(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return new AcceptedResult(locationUri: uri, value: null);
        }

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="uri">The optional URI with the location at which the status of requested content can be monitored.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Accepted(string? uri)
            => new AcceptedResult(location: uri, value: null);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="uri">The URI with the location at which the status of requested content can be monitored.</param>
        /// <param name="value">The optional content value to format in the response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Accepted(string? uri, object? value)
            => new AcceptedResult(uri, value);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="uri">The URI with the location at which the status of requested content can be monitored.</param>
        /// <param name="value">The optional content value to format in the response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Accepted(Uri uri, object? value)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return new AcceptedResult(locationUri: uri, value: value);
        }

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult AcceptedAtRoute(object? routeValues)
            => AcceptedAtRoute(routeName: null, routeValues: routeValues, value: null);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult AcceptedAtRoute(string? routeName)
            => AcceptedAtRoute(routeName, routeValues: null, value: null);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        ///<param name="routeValues">The route data to use for generating the URL.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult AcceptedAtRoute(string? routeName, object? routeValues)
            => AcceptedAtRoute(routeName, routeValues, value: null);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The optional content value to format in the response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult AcceptedAtRoute(object? routeValues, object? value)
            => AcceptedAtRoute(routeName: null, routeValues: routeValues, value: value);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The optional content value to format in the response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult AcceptedAtRoute(string? routeName, object? routeValues, object? value)
            => new AcceptedAtRouteResult(routeName, routeValues, value);
        #endregion
    }
}
