// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>
/// Represents an <see cref="IResult"/> that when executed will
/// produce an HTTP response with the given response status code.
/// </summary>
public sealed partial class StatusCodeHttpResult : IResult
{
    internal static readonly IReadOnlyDictionary<int, StatusCodeHttpResult> KnownStatusCodes = new Dictionary<int, StatusCodeHttpResult>
    {
        [StatusCodes.Status100Continue] = new StatusCodeHttpResult(StatusCodes.Status100Continue),
        [StatusCodes.Status101SwitchingProtocols] = new StatusCodeHttpResult(StatusCodes.Status101SwitchingProtocols),
        [StatusCodes.Status102Processing] = new StatusCodeHttpResult(StatusCodes.Status102Processing),
        [StatusCodes.Status200OK] = new StatusCodeHttpResult(StatusCodes.Status200OK),
        [StatusCodes.Status201Created] = new StatusCodeHttpResult(StatusCodes.Status201Created),
        [StatusCodes.Status202Accepted] = new StatusCodeHttpResult(StatusCodes.Status202Accepted),
        [StatusCodes.Status203NonAuthoritative] = new StatusCodeHttpResult(StatusCodes.Status203NonAuthoritative),
        [StatusCodes.Status204NoContent] = new StatusCodeHttpResult(StatusCodes.Status204NoContent),
        [StatusCodes.Status205ResetContent] = new StatusCodeHttpResult(StatusCodes.Status205ResetContent),
        [StatusCodes.Status206PartialContent] = new StatusCodeHttpResult(StatusCodes.Status206PartialContent),
        [StatusCodes.Status207MultiStatus] = new StatusCodeHttpResult(StatusCodes.Status207MultiStatus),
        [StatusCodes.Status208AlreadyReported] = new StatusCodeHttpResult(StatusCodes.Status208AlreadyReported),
        [StatusCodes.Status226IMUsed] = new StatusCodeHttpResult(StatusCodes.Status226IMUsed),
        [StatusCodes.Status300MultipleChoices] = new StatusCodeHttpResult(StatusCodes.Status300MultipleChoices),
        [StatusCodes.Status301MovedPermanently] = new StatusCodeHttpResult(StatusCodes.Status301MovedPermanently),
        [StatusCodes.Status302Found] = new StatusCodeHttpResult(StatusCodes.Status302Found),
        [StatusCodes.Status303SeeOther] = new StatusCodeHttpResult(StatusCodes.Status303SeeOther),
        [StatusCodes.Status304NotModified] = new StatusCodeHttpResult(StatusCodes.Status304NotModified),
        [StatusCodes.Status305UseProxy] = new StatusCodeHttpResult(StatusCodes.Status305UseProxy),
        [StatusCodes.Status306SwitchProxy] = new StatusCodeHttpResult(StatusCodes.Status306SwitchProxy),
        [StatusCodes.Status307TemporaryRedirect] = new StatusCodeHttpResult(StatusCodes.Status307TemporaryRedirect),
        [StatusCodes.Status308PermanentRedirect] = new StatusCodeHttpResult(StatusCodes.Status308PermanentRedirect),
        [StatusCodes.Status400BadRequest] = new StatusCodeHttpResult(StatusCodes.Status400BadRequest),
        [StatusCodes.Status401Unauthorized] = new StatusCodeHttpResult(StatusCodes.Status401Unauthorized),
        [StatusCodes.Status402PaymentRequired] = new StatusCodeHttpResult(StatusCodes.Status402PaymentRequired),
        [StatusCodes.Status403Forbidden] = new StatusCodeHttpResult(StatusCodes.Status403Forbidden),
        [StatusCodes.Status404NotFound] = new StatusCodeHttpResult(StatusCodes.Status404NotFound),
        [StatusCodes.Status405MethodNotAllowed] = new StatusCodeHttpResult(StatusCodes.Status405MethodNotAllowed),
        [StatusCodes.Status406NotAcceptable] = new StatusCodeHttpResult(StatusCodes.Status406NotAcceptable),
        [StatusCodes.Status407ProxyAuthenticationRequired] = new StatusCodeHttpResult(StatusCodes.Status407ProxyAuthenticationRequired),
        [StatusCodes.Status408RequestTimeout] = new StatusCodeHttpResult(StatusCodes.Status408RequestTimeout),
        [StatusCodes.Status409Conflict] = new StatusCodeHttpResult(StatusCodes.Status409Conflict),
        [StatusCodes.Status410Gone] = new StatusCodeHttpResult(StatusCodes.Status410Gone),
        [StatusCodes.Status411LengthRequired] = new StatusCodeHttpResult(StatusCodes.Status411LengthRequired),
        [StatusCodes.Status412PreconditionFailed] = new StatusCodeHttpResult(StatusCodes.Status412PreconditionFailed),
        [StatusCodes.Status413RequestEntityTooLarge] = new StatusCodeHttpResult(StatusCodes.Status413RequestEntityTooLarge),
        [StatusCodes.Status413PayloadTooLarge] = new StatusCodeHttpResult(StatusCodes.Status413PayloadTooLarge),
        [StatusCodes.Status414RequestUriTooLong] = new StatusCodeHttpResult(StatusCodes.Status414RequestUriTooLong),
        [StatusCodes.Status414UriTooLong] = new StatusCodeHttpResult(StatusCodes.Status414UriTooLong),
        [StatusCodes.Status415UnsupportedMediaType] = new StatusCodeHttpResult(StatusCodes.Status415UnsupportedMediaType),
        [StatusCodes.Status416RequestedRangeNotSatisfiable] = new StatusCodeHttpResult(StatusCodes.Status416RequestedRangeNotSatisfiable),
        [StatusCodes.Status416RangeNotSatisfiable] = new StatusCodeHttpResult(StatusCodes.Status416RangeNotSatisfiable),
        [StatusCodes.Status417ExpectationFailed] = new StatusCodeHttpResult(StatusCodes.Status417ExpectationFailed),
        [StatusCodes.Status418ImATeapot] = new StatusCodeHttpResult(StatusCodes.Status418ImATeapot),
        [StatusCodes.Status419AuthenticationTimeout] = new StatusCodeHttpResult(StatusCodes.Status419AuthenticationTimeout),
        [StatusCodes.Status421MisdirectedRequest] = new StatusCodeHttpResult(StatusCodes.Status421MisdirectedRequest),
        [StatusCodes.Status422UnprocessableEntity] = new StatusCodeHttpResult(StatusCodes.Status422UnprocessableEntity),
        [StatusCodes.Status423Locked] = new StatusCodeHttpResult(StatusCodes.Status423Locked),
        [StatusCodes.Status424FailedDependency] = new StatusCodeHttpResult(StatusCodes.Status424FailedDependency),
        [StatusCodes.Status426UpgradeRequired] = new StatusCodeHttpResult(StatusCodes.Status426UpgradeRequired),
        [StatusCodes.Status428PreconditionRequired] = new StatusCodeHttpResult(StatusCodes.Status428PreconditionRequired),
        [StatusCodes.Status429TooManyRequests] = new StatusCodeHttpResult(StatusCodes.Status429TooManyRequests),
        [StatusCodes.Status431RequestHeaderFieldsTooLarge] = new StatusCodeHttpResult(StatusCodes.Status431RequestHeaderFieldsTooLarge),
        [StatusCodes.Status451UnavailableForLegalReasons] = new StatusCodeHttpResult(StatusCodes.Status451UnavailableForLegalReasons),
        [StatusCodes.Status500InternalServerError] = new StatusCodeHttpResult(StatusCodes.Status500InternalServerError),
        [StatusCodes.Status501NotImplemented] = new StatusCodeHttpResult(StatusCodes.Status501NotImplemented),
        [StatusCodes.Status502BadGateway] = new StatusCodeHttpResult(StatusCodes.Status502BadGateway),
        [StatusCodes.Status503ServiceUnavailable] = new StatusCodeHttpResult(StatusCodes.Status503ServiceUnavailable),
        [StatusCodes.Status504GatewayTimeout] = new StatusCodeHttpResult(StatusCodes.Status504GatewayTimeout),
        [StatusCodes.Status505HttpVersionNotsupported] = new StatusCodeHttpResult(StatusCodes.Status505HttpVersionNotsupported),
        [StatusCodes.Status506VariantAlsoNegotiates] = new StatusCodeHttpResult(StatusCodes.Status506VariantAlsoNegotiates),
        [StatusCodes.Status507InsufficientStorage] = new StatusCodeHttpResult(StatusCodes.Status507InsufficientStorage),
        [StatusCodes.Status508LoopDetected] = new StatusCodeHttpResult(StatusCodes.Status508LoopDetected),
        [StatusCodes.Status510NotExtended] = new StatusCodeHttpResult(StatusCodes.Status510NotExtended),
        [StatusCodes.Status511NetworkAuthenticationRequired] = new StatusCodeHttpResult(StatusCodes.Status511NetworkAuthenticationRequired),
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusCodeHttpResult"/> class
    /// with the given <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    public StatusCodeHttpResult(int statusCode)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Gets the HTTP status code.
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// Sets the status code on the HTTP response.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>A task that represents the asynchronous execute operation.</returns>
    public Task ExecuteAsync(HttpContext httpContext)
    {
        // Creating the logger with a string to preserve the category after the refactoring.
        var loggerFactory = httpContext.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Http.Result.StatusCodeResult");
        HttpResultsHelper.Log.WritingResultAsStatusCode(logger, StatusCode);

        httpContext.Response.StatusCode = StatusCode;

        return Task.CompletedTask;
    }
}
