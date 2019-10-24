// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using System.Net;
using System.Text;

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3.QPack
{
    internal static class StatusCodes
    {
        private static readonly byte[] _bytesStatus100 = CreateStatusBytes((int)HttpStatusCode.Continue);
        private static readonly byte[] _bytesStatus101 = CreateStatusBytes((int)HttpStatusCode.SwitchingProtocols);
        private static readonly byte[] _bytesStatus102 = CreateStatusBytes((int)HttpStatusCode.Processing);

        private static readonly byte[] _bytesStatus200 = CreateStatusBytes((int)HttpStatusCode.OK);
        private static readonly byte[] _bytesStatus201 = CreateStatusBytes((int)HttpStatusCode.Created);
        private static readonly byte[] _bytesStatus202 = CreateStatusBytes((int)HttpStatusCode.Accepted);
        private static readonly byte[] _bytesStatus203 = CreateStatusBytes((int)HttpStatusCode.NonAuthoritativeInformation);
        private static readonly byte[] _bytesStatus204 = CreateStatusBytes((int)HttpStatusCode.NoContent);
        private static readonly byte[] _bytesStatus205 = CreateStatusBytes((int)HttpStatusCode.ResetContent);
        private static readonly byte[] _bytesStatus206 = CreateStatusBytes((int)HttpStatusCode.PartialContent);
        private static readonly byte[] _bytesStatus207 = CreateStatusBytes((int)HttpStatusCode.MultiStatus);
        private static readonly byte[] _bytesStatus208 = CreateStatusBytes((int)HttpStatusCode.AlreadyReported);
        private static readonly byte[] _bytesStatus226 = CreateStatusBytes((int)HttpStatusCode.IMUsed);

        private static readonly byte[] _bytesStatus300 = CreateStatusBytes((int)HttpStatusCode.MultipleChoices);
        private static readonly byte[] _bytesStatus301 = CreateStatusBytes((int)HttpStatusCode.MovedPermanently);
        private static readonly byte[] _bytesStatus302 = CreateStatusBytes((int)HttpStatusCode.Found);
        private static readonly byte[] _bytesStatus303 = CreateStatusBytes((int)HttpStatusCode.SeeOther);
        private static readonly byte[] _bytesStatus304 = CreateStatusBytes((int)HttpStatusCode.NotModified);
        private static readonly byte[] _bytesStatus305 = CreateStatusBytes((int)HttpStatusCode.UseProxy);
        private static readonly byte[] _bytesStatus306 = CreateStatusBytes((int)HttpStatusCode.Unused);
        private static readonly byte[] _bytesStatus307 = CreateStatusBytes((int)HttpStatusCode.TemporaryRedirect);
        private static readonly byte[] _bytesStatus308 = CreateStatusBytes((int)HttpStatusCode.PermanentRedirect);

        private static readonly byte[] _bytesStatus400 = CreateStatusBytes((int)HttpStatusCode.BadRequest);
        private static readonly byte[] _bytesStatus401 = CreateStatusBytes((int)HttpStatusCode.Unauthorized);
        private static readonly byte[] _bytesStatus402 = CreateStatusBytes((int)HttpStatusCode.PaymentRequired);
        private static readonly byte[] _bytesStatus403 = CreateStatusBytes((int)HttpStatusCode.Forbidden);
        private static readonly byte[] _bytesStatus404 = CreateStatusBytes((int)HttpStatusCode.NotFound);
        private static readonly byte[] _bytesStatus405 = CreateStatusBytes((int)HttpStatusCode.MethodNotAllowed);
        private static readonly byte[] _bytesStatus406 = CreateStatusBytes((int)HttpStatusCode.NotAcceptable);
        private static readonly byte[] _bytesStatus407 = CreateStatusBytes((int)HttpStatusCode.ProxyAuthenticationRequired);
        private static readonly byte[] _bytesStatus408 = CreateStatusBytes((int)HttpStatusCode.RequestTimeout);
        private static readonly byte[] _bytesStatus409 = CreateStatusBytes((int)HttpStatusCode.Conflict);
        private static readonly byte[] _bytesStatus410 = CreateStatusBytes((int)HttpStatusCode.Gone);
        private static readonly byte[] _bytesStatus411 = CreateStatusBytes((int)HttpStatusCode.LengthRequired);
        private static readonly byte[] _bytesStatus412 = CreateStatusBytes((int)HttpStatusCode.PreconditionFailed);
        private static readonly byte[] _bytesStatus413 = CreateStatusBytes((int)HttpStatusCode.RequestEntityTooLarge);
        private static readonly byte[] _bytesStatus414 = CreateStatusBytes((int)HttpStatusCode.RequestUriTooLong);
        private static readonly byte[] _bytesStatus415 = CreateStatusBytes((int)HttpStatusCode.UnsupportedMediaType);
        private static readonly byte[] _bytesStatus416 = CreateStatusBytes((int)HttpStatusCode.RequestedRangeNotSatisfiable);
        private static readonly byte[] _bytesStatus417 = CreateStatusBytes((int)HttpStatusCode.ExpectationFailed);
        private static readonly byte[] _bytesStatus418 = CreateStatusBytes((int)418);
        private static readonly byte[] _bytesStatus419 = CreateStatusBytes((int)419);
        private static readonly byte[] _bytesStatus421 = CreateStatusBytes((int)HttpStatusCode.MisdirectedRequest);
        private static readonly byte[] _bytesStatus422 = CreateStatusBytes((int)HttpStatusCode.UnprocessableEntity);
        private static readonly byte[] _bytesStatus423 = CreateStatusBytes((int)HttpStatusCode.Locked);
        private static readonly byte[] _bytesStatus424 = CreateStatusBytes((int)HttpStatusCode.FailedDependency);
        private static readonly byte[] _bytesStatus426 = CreateStatusBytes((int)HttpStatusCode.UpgradeRequired);
        private static readonly byte[] _bytesStatus428 = CreateStatusBytes((int)HttpStatusCode.PreconditionRequired);
        private static readonly byte[] _bytesStatus429 = CreateStatusBytes((int)HttpStatusCode.TooManyRequests);
        private static readonly byte[] _bytesStatus431 = CreateStatusBytes((int)HttpStatusCode.RequestHeaderFieldsTooLarge);
        private static readonly byte[] _bytesStatus451 = CreateStatusBytes((int)HttpStatusCode.UnavailableForLegalReasons);

        private static readonly byte[] _bytesStatus500 = CreateStatusBytes((int)HttpStatusCode.InternalServerError);
        private static readonly byte[] _bytesStatus501 = CreateStatusBytes((int)HttpStatusCode.NotImplemented);
        private static readonly byte[] _bytesStatus502 = CreateStatusBytes((int)HttpStatusCode.BadGateway);
        private static readonly byte[] _bytesStatus503 = CreateStatusBytes((int)HttpStatusCode.ServiceUnavailable);
        private static readonly byte[] _bytesStatus504 = CreateStatusBytes((int)HttpStatusCode.GatewayTimeout);
        private static readonly byte[] _bytesStatus505 = CreateStatusBytes((int)HttpStatusCode.HttpVersionNotSupported);
        private static readonly byte[] _bytesStatus506 = CreateStatusBytes((int)HttpStatusCode.VariantAlsoNegotiates);
        private static readonly byte[] _bytesStatus507 = CreateStatusBytes((int)HttpStatusCode.InsufficientStorage);
        private static readonly byte[] _bytesStatus508 = CreateStatusBytes((int)HttpStatusCode.LoopDetected);
        private static readonly byte[] _bytesStatus510 = CreateStatusBytes((int)HttpStatusCode.NotExtended);
        private static readonly byte[] _bytesStatus511 = CreateStatusBytes((int)HttpStatusCode.NetworkAuthenticationRequired);

        private static byte[] CreateStatusBytes(int statusCode)
        {
            return Encoding.ASCII.GetBytes(statusCode.ToString(CultureInfo.InvariantCulture));
        }

        public static byte[] ToStatusBytes(int statusCode)
        {
            switch (statusCode)
            {
                case (int)HttpStatusCode.Continue:
                    return _bytesStatus100;
                case (int)HttpStatusCode.SwitchingProtocols:
                    return _bytesStatus101;
                case (int)HttpStatusCode.Processing:
                    return _bytesStatus102;

                case (int)HttpStatusCode.OK:
                    return _bytesStatus200;
                case (int)HttpStatusCode.Created:
                    return _bytesStatus201;
                case (int)HttpStatusCode.Accepted:
                    return _bytesStatus202;
                case (int)HttpStatusCode.NonAuthoritativeInformation:
                    return _bytesStatus203;
                case (int)HttpStatusCode.NoContent:
                    return _bytesStatus204;
                case (int)HttpStatusCode.ResetContent:
                    return _bytesStatus205;
                case (int)HttpStatusCode.PartialContent:
                    return _bytesStatus206;
                case (int)HttpStatusCode.MultiStatus:
                    return _bytesStatus207;
                case (int)HttpStatusCode.AlreadyReported:
                    return _bytesStatus208;
                case (int)HttpStatusCode.IMUsed:
                    return _bytesStatus226;

                case (int)HttpStatusCode.MultipleChoices:
                    return _bytesStatus300;
                case (int)HttpStatusCode.MovedPermanently:
                    return _bytesStatus301;
                case (int)HttpStatusCode.Found:
                    return _bytesStatus302;
                case (int)HttpStatusCode.SeeOther:
                    return _bytesStatus303;
                case (int)HttpStatusCode.NotModified:
                    return _bytesStatus304;
                case (int)HttpStatusCode.UseProxy:
                    return _bytesStatus305;
                case (int)HttpStatusCode.Unused:
                    return _bytesStatus306;
                case (int)HttpStatusCode.TemporaryRedirect:
                    return _bytesStatus307;
                case (int)HttpStatusCode.PermanentRedirect:
                    return _bytesStatus308;

                case (int)HttpStatusCode.BadRequest:
                    return _bytesStatus400;
                case (int)HttpStatusCode.Unauthorized:
                    return _bytesStatus401;
                case (int)HttpStatusCode.PaymentRequired:
                    return _bytesStatus402;
                case (int)HttpStatusCode.Forbidden:
                    return _bytesStatus403;
                case (int)HttpStatusCode.NotFound:
                    return _bytesStatus404;
                case (int)HttpStatusCode.MethodNotAllowed:
                    return _bytesStatus405;
                case (int)HttpStatusCode.NotAcceptable:
                    return _bytesStatus406;
                case (int)HttpStatusCode.ProxyAuthenticationRequired:
                    return _bytesStatus407;
                case (int)HttpStatusCode.RequestTimeout:
                    return _bytesStatus408;
                case (int)HttpStatusCode.Conflict:
                    return _bytesStatus409;
                case (int)HttpStatusCode.Gone:
                    return _bytesStatus410;
                case (int)HttpStatusCode.LengthRequired:
                    return _bytesStatus411;
                case (int)HttpStatusCode.PreconditionFailed:
                    return _bytesStatus412;
                case (int)HttpStatusCode.RequestEntityTooLarge:
                    return _bytesStatus413;
                case (int)HttpStatusCode.RequestUriTooLong:
                    return _bytesStatus414;
                case (int)HttpStatusCode.UnsupportedMediaType:
                    return _bytesStatus415;
                case (int)HttpStatusCode.RequestedRangeNotSatisfiable:
                    return _bytesStatus416;
                case (int)HttpStatusCode.ExpectationFailed:
                    return _bytesStatus417;
                case (int)418:
                    return _bytesStatus418;
                case (int)419:
                    return _bytesStatus419;
                case (int)HttpStatusCode.MisdirectedRequest:
                    return _bytesStatus421;
                case (int)HttpStatusCode.UnprocessableEntity:
                    return _bytesStatus422;
                case (int)HttpStatusCode.Locked:
                    return _bytesStatus423;
                case (int)HttpStatusCode.FailedDependency:
                    return _bytesStatus424;
                case (int)HttpStatusCode.UpgradeRequired:
                    return _bytesStatus426;
                case (int)HttpStatusCode.PreconditionRequired:
                    return _bytesStatus428;
                case (int)HttpStatusCode.TooManyRequests:
                    return _bytesStatus429;
                case (int)HttpStatusCode.RequestHeaderFieldsTooLarge:
                    return _bytesStatus431;
                case (int)HttpStatusCode.UnavailableForLegalReasons:
                    return _bytesStatus451;

                case (int)HttpStatusCode.InternalServerError:
                    return _bytesStatus500;
                case (int)HttpStatusCode.NotImplemented:
                    return _bytesStatus501;
                case (int)HttpStatusCode.BadGateway:
                    return _bytesStatus502;
                case (int)HttpStatusCode.ServiceUnavailable:
                    return _bytesStatus503;
                case (int)HttpStatusCode.GatewayTimeout:
                    return _bytesStatus504;
                case (int)HttpStatusCode.HttpVersionNotSupported:
                    return _bytesStatus505;
                case (int)HttpStatusCode.VariantAlsoNegotiates:
                    return _bytesStatus506;
                case (int)HttpStatusCode.InsufficientStorage:
                    return _bytesStatus507;
                case (int)HttpStatusCode.LoopDetected:
                    return _bytesStatus508;
                case (int)HttpStatusCode.NotExtended:
                    return _bytesStatus510;
                case (int)HttpStatusCode.NetworkAuthenticationRequired:
                    return _bytesStatus511;

                default:
                    return Encoding.ASCII.GetBytes(statusCode.ToString(CultureInfo.InvariantCulture));

            }
        }
    }
}
