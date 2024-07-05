// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

internal static class ReasonPhrases
{
    private static readonly byte[] s_bytesStatus100 = CreateStatusBytes(StatusCodes.Status100Continue);
    private static readonly byte[] s_bytesStatus101 = CreateStatusBytes(StatusCodes.Status101SwitchingProtocols);
    private static readonly byte[] s_bytesStatus102 = CreateStatusBytes(StatusCodes.Status102Processing);

    private static readonly byte[] s_bytesStatus200 = CreateStatusBytes(StatusCodes.Status200OK);
    private static readonly byte[] s_bytesStatus201 = CreateStatusBytes(StatusCodes.Status201Created);
    private static readonly byte[] s_bytesStatus202 = CreateStatusBytes(StatusCodes.Status202Accepted);
    private static readonly byte[] s_bytesStatus203 = CreateStatusBytes(StatusCodes.Status203NonAuthoritative);
    private static readonly byte[] s_bytesStatus204 = CreateStatusBytes(StatusCodes.Status204NoContent);
    private static readonly byte[] s_bytesStatus205 = CreateStatusBytes(StatusCodes.Status205ResetContent);
    private static readonly byte[] s_bytesStatus206 = CreateStatusBytes(StatusCodes.Status206PartialContent);
    private static readonly byte[] s_bytesStatus207 = CreateStatusBytes(StatusCodes.Status207MultiStatus);
    private static readonly byte[] s_bytesStatus208 = CreateStatusBytes(StatusCodes.Status208AlreadyReported);
    private static readonly byte[] s_bytesStatus226 = CreateStatusBytes(StatusCodes.Status226IMUsed);

    private static readonly byte[] s_bytesStatus300 = CreateStatusBytes(StatusCodes.Status300MultipleChoices);
    private static readonly byte[] s_bytesStatus301 = CreateStatusBytes(StatusCodes.Status301MovedPermanently);
    private static readonly byte[] s_bytesStatus302 = CreateStatusBytes(StatusCodes.Status302Found);
    private static readonly byte[] s_bytesStatus303 = CreateStatusBytes(StatusCodes.Status303SeeOther);
    private static readonly byte[] s_bytesStatus304 = CreateStatusBytes(StatusCodes.Status304NotModified);
    private static readonly byte[] s_bytesStatus305 = CreateStatusBytes(StatusCodes.Status305UseProxy);
    private static readonly byte[] s_bytesStatus306 = CreateStatusBytes(StatusCodes.Status306SwitchProxy);
    private static readonly byte[] s_bytesStatus307 = CreateStatusBytes(StatusCodes.Status307TemporaryRedirect);
    private static readonly byte[] s_bytesStatus308 = CreateStatusBytes(StatusCodes.Status308PermanentRedirect);

    private static readonly byte[] s_bytesStatus400 = CreateStatusBytes(StatusCodes.Status400BadRequest);
    private static readonly byte[] s_bytesStatus401 = CreateStatusBytes(StatusCodes.Status401Unauthorized);
    private static readonly byte[] s_bytesStatus402 = CreateStatusBytes(StatusCodes.Status402PaymentRequired);
    private static readonly byte[] s_bytesStatus403 = CreateStatusBytes(StatusCodes.Status403Forbidden);
    private static readonly byte[] s_bytesStatus404 = CreateStatusBytes(StatusCodes.Status404NotFound);
    private static readonly byte[] s_bytesStatus405 = CreateStatusBytes(StatusCodes.Status405MethodNotAllowed);
    private static readonly byte[] s_bytesStatus406 = CreateStatusBytes(StatusCodes.Status406NotAcceptable);
    private static readonly byte[] s_bytesStatus407 = CreateStatusBytes(StatusCodes.Status407ProxyAuthenticationRequired);
    private static readonly byte[] s_bytesStatus408 = CreateStatusBytes(StatusCodes.Status408RequestTimeout);
    private static readonly byte[] s_bytesStatus409 = CreateStatusBytes(StatusCodes.Status409Conflict);
    private static readonly byte[] s_bytesStatus410 = CreateStatusBytes(StatusCodes.Status410Gone);
    private static readonly byte[] s_bytesStatus411 = CreateStatusBytes(StatusCodes.Status411LengthRequired);
    private static readonly byte[] s_bytesStatus412 = CreateStatusBytes(StatusCodes.Status412PreconditionFailed);
    private static readonly byte[] s_bytesStatus413 = CreateStatusBytes(StatusCodes.Status413PayloadTooLarge);
    private static readonly byte[] s_bytesStatus414 = CreateStatusBytes(StatusCodes.Status414UriTooLong);
    private static readonly byte[] s_bytesStatus415 = CreateStatusBytes(StatusCodes.Status415UnsupportedMediaType);
    private static readonly byte[] s_bytesStatus416 = CreateStatusBytes(StatusCodes.Status416RangeNotSatisfiable);
    private static readonly byte[] s_bytesStatus417 = CreateStatusBytes(StatusCodes.Status417ExpectationFailed);
    private static readonly byte[] s_bytesStatus418 = CreateStatusBytes(StatusCodes.Status418ImATeapot);
    private static readonly byte[] s_bytesStatus419 = CreateStatusBytes(StatusCodes.Status419AuthenticationTimeout);
    private static readonly byte[] s_bytesStatus421 = CreateStatusBytes(StatusCodes.Status421MisdirectedRequest);
    private static readonly byte[] s_bytesStatus422 = CreateStatusBytes(StatusCodes.Status422UnprocessableEntity);
    private static readonly byte[] s_bytesStatus423 = CreateStatusBytes(StatusCodes.Status423Locked);
    private static readonly byte[] s_bytesStatus424 = CreateStatusBytes(StatusCodes.Status424FailedDependency);
    private static readonly byte[] s_bytesStatus426 = CreateStatusBytes(StatusCodes.Status426UpgradeRequired);
    private static readonly byte[] s_bytesStatus428 = CreateStatusBytes(StatusCodes.Status428PreconditionRequired);
    private static readonly byte[] s_bytesStatus429 = CreateStatusBytes(StatusCodes.Status429TooManyRequests);
    private static readonly byte[] s_bytesStatus431 = CreateStatusBytes(StatusCodes.Status431RequestHeaderFieldsTooLarge);
    private static readonly byte[] s_bytesStatus451 = CreateStatusBytes(StatusCodes.Status451UnavailableForLegalReasons);
    private static readonly byte[] s_bytesStatus499 = CreateStatusBytes(StatusCodes.Status499ClientClosedRequest);

    private static readonly byte[] s_bytesStatus500 = CreateStatusBytes(StatusCodes.Status500InternalServerError);
    private static readonly byte[] s_bytesStatus501 = CreateStatusBytes(StatusCodes.Status501NotImplemented);
    private static readonly byte[] s_bytesStatus502 = CreateStatusBytes(StatusCodes.Status502BadGateway);
    private static readonly byte[] s_bytesStatus503 = CreateStatusBytes(StatusCodes.Status503ServiceUnavailable);
    private static readonly byte[] s_bytesStatus504 = CreateStatusBytes(StatusCodes.Status504GatewayTimeout);
    private static readonly byte[] s_bytesStatus505 = CreateStatusBytes(StatusCodes.Status505HttpVersionNotsupported);
    private static readonly byte[] s_bytesStatus506 = CreateStatusBytes(StatusCodes.Status506VariantAlsoNegotiates);
    private static readonly byte[] s_bytesStatus507 = CreateStatusBytes(StatusCodes.Status507InsufficientStorage);
    private static readonly byte[] s_bytesStatus508 = CreateStatusBytes(StatusCodes.Status508LoopDetected);
    private static readonly byte[] s_bytesStatus510 = CreateStatusBytes(StatusCodes.Status510NotExtended);
    private static readonly byte[] s_bytesStatus511 = CreateStatusBytes(StatusCodes.Status511NetworkAuthenticationRequired);

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
            StatusCodes.Status100Continue => s_bytesStatus100,
            StatusCodes.Status101SwitchingProtocols => s_bytesStatus101,
            StatusCodes.Status102Processing => s_bytesStatus102,

            StatusCodes.Status200OK => s_bytesStatus200,
            StatusCodes.Status201Created => s_bytesStatus201,
            StatusCodes.Status202Accepted => s_bytesStatus202,
            StatusCodes.Status203NonAuthoritative => s_bytesStatus203,
            StatusCodes.Status204NoContent => s_bytesStatus204,
            StatusCodes.Status205ResetContent => s_bytesStatus205,
            StatusCodes.Status206PartialContent => s_bytesStatus206,
            StatusCodes.Status207MultiStatus => s_bytesStatus207,
            StatusCodes.Status208AlreadyReported => s_bytesStatus208,
            StatusCodes.Status226IMUsed => s_bytesStatus226,

            StatusCodes.Status300MultipleChoices => s_bytesStatus300,
            StatusCodes.Status301MovedPermanently => s_bytesStatus301,
            StatusCodes.Status302Found => s_bytesStatus302,
            StatusCodes.Status303SeeOther => s_bytesStatus303,
            StatusCodes.Status304NotModified => s_bytesStatus304,
            StatusCodes.Status305UseProxy => s_bytesStatus305,
            StatusCodes.Status306SwitchProxy => s_bytesStatus306,
            StatusCodes.Status307TemporaryRedirect => s_bytesStatus307,
            StatusCodes.Status308PermanentRedirect => s_bytesStatus308,

            StatusCodes.Status400BadRequest => s_bytesStatus400,
            StatusCodes.Status401Unauthorized => s_bytesStatus401,
            StatusCodes.Status402PaymentRequired => s_bytesStatus402,
            StatusCodes.Status403Forbidden => s_bytesStatus403,
            StatusCodes.Status404NotFound => s_bytesStatus404,
            StatusCodes.Status405MethodNotAllowed => s_bytesStatus405,
            StatusCodes.Status406NotAcceptable => s_bytesStatus406,
            StatusCodes.Status407ProxyAuthenticationRequired => s_bytesStatus407,
            StatusCodes.Status408RequestTimeout => s_bytesStatus408,
            StatusCodes.Status409Conflict => s_bytesStatus409,
            StatusCodes.Status410Gone => s_bytesStatus410,
            StatusCodes.Status411LengthRequired => s_bytesStatus411,
            StatusCodes.Status412PreconditionFailed => s_bytesStatus412,
            StatusCodes.Status413PayloadTooLarge => s_bytesStatus413,
            StatusCodes.Status414UriTooLong => s_bytesStatus414,
            StatusCodes.Status415UnsupportedMediaType => s_bytesStatus415,
            StatusCodes.Status416RangeNotSatisfiable => s_bytesStatus416,
            StatusCodes.Status417ExpectationFailed => s_bytesStatus417,
            StatusCodes.Status418ImATeapot => s_bytesStatus418,
            StatusCodes.Status419AuthenticationTimeout => s_bytesStatus419,
            StatusCodes.Status421MisdirectedRequest => s_bytesStatus421,
            StatusCodes.Status422UnprocessableEntity => s_bytesStatus422,
            StatusCodes.Status423Locked => s_bytesStatus423,
            StatusCodes.Status424FailedDependency => s_bytesStatus424,
            StatusCodes.Status426UpgradeRequired => s_bytesStatus426,
            StatusCodes.Status428PreconditionRequired => s_bytesStatus428,
            StatusCodes.Status429TooManyRequests => s_bytesStatus429,
            StatusCodes.Status431RequestHeaderFieldsTooLarge => s_bytesStatus431,
            StatusCodes.Status451UnavailableForLegalReasons => s_bytesStatus451,
            StatusCodes.Status499ClientClosedRequest => s_bytesStatus499,

            StatusCodes.Status500InternalServerError => s_bytesStatus500,
            StatusCodes.Status501NotImplemented => s_bytesStatus501,
            StatusCodes.Status502BadGateway => s_bytesStatus502,
            StatusCodes.Status503ServiceUnavailable => s_bytesStatus503,
            StatusCodes.Status504GatewayTimeout => s_bytesStatus504,
            StatusCodes.Status505HttpVersionNotsupported => s_bytesStatus505,
            StatusCodes.Status506VariantAlsoNegotiates => s_bytesStatus506,
            StatusCodes.Status507InsufficientStorage => s_bytesStatus507,
            StatusCodes.Status508LoopDetected => s_bytesStatus508,
            StatusCodes.Status510NotExtended => s_bytesStatus510,
            StatusCodes.Status511NetworkAuthenticationRequired => s_bytesStatus511,

            _ => null
        };

        if (candidate is not null && (string.IsNullOrEmpty(reasonPhrase) || WebUtilities.ReasonPhrases.GetReasonPhrase(statusCode) == reasonPhrase))
        {
            return candidate;
        }

        return CreateStatusBytes(statusCode, reasonPhrase);
    }
}
