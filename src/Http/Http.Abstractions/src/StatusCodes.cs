// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Net;
namespace Microsoft.AspNetCore.Http;

/// <summary>
/// A collection of constants for HTTP status codes.
///
/// Status Codes listed at http://www.iana.org/assignments/http-status-codes/http-status-codes.xhtml
/// </summary>
public static class StatusCodes
{
    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.Continue" />.
    /// </summary>
    public const int Status100Continue = (int) HttpStatusCode.Continue;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.SwitchingProtocols" />.
    /// </summary>
    public const int Status101SwitchingProtocols = (int) HttpStatusCode.SwitchingProtocols;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.Processing" />.
    /// </summary>
    public const int Status102Processing = (int) HttpStatusCode.Processing;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.OK" />.
    /// </summary>
    public const int Status200OK = (int) HttpStatusCode.OK;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.Created" />.
    /// </summary>
    public const int Status201Created = (int) HttpStatusCode.Created;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.Accepted" />.
    /// </summary>
    public const int Status202Accepted = (int) HttpStatusCode.Accepted;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.NonAuthoritative" />.
    /// </summary>
    public const int Status203NonAuthoritative = (int) HttpStatusCode.NonAuthoritative;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.NoContent" />.
    /// </summary>
    public const int Status204NoContent = (int) HttpStatusCode.NoContent;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.ResetContent" />.
    /// </summary>
    public const int Status205ResetContent = (int) HttpStatusCode.ResetContent;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.PartialContent" />.
    /// </summary>
    public const int Status206PartialContent = (int) HttpStatusCode.PartialContent;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.MultiStatus" />.
    /// </summary>
    public const int Status207MultiStatus = (int) HttpStatusCode.MultiStatus;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.AlreadyReported" />.
    /// </summary>
    public const int Status208AlreadyReported = (int) HttpStatusCode.AlreadyReported;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.IMUsed" />.
    /// </summary>
    public const int Status226IMUsed = (int) HttpStatusCode.IMUsed;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.MultipleChoices" />.
    /// </summary>
    public const int Status300MultipleChoices = (int) HttpStatusCode.MultipleChoices;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.MovedPermanently" />.
    /// </summary>
    public const int Status301MovedPermanently = (int) HttpStatusCode.MovedPermanently;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.Found" />.
    /// </summary>
    public const int Status302Found = (int) HttpStatusCode.Found;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.SeeOther" />.
    /// </summary>
    public const int Status303SeeOther = (int) HttpStatusCode.SeeOther;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.NotModified" />.
    /// </summary>
    public const int Status304NotModified = (int) HttpStatusCode.NotModified;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.UseProxy" />.
    /// </summary>
    public const int Status305UseProxy = (int) HttpStatusCode.UseProxy;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.Unused" />.
    /// </summary>
    public const int Status306SwitchProxy = (int) HttpStatusCode.Unused; // RFC 2616, removed

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.TemporaryRedirect" />.
    /// </summary>
    public const int Status307TemporaryRedirect = (int) HttpStatusCode.TemporaryRedirect;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.PermanentRedirect" />.
    /// </summary>
    public const int Status308PermanentRedirect = (int) HttpStatusCode.PermanentRedirect;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.BadRequest" />.
    /// </summary>
    public const int Status400BadRequest = (int) HttpStatusCode.BadRequest;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.Unauthorized" />.
    /// </summary>
    public const int Status401Unauthorized = (int) HttpStatusCode.Unauthorized;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.PaymentRequired" />.
    /// </summary>
    public const int Status402PaymentRequired = (int) HttpStatusCode.PaymentRequired;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.Forbidden" />.
    /// </summary>
    public const int Status403Forbidden = (int) HttpStatusCode.Forbidden;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.NotFound" />.
    /// </summary>
    public const int Status404NotFound = (int) HttpStatusCode.NotFound;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.MethodNotAllowed" />.
    /// </summary>
    public const int Status405MethodNotAllowed = (int) HttpStatusCode.MethodNotAllowed;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.NotAcceptable" />.
    /// </summary>
    public const int Status406NotAcceptable = (int) HttpStatusCode.NotAcceptable;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.ProxyAuthenticationRequired" />.
    /// </summary>
    public const int Status407ProxyAuthenticationRequired = (int) HttpStatusCode.ProxyAuthenticationRequired;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.RequestTimeout" />.
    /// </summary>
    public const int Status408RequestTimeout = (int) HttpStatusCode.RequestTimeout;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.Conflict" />.
    /// </summary>
    public const int Status409Conflict = (int) HttpStatusCode.Conflict;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.Gone" />.
    /// </summary>
    public const int Status410Gone = (int) HttpStatusCode.Gone;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.LengthRequired" />.
    /// </summary>
    public const int Status411LengthRequired = (int) HttpStatusCode.LengthRequired;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.PreconditionFailed" />.
    /// </summary>
    public const int Status412PreconditionFailed = (int) HttpStatusCode.PreconditionFailed;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.RequestEntityTooLarge" />.
    /// </summary>
    public const int Status413RequestEntityTooLarge = (int) HttpStatusCode.RequestEntityTooLarge; // RFC 2616, renamed

    /// <summary>
    /// An alias for <see cref="F:StatusCodes.Status413RequestEntityTooLarge" />.
    /// </summary>
    public const int Status413PayloadTooLarge = Status413RequestEntityTooLarge; // RFC 7231

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.RequestUriTooLong" />.
    /// </summary>
    public const int Status414RequestUriTooLong = (int) HttpStatusCode.RequestUriTooLong; // RFC 2616, renamed

    /// <summary>
    /// An alias for <see cref="F:StatusCodes.Status414RequestUriTooLong" />.
    /// </summary>
    public const int Status414UriTooLong = Status414RequestUriTooLong; // RFC 7231

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.UnsupportedMediaType" />.
    /// </summary>
    public const int Status415UnsupportedMediaType = (int) HttpStatusCode.UnsupportedMediaType;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.RequestedRangeNotSatisfiable" />.
    /// </summary>
    public const int Status416RequestedRangeNotSatisfiable = (int) HttpStatusCode.RequestedRangeNotSatisfiable; // RFC 2616, renamed

    /// <summary>
    /// An alias for <see cref="F:StatusCodes.Status416RequestedRangeNotSatisfiable" />.
    /// </summary>
    public const int Status416RangeNotSatisfiable = Status416RequestedRangeNotSatisfiable; // RFC 7233

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.ExpectationFailed" />.
    /// </summary>
    public const int Status417ExpectationFailed = (int) HttpStatusCode.ExpectationFailed;

    /// <summary>
    /// HTTP status code 418.
    /// </summary>
    public const int Status418ImATeapot = 418;

    /// <summary>
    /// HTTP status code 419.
    /// </summary>
    public const int Status419AuthenticationTimeout = 419; // Not defined in any RFC

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.MisdirectedRequest" />.
    /// </summary>
    public const int Status421MisdirectedRequest = (int) HttpStatusCode.MisdirectedRequest;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.UnprocessableEntity" />.
    /// </summary>
    public const int Status422UnprocessableEntity = (int) HttpStatusCode.UnprocessableEntity;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.Locked" />.
    /// </summary>
    public const int Status423Locked = (int) HttpStatusCode.Locked;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.FailedDependency" />.
    /// </summary>
    public const int Status424FailedDependency = (int) HttpStatusCode.FailedDependency;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.UpgradeRequired" />.
    /// </summary>
    public const int Status426UpgradeRequired = (int) HttpStatusCode.UpgradeRequired;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.PreconditionRequired" />.
    /// </summary>
    public const int Status428PreconditionRequired = (int) HttpStatusCode.PreconditionRequired;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.TooManyRequests" />.
    /// </summary>
    public const int Status429TooManyRequests = (int) HttpStatusCode.TooManyRequests;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.RequestHeaderFieldsTooLarge" />.
    /// </summary>
    public const int Status431RequestHeaderFieldsTooLarge = (int) HttpStatusCode.RequestHeaderFieldsTooLarge;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.UnavailableForLegalReasons" />.
    /// </summary>
    public const int Status451UnavailableForLegalReasons = (int) HttpStatusCode.UnavailableForLegalReasons;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.InternalServerError" />.
    /// </summary>
    public const int Status500InternalServerError = (int) HttpStatusCode.InternalServerError;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.NotImplemented" />.
    /// </summary>
    public const int Status501NotImplemented = (int) HttpStatusCode.NotImplemented;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.BadGateway" />.
    /// </summary>
    public const int Status502BadGateway = (int) HttpStatusCode.BadGateway;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.ServiceUnavailable" />.
    /// </summary>
    public const int Status503ServiceUnavailable = (int) HttpStatusCode.ServiceUnavailable;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.GatewayTimeout" />.
    /// </summary>
    public const int Status504GatewayTimeout = (int) HttpStatusCode.GatewayTimeout;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.HttpVersionNotsupported" />.
    /// </summary>
    public const int Status505HttpVersionNotsupported = (int) HttpStatusCode.HttpVersionNotsupported;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.VariantAlsoNegotiates" />.
    /// </summary>
    public const int Status506VariantAlsoNegotiates = (int) HttpStatusCode.VariantAlsoNegotiates;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.InsufficientStorage" />.
    /// </summary>
    public const int Status507InsufficientStorage = (int) HttpStatusCode.InsufficientStorage;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.LoopDetected" />.
    /// </summary>
    public const int Status508LoopDetected = (int) HttpStatusCode.LoopDetected;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.NotExtended" />.
    /// </summary>
    public const int Status510NotExtended = (int) HttpStatusCode.NotExtended;

    /// <summary>
    /// The numeric value of <see cref="F:System.Net.HttpStatusCode.NetworkAuthenticationRequired" />.
    /// </summary>
    public const int Status511NetworkAuthenticationRequired = (int) HttpStatusCode.NetworkAuthenticationRequired;
}
