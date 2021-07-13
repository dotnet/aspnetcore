// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
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
            AuthenticationProperties? properties = null,
            IList<string>? authenticationSchemes = null)
            => new ChallengeResult { AuthenticationSchemes = authenticationSchemes ?? Array.Empty<string>(), Properties = properties };

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
        public static IResult Forbid(AuthenticationProperties? properties = null, IList<string>? authenticationSchemes = null)
            => new ForbidResult { Properties = properties, AuthenticationSchemes = authenticationSchemes ?? Array.Empty<string>(), };

        /// <summary>
        /// Creates an <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.SignInAsync(HttpContext, string?, ClaimsPrincipal, AuthenticationProperties?)" />.
        /// </summary>
        /// <param name="principal">The <see cref="ClaimsPrincipal"/> containing the user claims.</param>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-in operation.</param>
        /// <param name="authenticationScheme">The authentication scheme to use for the sign-in operation.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult SignIn(
            ClaimsPrincipal principal,
            AuthenticationProperties? properties = null,
            string? authenticationScheme = null)
            => new SignInResult(authenticationScheme, principal, properties);

        /// <summary>
        /// Creates an <see cref="IResult"/> that on execution invokes <see cref="AuthenticationHttpContextExtensions.SignOutAsync(HttpContext, string?, AuthenticationProperties?)" />.
        /// </summary>
        /// <param name="properties"><see cref="AuthenticationProperties"/> used to perform the sign-out operation.</param>
        /// <param name="authenticationSchemes">The authentication scheme to use for the sign-out operation.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult SignOut(AuthenticationProperties? properties = null, IList<string>? authenticationSchemes = null)
            => new SignOutResult(authenticationSchemes ?? Array.Empty<string>(), properties);

        /// <summary>
        /// Writes the <paramref name="content"/> string to the HTTP response.
        /// <para>
        /// This is an alias for <see cref="Text(string, string?, Encoding?)"/>.
        /// </para>
        /// </summary>
        /// <param name="content">The content to write to the response.</param>
        /// <param name="contentType">The content type (MIME type).</param>
        /// <param name="contentEncoding">The content encoding.</param>
        /// <returns>The created <see cref="IResult"/> object for the response.</returns>
        /// <remarks>
        /// If encoding is provided by both the 'charset' and the <paramref name="contentEncoding"/> parameters, then
        /// the <paramref name="contentEncoding"/> parameter is chosen as the final encoding.
        /// </remarks>
        public static IResult Content(string content, string? contentType = null, Encoding? contentEncoding = null)
            => Text(content, contentType, contentEncoding);

        /// <summary>
        /// Writes the <paramref name="content"/> string to the HTTP response.
        /// <para>
        /// This is an alias for <see cref="Content(string, string?, Encoding?)"/>.
        /// </para>
        /// </summary>
        /// <param name="content">The content to write to the response.</param>
        /// <param name="contentType">The content type (MIME type).</param>
        /// <param name="contentEncoding">The content encoding.</param>
        /// <returns>The created <see cref="IResult"/> object for the response.</returns>
        /// <remarks>
        /// If encoding is provided by both the 'charset' and the <paramref name="contentEncoding"/> parameters, then
        /// the <paramref name="contentEncoding"/> parameter is chosen as the final encoding.
        /// </remarks>
        public static IResult Text(string content, string? contentType = null, Encoding? contentEncoding = null)
        {
            MediaTypeHeaderValue? mediaTypeHeaderValue = null;
            if (contentType is not null)
            {
                mediaTypeHeaderValue = MediaTypeHeaderValue.Parse(contentType);
                mediaTypeHeaderValue.Encoding = contentEncoding ?? mediaTypeHeaderValue.Encoding;
            }

            return new ContentResult
            {
                Content = content,
                ContentType = mediaTypeHeaderValue?.ToString()
            };
        }

        /// <summary>
        /// Writes the <paramref name="content"/> string to the HTTP response.
        /// </summary>
        /// <param name="content">The content to write to the response.</param>
        /// <param name="contentType">The content type (MIME type).</param>
        /// <returns>The created <see cref="IResult"/> object for the response.</returns>
        public static IResult Content(string content, MediaTypeHeaderValue contentType)
            => new ContentResult
            {
                Content = content,
                ContentType = contentType.ToString()
            };

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
            => new JsonResult
            {
                Value = data,
                JsonSerializerOptions = options,
                ContentType = contentType,
                StatusCode = statusCode,
            };

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
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static IResult File(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            byte[] fileContents,
            string? contentType = null,
            string? fileDownloadName = null,
            bool enableRangeProcessing = false,
            DateTimeOffset? lastModified = null,
            EntityTagHeaderValue? entityTag = null)
            => new FileContentResult(fileContents, contentType)
            {
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
                LastModified = lastModified,
                EntityTag = entityTag,
            };

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
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Bytes(
            byte[] contents,
            string? contentType = null,
            string? fileDownloadName = null,
            bool enableRangeProcessing = false,
            DateTimeOffset? lastModified = null,
            EntityTagHeaderValue? entityTag = null)
            => new FileContentResult(contents, contentType)
            {
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
                LastModified = lastModified,
                EntityTag = entityTag,
            };


        /// <summary>
        /// Writes the specified <see cref="Stream"/> to the response.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// <para>
        /// This API is an alias for <see cref="Stream(Stream, string, string?, DateTimeOffset?, EntityTagHeaderValue?, bool)"/>.
        /// </para>
        /// </summary>
        /// <param name="fileStream">The <see cref="Stream"/> with the contents of the file.</param>
        /// <param name="contentType">The Content-Type of the file.</param>
        /// <param name="fileDownloadName">The the file name to be used in the <c>Content-Disposition</c> header.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.
        /// Used to configure the <c>Last-Modified</c> response header and perform conditional range requests.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> to be configure the <c>ETag</c> response header
        /// and perform conditional requests.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="fileStream" /> parameter is disposed after the response is sent.
        /// </remarks>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static IResult File(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            Stream fileStream,
            string? contentType = null,
            string? fileDownloadName = null,
            DateTimeOffset? lastModified = null,
            EntityTagHeaderValue? entityTag = null,
            bool enableRangeProcessing = false)
        {
            return new FileStreamResult(fileStream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
                EnableRangeProcessing = enableRangeProcessing,
            };
        }

        /// <summary>
        /// Writes the specified <see cref="Stream"/> to the response.
        /// <para>
        /// This supports range requests (<see cref="StatusCodes.Status206PartialContent"/> or
        /// <see cref="StatusCodes.Status416RangeNotSatisfiable"/> if the range is not satisfiable).
        /// </para>
        /// <para>
        /// This API is an alias for <see cref="File(Stream, string, string?, DateTimeOffset?, EntityTagHeaderValue?, bool)"/>.
        /// </para>
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to write to the response.</param>
        /// <param name="contentType">The <c>Content-Type</c> of the response. Defaults to <c>application/octet-stream</c>.</param>
        /// <param name="fileDownloadName">The the file name to be used in the <c>Content-Disposition</c> header.</param>
        /// <param name="lastModified">The <see cref="DateTimeOffset"/> of when the file was last modified.
        /// Used to configure the <c>Last-Modified</c> response header and perform conditional range requests.</param>
        /// <param name="entityTag">The <see cref="EntityTagHeaderValue"/> to be configure the <c>ETag</c> response header
        /// and perform conditional requests.</param>
        /// <param name="enableRangeProcessing">Set to <c>true</c> to enable range requests processing.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        /// <remarks>
        /// The <paramref name="stream" /> parameter is disposed after the response is sent.
        /// </remarks>
        public static IResult Stream(
            Stream stream,
            string? contentType = null,
            string? fileDownloadName = null,
            DateTimeOffset? lastModified = null,
            EntityTagHeaderValue? entityTag = null,
            bool enableRangeProcessing = false)
        {
            return new FileStreamResult(stream, contentType)
            {
                LastModified = lastModified,
                EntityTag = entityTag,
                FileDownloadName = fileDownloadName,
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
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static IResult File(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            string path,
            string? contentType = null,
            string? fileDownloadName = null,
            DateTimeOffset? lastModified = null,
            EntityTagHeaderValue? entityTag = null,
            bool enableRangeProcessing = false)
        {
            if (Path.IsPathRooted(path))
            {
                return new PhysicalFileResult(path, contentType)
                {
                    FileDownloadName = fileDownloadName,
                    LastModified = lastModified,
                    EntityTag = entityTag,
                    EnableRangeProcessing = enableRangeProcessing,
                };
            }
            else
            {
                return new VirtualFileResult(path, contentType)
                {
                    FileDownloadName = fileDownloadName,
                    LastModified = lastModified,
                    EntityTag = entityTag,
                    EnableRangeProcessing = enableRangeProcessing,
                };
            }
        }

        /// <summary>
        /// Redirects to the specified <paramref name="url"/>.
        /// <list type="bullet">
        /// <item>When <paramref name="permanent"/> and <paramref name="preserveMethod"/> are set, sets the <see cref="StatusCodes.Status308PermanentRedirect"/> status code.</item>
        /// <item>When <paramref name="preserveMethod"/> is set, sets the <see cref="StatusCodes.Status307TemporaryRedirect"/> status code.</item>
        /// <item>When <paramref name="permanent"/> is set, sets the <see cref="StatusCodes.Status301MovedPermanently"/> status code.</item>
        /// <item>Otherwise, configures <see cref="StatusCodes.Status302Found"/>.</item>
        /// </list>
        /// </summary>
        /// <param name="url">The URL to redirect to.</param>
        /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
        /// <param name="preserveMethod">If set to true, make the temporary redirect (307) or permanent redirect (308) preserve the initial request method.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Redirect(string url, bool permanent = false, bool preserveMethod = false)
            => new RedirectResult(url, permanent, preserveMethod);

        /// <summary>
        /// Redirects to the specified <paramref name="localUrl"/>.
        /// <list type="bullet">
        /// <item>When <paramref name="permanent"/> and <paramref name="preserveMethod"/> are set, sets the <see cref="StatusCodes.Status308PermanentRedirect"/> status code.</item>
        /// <item>When <paramref name="preserveMethod"/> is set, sets the <see cref="StatusCodes.Status307TemporaryRedirect"/> status code.</item>
        /// <item>When <paramref name="permanent"/> is set, sets the <see cref="StatusCodes.Status301MovedPermanently"/> status code.</item>
        /// <item>Otherwise, configures <see cref="StatusCodes.Status302Found"/>.</item>
        /// </list>
        /// </summary>
        /// <param name="localUrl">The local URL to redirect to.</param>
        /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
        /// <param name="preserveMethod">If set to true, make the temporary redirect (307) or permanent redirect (308) preserve the initial request method.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult LocalRedirect(string localUrl, bool permanent = false, bool preserveMethod = false)
            => new LocalRedirectResult(localUrl, permanent, preserveMethod);
 
        /// <summary>
        /// Redirects to the specified route.
        /// <list type="bullet">
        /// <item>When <paramref name="permanent"/> and <paramref name="preserveMethod"/> are set, sets the <see cref="StatusCodes.Status308PermanentRedirect"/> status code.</item>
        /// <item>When <paramref name="preserveMethod"/> is set, sets the <see cref="StatusCodes.Status307TemporaryRedirect"/> status code.</item>
        /// <item>When <paramref name="permanent"/> is set, sets the <see cref="StatusCodes.Status301MovedPermanently"/> status code.</item>
        /// <item>Otherwise, configures <see cref="StatusCodes.Status302Found"/>.</item>
        /// </list>
        /// </summary>
        /// <param name="routeName">The name of the route.</param>
        /// <param name="routeValues">The parameters for a route.</param>
        /// <param name="permanent">Specifies whether the redirect should be permanent (301) or temporary (302).</param>
        /// <param name="preserveMethod">If set to true, make the temporary redirect (307) or permanent redirect (308) preserve the initial request method.</param>
        /// <param name="fragment">The fragment to add to the URL.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult RedirectToRoute(string? routeName = null, object? routeValues = null, bool permanent = false, bool preserveMethod = false, string? fragment = null)
            => new RedirectToRouteResult(
                routeName: routeName,
                routeValues: routeValues,
                permanent: permanent,
                preserveMethod: preserveMethod,
                fragment: fragment);

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
        /// <param name="value">The value to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult NotFound(object? value = null)
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
        /// <param name="error">An error object to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult BadRequest(object? error = null)
            => new BadRequestObjectResult(error);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status409Conflict"/> response.
        /// </summary>
        /// <param name="error">An error object to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Conflict(object? error = null)
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
        /// <param name="value">The value to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Ok(object? value = null)
            => new OkObjectResult(value);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status422UnprocessableEntity"/> response.
        /// </summary>
        /// <param name="error">An error object to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult UnprocessableEntity(object? error = null)
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
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The value to be included in the HTTP response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult CreatedAtRoute(string? routeName = null, object? routeValues = null, object? value = null)
            => new CreatedAtRouteResult(routeName, routeValues, value);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="uri">The URI with the location at which the status of requested content can be monitored.</param>
        /// <param name="value">The optional content value to format in the response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult Accepted(string? uri = null, object? value = null)
            => new AcceptedResult(uri, value);

        /// <summary>
        /// Produces a <see cref="StatusCodes.Status202Accepted"/> response.
        /// </summary>
        /// <param name="routeName">The name of the route to use for generating the URL.</param>
        /// <param name="routeValues">The route data to use for generating the URL.</param>
        /// <param name="value">The optional content value to format in the response body.</param>
        /// <returns>The created <see cref="IResult"/> for the response.</returns>
        public static IResult AcceptedAtRoute(string? routeName = null, object? routeValues = null, object? value = null)
            => new AcceptedAtRouteResult(routeName, routeValues, value);
    }
}
