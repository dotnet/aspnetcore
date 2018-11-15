// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http
{
    public static class ReasonPhrases
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

            return Encoding.ASCII.GetBytes(statusCode.ToString(CultureInfo.InvariantCulture) + " " + reasonPhrase);
        }

        public static byte[] ToStatusBytes(int statusCode, string reasonPhrase = null)
        {
            if (string.IsNullOrEmpty(reasonPhrase))
            {
                switch (statusCode)
                {
                    case StatusCodes.Status100Continue:
                        return _bytesStatus100;
                    case StatusCodes.Status101SwitchingProtocols:
                        return _bytesStatus101;
                    case StatusCodes.Status102Processing:
                        return _bytesStatus102;

                    case StatusCodes.Status200OK:
                        return _bytesStatus200;
                    case StatusCodes.Status201Created:
                        return _bytesStatus201;
                    case StatusCodes.Status202Accepted:
                        return _bytesStatus202;
                    case StatusCodes.Status203NonAuthoritative:
                        return _bytesStatus203;
                    case StatusCodes.Status204NoContent:
                        return _bytesStatus204;
                    case StatusCodes.Status205ResetContent:
                        return _bytesStatus205;
                    case StatusCodes.Status206PartialContent:
                        return _bytesStatus206;
                    case StatusCodes.Status207MultiStatus:
                        return _bytesStatus207;
                    case StatusCodes.Status208AlreadyReported:
                        return _bytesStatus208;
                    case StatusCodes.Status226IMUsed:
                        return _bytesStatus226;

                    case StatusCodes.Status300MultipleChoices:
                        return _bytesStatus300;
                    case StatusCodes.Status301MovedPermanently:
                        return _bytesStatus301;
                    case StatusCodes.Status302Found:
                        return _bytesStatus302;
                    case StatusCodes.Status303SeeOther:
                        return _bytesStatus303;
                    case StatusCodes.Status304NotModified:
                        return _bytesStatus304;
                    case StatusCodes.Status305UseProxy:
                        return _bytesStatus305;
                    case StatusCodes.Status306SwitchProxy:
                        return _bytesStatus306;
                    case StatusCodes.Status307TemporaryRedirect:
                        return _bytesStatus307;
                    case StatusCodes.Status308PermanentRedirect:
                        return _bytesStatus308;

                    case StatusCodes.Status400BadRequest:
                        return _bytesStatus400;
                    case StatusCodes.Status401Unauthorized:
                        return _bytesStatus401;
                    case StatusCodes.Status402PaymentRequired:
                        return _bytesStatus402;
                    case StatusCodes.Status403Forbidden:
                        return _bytesStatus403;
                    case StatusCodes.Status404NotFound:
                        return _bytesStatus404;
                    case StatusCodes.Status405MethodNotAllowed:
                        return _bytesStatus405;
                    case StatusCodes.Status406NotAcceptable:
                        return _bytesStatus406;
                    case StatusCodes.Status407ProxyAuthenticationRequired:
                        return _bytesStatus407;
                    case StatusCodes.Status408RequestTimeout:
                        return _bytesStatus408;
                    case StatusCodes.Status409Conflict:
                        return _bytesStatus409;
                    case StatusCodes.Status410Gone:
                        return _bytesStatus410;
                    case StatusCodes.Status411LengthRequired:
                        return _bytesStatus411;
                    case StatusCodes.Status412PreconditionFailed:
                        return _bytesStatus412;
                    case StatusCodes.Status413PayloadTooLarge:
                        return _bytesStatus413;
                    case StatusCodes.Status414UriTooLong:
                        return _bytesStatus414;
                    case StatusCodes.Status415UnsupportedMediaType:
                        return _bytesStatus415;
                    case StatusCodes.Status416RangeNotSatisfiable:
                        return _bytesStatus416;
                    case StatusCodes.Status417ExpectationFailed:
                        return _bytesStatus417;
                    case StatusCodes.Status418ImATeapot:
                        return _bytesStatus418;
                    case StatusCodes.Status419AuthenticationTimeout:
                        return _bytesStatus419;
                    case StatusCodes.Status421MisdirectedRequest:
                        return _bytesStatus421;
                    case StatusCodes.Status422UnprocessableEntity:
                        return _bytesStatus422;
                    case StatusCodes.Status423Locked:
                        return _bytesStatus423;
                    case StatusCodes.Status424FailedDependency:
                        return _bytesStatus424;
                    case StatusCodes.Status426UpgradeRequired:
                        return _bytesStatus426;
                    case StatusCodes.Status428PreconditionRequired:
                        return _bytesStatus428;
                    case StatusCodes.Status429TooManyRequests:
                        return _bytesStatus429;
                    case StatusCodes.Status431RequestHeaderFieldsTooLarge:
                        return _bytesStatus431;
                    case StatusCodes.Status451UnavailableForLegalReasons:
                        return _bytesStatus451;

                    case StatusCodes.Status500InternalServerError:
                        return _bytesStatus500;
                    case StatusCodes.Status501NotImplemented:
                        return _bytesStatus501;
                    case StatusCodes.Status502BadGateway:
                        return _bytesStatus502;
                    case StatusCodes.Status503ServiceUnavailable:
                        return _bytesStatus503;
                    case StatusCodes.Status504GatewayTimeout:
                        return _bytesStatus504;
                    case StatusCodes.Status505HttpVersionNotsupported:
                        return _bytesStatus505;
                    case StatusCodes.Status506VariantAlsoNegotiates:
                        return _bytesStatus506;
                    case StatusCodes.Status507InsufficientStorage:
                        return _bytesStatus507;
                    case StatusCodes.Status508LoopDetected:
                        return _bytesStatus508;
                    case StatusCodes.Status510NotExtended:
                        return _bytesStatus510;
                    case StatusCodes.Status511NetworkAuthenticationRequired:
                        return _bytesStatus511;

                    default:
                        var predefinedReasonPhrase = WebUtilities.ReasonPhrases.GetReasonPhrase(statusCode);
                        // https://tools.ietf.org/html/rfc7230#section-3.1.2 requires trailing whitespace regardless of reason phrase
                        var formattedStatusCode = statusCode.ToString(CultureInfo.InvariantCulture) + " ";
                        return string.IsNullOrEmpty(predefinedReasonPhrase)
                            ? Encoding.ASCII.GetBytes(formattedStatusCode)
                            : Encoding.ASCII.GetBytes(formattedStatusCode + predefinedReasonPhrase);

                }
            }
            return Encoding.ASCII.GetBytes(statusCode.ToString(CultureInfo.InvariantCulture) + " " + reasonPhrase);
        }
    }
}
