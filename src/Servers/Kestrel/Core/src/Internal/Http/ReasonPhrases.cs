// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

internal static class ReasonPhrases
{
    private static readonly byte[] _bytesStatus100 = CreateStatusBytes(StatusCodes.Status100Continue);
    private static readonly byte[] _bytesStatus101 = CreateStatusBytes(StatusCodes.Status101SwitchingProtocols);
    private static readonly byte[] _bytesStatus102 = CreateStatusBytes(StatusCodes.Status102Processing);

    private static readonly byte[] _bytesStatus200 = CreateStatusBytes(StatusCodes.Status200OK);
    private static readonly byte[] _bytesStatus201 = CreateStatusBytes(StatusCodes.Status201Created);
    private static readonly byte[] _bytesStatus202 = CreateStatusBytes(StatusCodes.Status202Accepted);
    private static readonly byte[] _bytesStatus203 = CreateStatusBytes(StatusCodes.Status203NonAuthoritative);
    private static readonly byte[] _bytesStatus204 = CreateStatusBytes(StatusCodes.Status204NoContent);
    private static readonly byte[] _bytesStatus205 = CreateStatusBytes(StatusCodes.Status205ResetContent);
    private static readonly byte[] _bytesStatus206 = CreateStatusBytes(StatusCodes.Status206PartialContent);
    private static readonly byte[] _bytesStatus207 = CreateStatusBytes(StatusCodes.Status207MultiStatus);
    private static readonly byte[] _bytesStatus208 = CreateStatusBytes(StatusCodes.Status208AlreadyReported);
    private static readonly byte[] _bytesStatus226 = CreateStatusBytes(StatusCodes.Status226IMUsed);

    private static readonly byte[] _bytesStatus300 = CreateStatusBytes(StatusCodes.Status300MultipleChoices);
    private static readonly byte[] _bytesStatus301 = CreateStatusBytes(StatusCodes.Status301MovedPermanently);
    private static readonly byte[] _bytesStatus302 = CreateStatusBytes(StatusCodes.Status302Found);
    private static readonly byte[] _bytesStatus303 = CreateStatusBytes(StatusCodes.Status303SeeOther);
    private static readonly byte[] _bytesStatus304 = CreateStatusBytes(StatusCodes.Status304NotModified);
    private static readonly byte[] _bytesStatus305 = CreateStatusBytes(StatusCodes.Status305UseProxy);
    private static readonly byte[] _bytesStatus306 = CreateStatusBytes(StatusCodes.Status306SwitchProxy);
    private static readonly byte[] _bytesStatus307 = CreateStatusBytes(StatusCodes.Status307TemporaryRedirect);
    private static readonly byte[] _bytesStatus308 = CreateStatusBytes(StatusCodes.Status308PermanentRedirect);

    private static readonly byte[] _bytesStatus400 = CreateStatusBytes(StatusCodes.Status400BadRequest);
    private static readonly byte[] _bytesStatus401 = CreateStatusBytes(StatusCodes.Status401Unauthorized);
    private static readonly byte[] _bytesStatus402 = CreateStatusBytes(StatusCodes.Status402PaymentRequired);
    private static readonly byte[] _bytesStatus403 = CreateStatusBytes(StatusCodes.Status403Forbidden);
    private static readonly byte[] _bytesStatus404 = CreateStatusBytes(StatusCodes.Status404NotFound);
    private static readonly byte[] _bytesStatus405 = CreateStatusBytes(StatusCodes.Status405MethodNotAllowed);
    private static readonly byte[] _bytesStatus406 = CreateStatusBytes(StatusCodes.Status406NotAcceptable);
    private static readonly byte[] _bytesStatus407 = CreateStatusBytes(StatusCodes.Status407ProxyAuthenticationRequired);
    private static readonly byte[] _bytesStatus408 = CreateStatusBytes(StatusCodes.Status408RequestTimeout);
    private static readonly byte[] _bytesStatus409 = CreateStatusBytes(StatusCodes.Status409Conflict);
    private static readonly byte[] _bytesStatus410 = CreateStatusBytes(StatusCodes.Status410Gone);
    private static readonly byte[] _bytesStatus411 = CreateStatusBytes(StatusCodes.Status411LengthRequired);
    private static readonly byte[] _bytesStatus412 = CreateStatusBytes(StatusCodes.Status412PreconditionFailed);
    private static readonly byte[] _bytesStatus413 = CreateStatusBytes(StatusCodes.Status413PayloadTooLarge);
    private static readonly byte[] _bytesStatus414 = CreateStatusBytes(StatusCodes.Status414UriTooLong);
    private static readonly byte[] _bytesStatus415 = CreateStatusBytes(StatusCodes.Status415UnsupportedMediaType);
    private static readonly byte[] _bytesStatus416 = CreateStatusBytes(StatusCodes.Status416RangeNotSatisfiable);
    private static readonly byte[] _bytesStatus417 = CreateStatusBytes(StatusCodes.Status417ExpectationFailed);
    private static readonly byte[] _bytesStatus418 = CreateStatusBytes(StatusCodes.Status418ImATeapot);
    private static readonly byte[] _bytesStatus419 = CreateStatusBytes(StatusCodes.Status419AuthenticationTimeout);
    private static readonly byte[] _bytesStatus421 = CreateStatusBytes(StatusCodes.Status421MisdirectedRequest);
    private static readonly byte[] _bytesStatus422 = CreateStatusBytes(StatusCodes.Status422UnprocessableEntity);
    private static readonly byte[] _bytesStatus423 = CreateStatusBytes(StatusCodes.Status423Locked);
    private static readonly byte[] _bytesStatus424 = CreateStatusBytes(StatusCodes.Status424FailedDependency);
    private static readonly byte[] _bytesStatus426 = CreateStatusBytes(StatusCodes.Status426UpgradeRequired);
    private static readonly byte[] _bytesStatus428 = CreateStatusBytes(StatusCodes.Status428PreconditionRequired);
    private static readonly byte[] _bytesStatus429 = CreateStatusBytes(StatusCodes.Status429TooManyRequests);
    private static readonly byte[] _bytesStatus431 = CreateStatusBytes(StatusCodes.Status431RequestHeaderFieldsTooLarge);
    private static readonly byte[] _bytesStatus451 = CreateStatusBytes(StatusCodes.Status451UnavailableForLegalReasons);
    private static readonly byte[] _bytesStatus499 = CreateStatusBytes(StatusCodes.Status499ClientClosedRequest);

    private static readonly byte[] _bytesStatus500 = CreateStatusBytes(StatusCodes.Status500InternalServerError);
    private static readonly byte[] _bytesStatus501 = CreateStatusBytes(StatusCodes.Status501NotImplemented);
    private static readonly byte[] _bytesStatus502 = CreateStatusBytes(StatusCodes.Status502BadGateway);
    private static readonly byte[] _bytesStatus503 = CreateStatusBytes(StatusCodes.Status503ServiceUnavailable);
    private static readonly byte[] _bytesStatus504 = CreateStatusBytes(StatusCodes.Status504GatewayTimeout);
    private static readonly byte[] _bytesStatus505 = CreateStatusBytes(StatusCodes.Status505HttpVersionNotsupported);
    private static readonly byte[] _bytesStatus506 = CreateStatusBytes(StatusCodes.Status506VariantAlsoNegotiates);
    private static readonly byte[] _bytesStatus507 = CreateStatusBytes(StatusCodes.Status507InsufficientStorage);
    private static readonly byte[] _bytesStatus508 = CreateStatusBytes(StatusCodes.Status508LoopDetected);
    private static readonly byte[] _bytesStatus510 = CreateStatusBytes(StatusCodes.Status510NotExtended);
    private static readonly byte[] _bytesStatus511 = CreateStatusBytes(StatusCodes.Status511NetworkAuthenticationRequired);

    private static byte[] CreateStatusBytes(int statusCode)
    {
        var reasonPhrase = WebUtilities.ReasonPhrases.GetReasonPhrase(statusCode);
        Debug.Assert(!string.IsNullOrEmpty(reasonPhrase));

        return CreateStatusBytes(statusCode, reasonPhrase);
    }

    private static byte[] CreateStatusBytes(int statusCode, string? reasonPhrase)
    {
        // https://tools.ietf.org/html/rfc7230#section-3.1.2 requires trailing whitespace regardless of reason phrase
        return Encoding.ASCII.GetBytes(statusCode.ToString(CultureInfo.InvariantCulture) + " " + reasonPhrase);
    }

    public static byte[] ToStatusBytes(int statusCode, string? reasonPhrase = null)
    {
        var candidate = statusCode switch
        {
            StatusCodes.Status100Continue => _bytesStatus100,
            StatusCodes.Status101SwitchingProtocols => _bytesStatus101,
            StatusCodes.Status102Processing => _bytesStatus102,

            StatusCodes.Status200OK => _bytesStatus200,
            StatusCodes.Status201Created => _bytesStatus201,
            StatusCodes.Status202Accepted => _bytesStatus202,
            StatusCodes.Status203NonAuthoritative => _bytesStatus203,
            StatusCodes.Status204NoContent => _bytesStatus204,
            StatusCodes.Status205ResetContent => _bytesStatus205,
            StatusCodes.Status206PartialContent => _bytesStatus206,
            StatusCodes.Status207MultiStatus => _bytesStatus207,
            StatusCodes.Status208AlreadyReported => _bytesStatus208,
            StatusCodes.Status226IMUsed => _bytesStatus226,

            StatusCodes.Status300MultipleChoices => _bytesStatus300,
            StatusCodes.Status301MovedPermanently => _bytesStatus301,
            StatusCodes.Status302Found => _bytesStatus302,
            StatusCodes.Status303SeeOther => _bytesStatus303,
            StatusCodes.Status304NotModified => _bytesStatus304,
            StatusCodes.Status305UseProxy => _bytesStatus305,
            StatusCodes.Status306SwitchProxy => _bytesStatus306,
            StatusCodes.Status307TemporaryRedirect => _bytesStatus307,
            StatusCodes.Status308PermanentRedirect => _bytesStatus308,

            StatusCodes.Status400BadRequest => _bytesStatus400,
            StatusCodes.Status401Unauthorized => _bytesStatus401,
            StatusCodes.Status402PaymentRequired => _bytesStatus402,
            StatusCodes.Status403Forbidden => _bytesStatus403,
            StatusCodes.Status404NotFound => _bytesStatus404,
            StatusCodes.Status405MethodNotAllowed => _bytesStatus405,
            StatusCodes.Status406NotAcceptable => _bytesStatus406,
            StatusCodes.Status407ProxyAuthenticationRequired => _bytesStatus407,
            StatusCodes.Status408RequestTimeout => _bytesStatus408,
            StatusCodes.Status409Conflict => _bytesStatus409,
            StatusCodes.Status410Gone => _bytesStatus410,
            StatusCodes.Status411LengthRequired => _bytesStatus411,
            StatusCodes.Status412PreconditionFailed => _bytesStatus412,
            StatusCodes.Status413PayloadTooLarge => _bytesStatus413,
            StatusCodes.Status414UriTooLong => _bytesStatus414,
            StatusCodes.Status415UnsupportedMediaType => _bytesStatus415,
            StatusCodes.Status416RangeNotSatisfiable => _bytesStatus416,
            StatusCodes.Status417ExpectationFailed => _bytesStatus417,
            StatusCodes.Status418ImATeapot => _bytesStatus418,
            StatusCodes.Status419AuthenticationTimeout => _bytesStatus419,
            StatusCodes.Status421MisdirectedRequest => _bytesStatus421,
            StatusCodes.Status422UnprocessableEntity => _bytesStatus422,
            StatusCodes.Status423Locked => _bytesStatus423,
            StatusCodes.Status424FailedDependency => _bytesStatus424,
            StatusCodes.Status426UpgradeRequired => _bytesStatus426,
            StatusCodes.Status428PreconditionRequired => _bytesStatus428,
            StatusCodes.Status429TooManyRequests => _bytesStatus429,
            StatusCodes.Status431RequestHeaderFieldsTooLarge => _bytesStatus431,
            StatusCodes.Status451UnavailableForLegalReasons => _bytesStatus451,
            StatusCodes.Status499ClientClosedRequest => _bytesStatus499,

            StatusCodes.Status500InternalServerError => _bytesStatus500,
            StatusCodes.Status501NotImplemented => _bytesStatus501,
            StatusCodes.Status502BadGateway => _bytesStatus502,
            StatusCodes.Status503ServiceUnavailable => _bytesStatus503,
            StatusCodes.Status504GatewayTimeout => _bytesStatus504,
            StatusCodes.Status505HttpVersionNotsupported => _bytesStatus505,
            StatusCodes.Status506VariantAlsoNegotiates => _bytesStatus506,
            StatusCodes.Status507InsufficientStorage => _bytesStatus507,
            StatusCodes.Status508LoopDetected => _bytesStatus508,
            StatusCodes.Status510NotExtended => _bytesStatus510,
            StatusCodes.Status511NetworkAuthenticationRequired => _bytesStatus511,

            _ => null
        };

        if (candidate is not null && (string.IsNullOrEmpty(reasonPhrase) || WebUtilities.ReasonPhrases.GetReasonPhrase(statusCode) == reasonPhrase))
        {
            return candidate;
        }

        return CreateStatusBytes(statusCode, reasonPhrase);
    }
}
