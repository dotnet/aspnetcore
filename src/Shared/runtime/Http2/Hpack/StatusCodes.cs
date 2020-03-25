// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text;

namespace System.Net.Http.HPack
{
    internal static class StatusCodes
    {
        // This uses C# compiler's ability to refer to static data directly. For more information see https://vcsjones.dev/2019/02/01/csharp-readonly-span-bytes-static

        private static ReadOnlySpan<byte> BytesStatus100 => new byte[] { (byte)'1', (byte)'0', (byte)'0' };
        private static ReadOnlySpan<byte> BytesStatus101 => new byte[] { (byte)'1', (byte)'0', (byte)'1' };
        private static ReadOnlySpan<byte> BytesStatus102 => new byte[] { (byte)'1', (byte)'0', (byte)'2' };

        private static ReadOnlySpan<byte> BytesStatus200 => new byte[] { (byte)'2', (byte)'0', (byte)'0' };
        private static ReadOnlySpan<byte> BytesStatus201 => new byte[] { (byte)'2', (byte)'0', (byte)'1' };
        private static ReadOnlySpan<byte> BytesStatus202 => new byte[] { (byte)'2', (byte)'0', (byte)'2' };
        private static ReadOnlySpan<byte> BytesStatus203 => new byte[] { (byte)'2', (byte)'0', (byte)'3' };
        private static ReadOnlySpan<byte> BytesStatus204 => new byte[] { (byte)'2', (byte)'0', (byte)'4' };
        private static ReadOnlySpan<byte> BytesStatus205 => new byte[] { (byte)'2', (byte)'0', (byte)'5' };
        private static ReadOnlySpan<byte> BytesStatus206 => new byte[] { (byte)'2', (byte)'0', (byte)'6' };
        private static ReadOnlySpan<byte> BytesStatus207 => new byte[] { (byte)'2', (byte)'0', (byte)'7' };
        private static ReadOnlySpan<byte> BytesStatus208 => new byte[] { (byte)'2', (byte)'0', (byte)'8' };
        private static ReadOnlySpan<byte> BytesStatus226 => new byte[] { (byte)'2', (byte)'2', (byte)'6' };

        private static ReadOnlySpan<byte> BytesStatus300 => new byte[] { (byte)'3', (byte)'0', (byte)'0' };
        private static ReadOnlySpan<byte> BytesStatus301 => new byte[] { (byte)'3', (byte)'0', (byte)'1' };
        private static ReadOnlySpan<byte> BytesStatus302 => new byte[] { (byte)'3', (byte)'0', (byte)'2' };
        private static ReadOnlySpan<byte> BytesStatus303 => new byte[] { (byte)'3', (byte)'0', (byte)'3' };
        private static ReadOnlySpan<byte> BytesStatus304 => new byte[] { (byte)'3', (byte)'0', (byte)'4' };
        private static ReadOnlySpan<byte> BytesStatus305 => new byte[] { (byte)'3', (byte)'0', (byte)'5' };
        private static ReadOnlySpan<byte> BytesStatus306 => new byte[] { (byte)'3', (byte)'0', (byte)'6' };
        private static ReadOnlySpan<byte> BytesStatus307 => new byte[] { (byte)'3', (byte)'0', (byte)'7' };
        private static ReadOnlySpan<byte> BytesStatus308 => new byte[] { (byte)'3', (byte)'0', (byte)'8' };

        private static ReadOnlySpan<byte> BytesStatus400 => new byte[] { (byte)'4', (byte)'0', (byte)'0' };
        private static ReadOnlySpan<byte> BytesStatus401 => new byte[] { (byte)'4', (byte)'0', (byte)'1' };
        private static ReadOnlySpan<byte> BytesStatus402 => new byte[] { (byte)'4', (byte)'0', (byte)'2' };
        private static ReadOnlySpan<byte> BytesStatus403 => new byte[] { (byte)'4', (byte)'0', (byte)'3' };
        private static ReadOnlySpan<byte> BytesStatus404 => new byte[] { (byte)'4', (byte)'0', (byte)'4' };
        private static ReadOnlySpan<byte> BytesStatus405 => new byte[] { (byte)'4', (byte)'0', (byte)'5' };
        private static ReadOnlySpan<byte> BytesStatus406 => new byte[] { (byte)'4', (byte)'0', (byte)'6' };
        private static ReadOnlySpan<byte> BytesStatus407 => new byte[] { (byte)'4', (byte)'0', (byte)'7' };
        private static ReadOnlySpan<byte> BytesStatus408 => new byte[] { (byte)'4', (byte)'0', (byte)'8' };
        private static ReadOnlySpan<byte> BytesStatus409 => new byte[] { (byte)'4', (byte)'0', (byte)'9' };
        private static ReadOnlySpan<byte> BytesStatus410 => new byte[] { (byte)'4', (byte)'1', (byte)'0' };
        private static ReadOnlySpan<byte> BytesStatus411 => new byte[] { (byte)'4', (byte)'1', (byte)'1' };
        private static ReadOnlySpan<byte> BytesStatus412 => new byte[] { (byte)'4', (byte)'1', (byte)'2' };
        private static ReadOnlySpan<byte> BytesStatus413 => new byte[] { (byte)'4', (byte)'1', (byte)'3' };
        private static ReadOnlySpan<byte> BytesStatus414 => new byte[] { (byte)'4', (byte)'1', (byte)'4' };
        private static ReadOnlySpan<byte> BytesStatus415 => new byte[] { (byte)'4', (byte)'1', (byte)'5' };
        private static ReadOnlySpan<byte> BytesStatus416 => new byte[] { (byte)'4', (byte)'1', (byte)'6' };
        private static ReadOnlySpan<byte> BytesStatus417 => new byte[] { (byte)'4', (byte)'1', (byte)'7' };
        private static ReadOnlySpan<byte> BytesStatus418 => new byte[] { (byte)'4', (byte)'1', (byte)'8' };
        private static ReadOnlySpan<byte> BytesStatus419 => new byte[] { (byte)'4', (byte)'1', (byte)'9' };
        private static ReadOnlySpan<byte> BytesStatus421 => new byte[] { (byte)'4', (byte)'2', (byte)'1' };
        private static ReadOnlySpan<byte> BytesStatus422 => new byte[] { (byte)'4', (byte)'2', (byte)'2' };
        private static ReadOnlySpan<byte> BytesStatus423 => new byte[] { (byte)'4', (byte)'2', (byte)'3' };
        private static ReadOnlySpan<byte> BytesStatus424 => new byte[] { (byte)'4', (byte)'2', (byte)'4' };
        private static ReadOnlySpan<byte> BytesStatus426 => new byte[] { (byte)'4', (byte)'2', (byte)'6' };
        private static ReadOnlySpan<byte> BytesStatus428 => new byte[] { (byte)'4', (byte)'2', (byte)'8' };
        private static ReadOnlySpan<byte> BytesStatus429 => new byte[] { (byte)'4', (byte)'2', (byte)'9' };
        private static ReadOnlySpan<byte> BytesStatus431 => new byte[] { (byte)'4', (byte)'3', (byte)'1' };
        private static ReadOnlySpan<byte> BytesStatus451 => new byte[] { (byte)'4', (byte)'5', (byte)'1' };

        private static ReadOnlySpan<byte> BytesStatus500 => new byte[] { (byte)'5', (byte)'0', (byte)'0' };
        private static ReadOnlySpan<byte> BytesStatus501 => new byte[] { (byte)'5', (byte)'0', (byte)'1' };
        private static ReadOnlySpan<byte> BytesStatus502 => new byte[] { (byte)'5', (byte)'0', (byte)'2' };
        private static ReadOnlySpan<byte> BytesStatus503 => new byte[] { (byte)'5', (byte)'0', (byte)'3' };
        private static ReadOnlySpan<byte> BytesStatus504 => new byte[] { (byte)'5', (byte)'0', (byte)'4' };
        private static ReadOnlySpan<byte> BytesStatus505 => new byte[] { (byte)'5', (byte)'0', (byte)'5' };
        private static ReadOnlySpan<byte> BytesStatus506 => new byte[] { (byte)'5', (byte)'0', (byte)'6' };
        private static ReadOnlySpan<byte> BytesStatus507 => new byte[] { (byte)'5', (byte)'0', (byte)'7' };
        private static ReadOnlySpan<byte> BytesStatus508 => new byte[] { (byte)'5', (byte)'0', (byte)'8' };
        private static ReadOnlySpan<byte> BytesStatus510 => new byte[] { (byte)'5', (byte)'1', (byte)'0' };
        private static ReadOnlySpan<byte> BytesStatus511 => new byte[] { (byte)'5', (byte)'1', (byte)'1' };

        public static ReadOnlySpan<byte> ToStatusBytes(int statusCode)
        {
            switch (statusCode)
            {
                case (int)HttpStatusCode.Continue:
                    return BytesStatus100;
                case (int)HttpStatusCode.SwitchingProtocols:
                    return BytesStatus101;
                case (int)HttpStatusCode.Processing:
                    return BytesStatus102;

                case (int)HttpStatusCode.OK:
                    return BytesStatus200;
                case (int)HttpStatusCode.Created:
                    return BytesStatus201;
                case (int)HttpStatusCode.Accepted:
                    return BytesStatus202;
                case (int)HttpStatusCode.NonAuthoritativeInformation:
                    return BytesStatus203;
                case (int)HttpStatusCode.NoContent:
                    return BytesStatus204;
                case (int)HttpStatusCode.ResetContent:
                    return BytesStatus205;
                case (int)HttpStatusCode.PartialContent:
                    return BytesStatus206;
                case (int)HttpStatusCode.MultiStatus:
                    return BytesStatus207;
                case (int)HttpStatusCode.AlreadyReported:
                    return BytesStatus208;
                case (int)HttpStatusCode.IMUsed:
                    return BytesStatus226;

                case (int)HttpStatusCode.MultipleChoices:
                    return BytesStatus300;
                case (int)HttpStatusCode.MovedPermanently:
                    return BytesStatus301;
                case (int)HttpStatusCode.Found:
                    return BytesStatus302;
                case (int)HttpStatusCode.SeeOther:
                    return BytesStatus303;
                case (int)HttpStatusCode.NotModified:
                    return BytesStatus304;
                case (int)HttpStatusCode.UseProxy:
                    return BytesStatus305;
                case (int)HttpStatusCode.Unused:
                    return BytesStatus306;
                case (int)HttpStatusCode.TemporaryRedirect:
                    return BytesStatus307;
                case (int)HttpStatusCode.PermanentRedirect:
                    return BytesStatus308;

                case (int)HttpStatusCode.BadRequest:
                    return BytesStatus400;
                case (int)HttpStatusCode.Unauthorized:
                    return BytesStatus401;
                case (int)HttpStatusCode.PaymentRequired:
                    return BytesStatus402;
                case (int)HttpStatusCode.Forbidden:
                    return BytesStatus403;
                case (int)HttpStatusCode.NotFound:
                    return BytesStatus404;
                case (int)HttpStatusCode.MethodNotAllowed:
                    return BytesStatus405;
                case (int)HttpStatusCode.NotAcceptable:
                    return BytesStatus406;
                case (int)HttpStatusCode.ProxyAuthenticationRequired:
                    return BytesStatus407;
                case (int)HttpStatusCode.RequestTimeout:
                    return BytesStatus408;
                case (int)HttpStatusCode.Conflict:
                    return BytesStatus409;
                case (int)HttpStatusCode.Gone:
                    return BytesStatus410;
                case (int)HttpStatusCode.LengthRequired:
                    return BytesStatus411;
                case (int)HttpStatusCode.PreconditionFailed:
                    return BytesStatus412;
                case (int)HttpStatusCode.RequestEntityTooLarge:
                    return BytesStatus413;
                case (int)HttpStatusCode.RequestUriTooLong:
                    return BytesStatus414;
                case (int)HttpStatusCode.UnsupportedMediaType:
                    return BytesStatus415;
                case (int)HttpStatusCode.RequestedRangeNotSatisfiable:
                    return BytesStatus416;
                case (int)HttpStatusCode.ExpectationFailed:
                    return BytesStatus417;
                case (int)418:
                    return BytesStatus418;
                case (int)419:
                    return BytesStatus419;
                case (int)HttpStatusCode.MisdirectedRequest:
                    return BytesStatus421;
                case (int)HttpStatusCode.UnprocessableEntity:
                    return BytesStatus422;
                case (int)HttpStatusCode.Locked:
                    return BytesStatus423;
                case (int)HttpStatusCode.FailedDependency:
                    return BytesStatus424;
                case (int)HttpStatusCode.UpgradeRequired:
                    return BytesStatus426;
                case (int)HttpStatusCode.PreconditionRequired:
                    return BytesStatus428;
                case (int)HttpStatusCode.TooManyRequests:
                    return BytesStatus429;
                case (int)HttpStatusCode.RequestHeaderFieldsTooLarge:
                    return BytesStatus431;
                case (int)HttpStatusCode.UnavailableForLegalReasons:
                    return BytesStatus451;

                case (int)HttpStatusCode.InternalServerError:
                    return BytesStatus500;
                case (int)HttpStatusCode.NotImplemented:
                    return BytesStatus501;
                case (int)HttpStatusCode.BadGateway:
                    return BytesStatus502;
                case (int)HttpStatusCode.ServiceUnavailable:
                    return BytesStatus503;
                case (int)HttpStatusCode.GatewayTimeout:
                    return BytesStatus504;
                case (int)HttpStatusCode.HttpVersionNotSupported:
                    return BytesStatus505;
                case (int)HttpStatusCode.VariantAlsoNegotiates:
                    return BytesStatus506;
                case (int)HttpStatusCode.InsufficientStorage:
                    return BytesStatus507;
                case (int)HttpStatusCode.LoopDetected:
                    return BytesStatus508;
                case (int)HttpStatusCode.NotExtended:
                    return BytesStatus510;
                case (int)HttpStatusCode.NetworkAuthenticationRequired:
                    return BytesStatus511;

                default:
                    return Encoding.ASCII.GetBytes(statusCode.ToString(CultureInfo.InvariantCulture));

            }
        }
    }
}
