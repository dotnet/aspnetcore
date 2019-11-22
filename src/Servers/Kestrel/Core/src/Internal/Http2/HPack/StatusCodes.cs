// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.HPack
{
    internal static class StatusCodes
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
            return statusCode switch
            {
                Microsoft.AspNetCore.Http.StatusCodes.Status100Continue => _bytesStatus100,
                Microsoft.AspNetCore.Http.StatusCodes.Status101SwitchingProtocols => _bytesStatus101,
                Microsoft.AspNetCore.Http.StatusCodes.Status102Processing => _bytesStatus102,

                Microsoft.AspNetCore.Http.StatusCodes.Status200OK => _bytesStatus200,
                Microsoft.AspNetCore.Http.StatusCodes.Status201Created => _bytesStatus201,
                Microsoft.AspNetCore.Http.StatusCodes.Status202Accepted => _bytesStatus202,
                Microsoft.AspNetCore.Http.StatusCodes.Status203NonAuthoritative => _bytesStatus203,
                Microsoft.AspNetCore.Http.StatusCodes.Status204NoContent => _bytesStatus204,
                Microsoft.AspNetCore.Http.StatusCodes.Status205ResetContent => _bytesStatus205,
                Microsoft.AspNetCore.Http.StatusCodes.Status206PartialContent => _bytesStatus206,
                Microsoft.AspNetCore.Http.StatusCodes.Status207MultiStatus => _bytesStatus207,
                Microsoft.AspNetCore.Http.StatusCodes.Status208AlreadyReported => _bytesStatus208,
                Microsoft.AspNetCore.Http.StatusCodes.Status226IMUsed => _bytesStatus226,

                Microsoft.AspNetCore.Http.StatusCodes.Status300MultipleChoices => _bytesStatus300,
                Microsoft.AspNetCore.Http.StatusCodes.Status301MovedPermanently => _bytesStatus301,
                Microsoft.AspNetCore.Http.StatusCodes.Status302Found => _bytesStatus302,
                Microsoft.AspNetCore.Http.StatusCodes.Status303SeeOther => _bytesStatus303,
                Microsoft.AspNetCore.Http.StatusCodes.Status304NotModified => _bytesStatus304,
                Microsoft.AspNetCore.Http.StatusCodes.Status305UseProxy => _bytesStatus305,
                Microsoft.AspNetCore.Http.StatusCodes.Status306SwitchProxy => _bytesStatus306,
                Microsoft.AspNetCore.Http.StatusCodes.Status307TemporaryRedirect => _bytesStatus307,
                Microsoft.AspNetCore.Http.StatusCodes.Status308PermanentRedirect => _bytesStatus308,

                Microsoft.AspNetCore.Http.StatusCodes.Status400BadRequest => _bytesStatus400,
                Microsoft.AspNetCore.Http.StatusCodes.Status401Unauthorized => _bytesStatus401,
                Microsoft.AspNetCore.Http.StatusCodes.Status402PaymentRequired => _bytesStatus402,
                Microsoft.AspNetCore.Http.StatusCodes.Status403Forbidden => _bytesStatus403,
                Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound => _bytesStatus404,
                Microsoft.AspNetCore.Http.StatusCodes.Status405MethodNotAllowed => _bytesStatus405,
                Microsoft.AspNetCore.Http.StatusCodes.Status406NotAcceptable => _bytesStatus406,
                Microsoft.AspNetCore.Http.StatusCodes.Status407ProxyAuthenticationRequired => _bytesStatus407,
                Microsoft.AspNetCore.Http.StatusCodes.Status408RequestTimeout => _bytesStatus408,
                Microsoft.AspNetCore.Http.StatusCodes.Status409Conflict => _bytesStatus409,
                Microsoft.AspNetCore.Http.StatusCodes.Status410Gone => _bytesStatus410,
                Microsoft.AspNetCore.Http.StatusCodes.Status411LengthRequired => _bytesStatus411,
                Microsoft.AspNetCore.Http.StatusCodes.Status412PreconditionFailed => _bytesStatus412,
                Microsoft.AspNetCore.Http.StatusCodes.Status413PayloadTooLarge => _bytesStatus413,
                Microsoft.AspNetCore.Http.StatusCodes.Status414UriTooLong => _bytesStatus414,
                Microsoft.AspNetCore.Http.StatusCodes.Status415UnsupportedMediaType => _bytesStatus415,
                Microsoft.AspNetCore.Http.StatusCodes.Status416RangeNotSatisfiable => _bytesStatus416,
                Microsoft.AspNetCore.Http.StatusCodes.Status417ExpectationFailed => _bytesStatus417,
                Microsoft.AspNetCore.Http.StatusCodes.Status418ImATeapot => _bytesStatus418,
                Microsoft.AspNetCore.Http.StatusCodes.Status419AuthenticationTimeout => _bytesStatus419,
                Microsoft.AspNetCore.Http.StatusCodes.Status421MisdirectedRequest => _bytesStatus421,
                Microsoft.AspNetCore.Http.StatusCodes.Status422UnprocessableEntity => _bytesStatus422,
                Microsoft.AspNetCore.Http.StatusCodes.Status423Locked => _bytesStatus423,
                Microsoft.AspNetCore.Http.StatusCodes.Status424FailedDependency => _bytesStatus424,
                Microsoft.AspNetCore.Http.StatusCodes.Status426UpgradeRequired => _bytesStatus426,
                Microsoft.AspNetCore.Http.StatusCodes.Status428PreconditionRequired => _bytesStatus428,
                Microsoft.AspNetCore.Http.StatusCodes.Status429TooManyRequests => _bytesStatus429,
                Microsoft.AspNetCore.Http.StatusCodes.Status431RequestHeaderFieldsTooLarge => _bytesStatus431,
                Microsoft.AspNetCore.Http.StatusCodes.Status451UnavailableForLegalReasons => _bytesStatus451,

                Microsoft.AspNetCore.Http.StatusCodes.Status500InternalServerError => _bytesStatus500,
                Microsoft.AspNetCore.Http.StatusCodes.Status501NotImplemented => _bytesStatus501,
                Microsoft.AspNetCore.Http.StatusCodes.Status502BadGateway => _bytesStatus502,
                Microsoft.AspNetCore.Http.StatusCodes.Status503ServiceUnavailable => _bytesStatus503,
                Microsoft.AspNetCore.Http.StatusCodes.Status504GatewayTimeout => _bytesStatus504,
                Microsoft.AspNetCore.Http.StatusCodes.Status505HttpVersionNotsupported => _bytesStatus505,
                Microsoft.AspNetCore.Http.StatusCodes.Status506VariantAlsoNegotiates => _bytesStatus506,
                Microsoft.AspNetCore.Http.StatusCodes.Status507InsufficientStorage => _bytesStatus507,
                Microsoft.AspNetCore.Http.StatusCodes.Status508LoopDetected => _bytesStatus508,
                Microsoft.AspNetCore.Http.StatusCodes.Status510NotExtended => _bytesStatus510,
                Microsoft.AspNetCore.Http.StatusCodes.Status511NetworkAuthenticationRequired => _bytesStatus511,

                _ => CreateStatusBytes(statusCode)
            };
        }
    }
}
