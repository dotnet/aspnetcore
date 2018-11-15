// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    public static class StatusCodes
    {
        private static readonly byte[] _bytesStatus100 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status100Continue);
        private static readonly byte[] _bytesStatus101 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status101SwitchingProtocols);
        private static readonly byte[] _bytesStatus102 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status102Processing);

        private static readonly byte[] _bytesStatus200 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status200OK);
        private static readonly byte[] _bytesStatus201 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status201Created);
        private static readonly byte[] _bytesStatus202 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status202Accepted);
        private static readonly byte[] _bytesStatus203 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status203NonAuthoritative);
        private static readonly byte[] _bytesStatus204 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status204NoContent);
        private static readonly byte[] _bytesStatus205 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status205ResetContent);
        private static readonly byte[] _bytesStatus206 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status206PartialContent);
        private static readonly byte[] _bytesStatus207 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status207MultiStatus);
        private static readonly byte[] _bytesStatus208 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status208AlreadyReported);
        private static readonly byte[] _bytesStatus226 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status226IMUsed);

        private static readonly byte[] _bytesStatus300 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status300MultipleChoices);
        private static readonly byte[] _bytesStatus301 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status301MovedPermanently);
        private static readonly byte[] _bytesStatus302 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status302Found);
        private static readonly byte[] _bytesStatus303 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status303SeeOther);
        private static readonly byte[] _bytesStatus304 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status304NotModified);
        private static readonly byte[] _bytesStatus305 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status305UseProxy);
        private static readonly byte[] _bytesStatus306 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status306SwitchProxy);
        private static readonly byte[] _bytesStatus307 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status307TemporaryRedirect);
        private static readonly byte[] _bytesStatus308 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status308PermanentRedirect);

        private static readonly byte[] _bytesStatus400 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest);
        private static readonly byte[] _bytesStatus401 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status401Unauthorized);
        private static readonly byte[] _bytesStatus402 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status402PaymentRequired);
        private static readonly byte[] _bytesStatus403 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status403Forbidden);
        private static readonly byte[] _bytesStatus404 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound);
        private static readonly byte[] _bytesStatus405 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status405MethodNotAllowed);
        private static readonly byte[] _bytesStatus406 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status406NotAcceptable);
        private static readonly byte[] _bytesStatus407 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status407ProxyAuthenticationRequired);
        private static readonly byte[] _bytesStatus408 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status408RequestTimeout);
        private static readonly byte[] _bytesStatus409 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status409Conflict);
        private static readonly byte[] _bytesStatus410 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status410Gone);
        private static readonly byte[] _bytesStatus411 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status411LengthRequired);
        private static readonly byte[] _bytesStatus412 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status412PreconditionFailed);
        private static readonly byte[] _bytesStatus413 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status413PayloadTooLarge);
        private static readonly byte[] _bytesStatus414 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status414UriTooLong);
        private static readonly byte[] _bytesStatus415 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status415UnsupportedMediaType);
        private static readonly byte[] _bytesStatus416 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status416RangeNotSatisfiable);
        private static readonly byte[] _bytesStatus417 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status417ExpectationFailed);
        private static readonly byte[] _bytesStatus418 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status418ImATeapot);
        private static readonly byte[] _bytesStatus419 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status419AuthenticationTimeout);
        private static readonly byte[] _bytesStatus421 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status421MisdirectedRequest);
        private static readonly byte[] _bytesStatus422 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status422UnprocessableEntity);
        private static readonly byte[] _bytesStatus423 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status423Locked);
        private static readonly byte[] _bytesStatus424 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status424FailedDependency);
        private static readonly byte[] _bytesStatus426 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status426UpgradeRequired);
        private static readonly byte[] _bytesStatus428 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status428PreconditionRequired);
        private static readonly byte[] _bytesStatus429 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status429TooManyRequests);
        private static readonly byte[] _bytesStatus431 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status431RequestHeaderFieldsTooLarge);
        private static readonly byte[] _bytesStatus451 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status451UnavailableForLegalReasons);

        private static readonly byte[] _bytesStatus500 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError);
        private static readonly byte[] _bytesStatus501 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status501NotImplemented);
        private static readonly byte[] _bytesStatus502 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status502BadGateway);
        private static readonly byte[] _bytesStatus503 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status503ServiceUnavailable);
        private static readonly byte[] _bytesStatus504 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status504GatewayTimeout);
        private static readonly byte[] _bytesStatus505 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status505HttpVersionNotsupported);
        private static readonly byte[] _bytesStatus506 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status506VariantAlsoNegotiates);
        private static readonly byte[] _bytesStatus507 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status507InsufficientStorage);
        private static readonly byte[] _bytesStatus508 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status508LoopDetected);
        private static readonly byte[] _bytesStatus510 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status510NotExtended);
        private static readonly byte[] _bytesStatus511 = CreateStatusBytes(Microsoft.AspNetCore.Http.StatusCodes.Status511NetworkAuthenticationRequired);

        private static byte[] CreateStatusBytes(int statusCode)
        {
            return Encoding.ASCII.GetBytes(statusCode.ToString(CultureInfo.InvariantCulture));
        }

        public static byte[] ToStatusBytes(int statusCode)
        {
            switch (statusCode)
            {
                case Microsoft.AspNetCore.Http.StatusCodes.Status100Continue:
                    return _bytesStatus100;
                case Microsoft.AspNetCore.Http.StatusCodes.Status101SwitchingProtocols:
                    return _bytesStatus101;
                case Microsoft.AspNetCore.Http.StatusCodes.Status102Processing:
                    return _bytesStatus102;

                case Microsoft.AspNetCore.Http.StatusCodes.Status200OK:
                    return _bytesStatus200;
                case Microsoft.AspNetCore.Http.StatusCodes.Status201Created:
                    return _bytesStatus201;
                case Microsoft.AspNetCore.Http.StatusCodes.Status202Accepted:
                    return _bytesStatus202;
                case Microsoft.AspNetCore.Http.StatusCodes.Status203NonAuthoritative:
                    return _bytesStatus203;
                case Microsoft.AspNetCore.Http.StatusCodes.Status204NoContent:
                    return _bytesStatus204;
                case Microsoft.AspNetCore.Http.StatusCodes.Status205ResetContent:
                    return _bytesStatus205;
                case Microsoft.AspNetCore.Http.StatusCodes.Status206PartialContent:
                    return _bytesStatus206;
                case Microsoft.AspNetCore.Http.StatusCodes.Status207MultiStatus:
                    return _bytesStatus207;
                case Microsoft.AspNetCore.Http.StatusCodes.Status208AlreadyReported:
                    return _bytesStatus208;
                case Microsoft.AspNetCore.Http.StatusCodes.Status226IMUsed:
                    return _bytesStatus226;

                case Microsoft.AspNetCore.Http.StatusCodes.Status300MultipleChoices:
                    return _bytesStatus300;
                case Microsoft.AspNetCore.Http.StatusCodes.Status301MovedPermanently:
                    return _bytesStatus301;
                case Microsoft.AspNetCore.Http.StatusCodes.Status302Found:
                    return _bytesStatus302;
                case Microsoft.AspNetCore.Http.StatusCodes.Status303SeeOther:
                    return _bytesStatus303;
                case Microsoft.AspNetCore.Http.StatusCodes.Status304NotModified:
                    return _bytesStatus304;
                case Microsoft.AspNetCore.Http.StatusCodes.Status305UseProxy:
                    return _bytesStatus305;
                case Microsoft.AspNetCore.Http.StatusCodes.Status306SwitchProxy:
                    return _bytesStatus306;
                case Microsoft.AspNetCore.Http.StatusCodes.Status307TemporaryRedirect:
                    return _bytesStatus307;
                case Microsoft.AspNetCore.Http.StatusCodes.Status308PermanentRedirect:
                    return _bytesStatus308;

                case Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest:
                    return _bytesStatus400;
                case Microsoft.AspNetCore.Http.StatusCodes.Status401Unauthorized:
                    return _bytesStatus401;
                case Microsoft.AspNetCore.Http.StatusCodes.Status402PaymentRequired:
                    return _bytesStatus402;
                case Microsoft.AspNetCore.Http.StatusCodes.Status403Forbidden:
                    return _bytesStatus403;
                case Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound:
                    return _bytesStatus404;
                case Microsoft.AspNetCore.Http.StatusCodes.Status405MethodNotAllowed:
                    return _bytesStatus405;
                case Microsoft.AspNetCore.Http.StatusCodes.Status406NotAcceptable:
                    return _bytesStatus406;
                case Microsoft.AspNetCore.Http.StatusCodes.Status407ProxyAuthenticationRequired:
                    return _bytesStatus407;
                case Microsoft.AspNetCore.Http.StatusCodes.Status408RequestTimeout:
                    return _bytesStatus408;
                case Microsoft.AspNetCore.Http.StatusCodes.Status409Conflict:
                    return _bytesStatus409;
                case Microsoft.AspNetCore.Http.StatusCodes.Status410Gone:
                    return _bytesStatus410;
                case Microsoft.AspNetCore.Http.StatusCodes.Status411LengthRequired:
                    return _bytesStatus411;
                case Microsoft.AspNetCore.Http.StatusCodes.Status412PreconditionFailed:
                    return _bytesStatus412;
                case Microsoft.AspNetCore.Http.StatusCodes.Status413PayloadTooLarge:
                    return _bytesStatus413;
                case Microsoft.AspNetCore.Http.StatusCodes.Status414UriTooLong:
                    return _bytesStatus414;
                case Microsoft.AspNetCore.Http.StatusCodes.Status415UnsupportedMediaType:
                    return _bytesStatus415;
                case Microsoft.AspNetCore.Http.StatusCodes.Status416RangeNotSatisfiable:
                    return _bytesStatus416;
                case Microsoft.AspNetCore.Http.StatusCodes.Status417ExpectationFailed:
                    return _bytesStatus417;
                case Microsoft.AspNetCore.Http.StatusCodes.Status418ImATeapot:
                    return _bytesStatus418;
                case Microsoft.AspNetCore.Http.StatusCodes.Status419AuthenticationTimeout:
                    return _bytesStatus419;
                case Microsoft.AspNetCore.Http.StatusCodes.Status421MisdirectedRequest:
                    return _bytesStatus421;
                case Microsoft.AspNetCore.Http.StatusCodes.Status422UnprocessableEntity:
                    return _bytesStatus422;
                case Microsoft.AspNetCore.Http.StatusCodes.Status423Locked:
                    return _bytesStatus423;
                case Microsoft.AspNetCore.Http.StatusCodes.Status424FailedDependency:
                    return _bytesStatus424;
                case Microsoft.AspNetCore.Http.StatusCodes.Status426UpgradeRequired:
                    return _bytesStatus426;
                case Microsoft.AspNetCore.Http.StatusCodes.Status428PreconditionRequired:
                    return _bytesStatus428;
                case Microsoft.AspNetCore.Http.StatusCodes.Status429TooManyRequests:
                    return _bytesStatus429;
                case Microsoft.AspNetCore.Http.StatusCodes.Status431RequestHeaderFieldsTooLarge:
                    return _bytesStatus431;
                case Microsoft.AspNetCore.Http.StatusCodes.Status451UnavailableForLegalReasons:
                    return _bytesStatus451;

                case Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError:
                    return _bytesStatus500;
                case Microsoft.AspNetCore.Http.StatusCodes.Status501NotImplemented:
                    return _bytesStatus501;
                case Microsoft.AspNetCore.Http.StatusCodes.Status502BadGateway:
                    return _bytesStatus502;
                case Microsoft.AspNetCore.Http.StatusCodes.Status503ServiceUnavailable:
                    return _bytesStatus503;
                case Microsoft.AspNetCore.Http.StatusCodes.Status504GatewayTimeout:
                    return _bytesStatus504;
                case Microsoft.AspNetCore.Http.StatusCodes.Status505HttpVersionNotsupported:
                    return _bytesStatus505;
                case Microsoft.AspNetCore.Http.StatusCodes.Status506VariantAlsoNegotiates:
                    return _bytesStatus506;
                case Microsoft.AspNetCore.Http.StatusCodes.Status507InsufficientStorage:
                    return _bytesStatus507;
                case Microsoft.AspNetCore.Http.StatusCodes.Status508LoopDetected:
                    return _bytesStatus508;
                case Microsoft.AspNetCore.Http.StatusCodes.Status510NotExtended:
                    return _bytesStatus510;
                case Microsoft.AspNetCore.Http.StatusCodes.Status511NetworkAuthenticationRequired:
                    return _bytesStatus511;

                default:
                    return Encoding.ASCII.GetBytes(statusCode.ToString(CultureInfo.InvariantCulture));

            }
        }
    }
}
