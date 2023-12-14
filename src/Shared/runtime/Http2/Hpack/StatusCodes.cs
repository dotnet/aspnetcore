// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace System.Net.Http.HPack
{
    internal static partial class StatusCodes
    {
        public static ReadOnlySpan<byte> ToStatusBytes(int statusCode)
        {
            switch (statusCode)
            {
                case (int)HttpStatusCode.Continue:
                    return "100"u8;
                case (int)HttpStatusCode.SwitchingProtocols:
                    return "101"u8;
                case (int)HttpStatusCode.Processing:
                    return "102"u8;

                case (int)HttpStatusCode.OK:
                    return "200"u8;
                case (int)HttpStatusCode.Created:
                    return "201"u8;
                case (int)HttpStatusCode.Accepted:
                    return "202"u8;
                case (int)HttpStatusCode.NonAuthoritativeInformation:
                    return "203"u8;
                case (int)HttpStatusCode.NoContent:
                    return "204"u8;
                case (int)HttpStatusCode.ResetContent:
                    return "205"u8;
                case (int)HttpStatusCode.PartialContent:
                    return "206"u8;
                case (int)HttpStatusCode.MultiStatus:
                    return "207"u8;
                case (int)HttpStatusCode.AlreadyReported:
                    return "208"u8;
                case (int)HttpStatusCode.IMUsed:
                    return "226"u8;

                case (int)HttpStatusCode.MultipleChoices:
                    return "300"u8;
                case (int)HttpStatusCode.MovedPermanently:
                    return "301"u8;
                case (int)HttpStatusCode.Found:
                    return "302"u8;
                case (int)HttpStatusCode.SeeOther:
                    return "303"u8;
                case (int)HttpStatusCode.NotModified:
                    return "304"u8;
                case (int)HttpStatusCode.UseProxy:
                    return "305"u8;
                case (int)HttpStatusCode.Unused:
                    return "306"u8;
                case (int)HttpStatusCode.TemporaryRedirect:
                    return "307"u8;
                case (int)HttpStatusCode.PermanentRedirect:
                    return "308"u8;

                case (int)HttpStatusCode.BadRequest:
                    return "400"u8;
                case (int)HttpStatusCode.Unauthorized:
                    return "401"u8;
                case (int)HttpStatusCode.PaymentRequired:
                    return "402"u8;
                case (int)HttpStatusCode.Forbidden:
                    return "403"u8;
                case (int)HttpStatusCode.NotFound:
                    return "404"u8;
                case (int)HttpStatusCode.MethodNotAllowed:
                    return "405"u8;
                case (int)HttpStatusCode.NotAcceptable:
                    return "406"u8;
                case (int)HttpStatusCode.ProxyAuthenticationRequired:
                    return "407"u8;
                case (int)HttpStatusCode.RequestTimeout:
                    return "408"u8;
                case (int)HttpStatusCode.Conflict:
                    return "409"u8;
                case (int)HttpStatusCode.Gone:
                    return "410"u8;
                case (int)HttpStatusCode.LengthRequired:
                    return "411"u8;
                case (int)HttpStatusCode.PreconditionFailed:
                    return "412"u8;
                case (int)HttpStatusCode.RequestEntityTooLarge:
                    return "413"u8;
                case (int)HttpStatusCode.RequestUriTooLong:
                    return "414"u8;
                case (int)HttpStatusCode.UnsupportedMediaType:
                    return "415"u8;
                case (int)HttpStatusCode.RequestedRangeNotSatisfiable:
                    return "416"u8;
                case (int)HttpStatusCode.ExpectationFailed:
                    return "417"u8;
                case (int)418:
                    return "418"u8;
                case (int)419:
                    return "419"u8;
                case (int)HttpStatusCode.MisdirectedRequest:
                    return "421"u8;
                case (int)HttpStatusCode.UnprocessableEntity:
                    return "422"u8;
                case (int)HttpStatusCode.Locked:
                    return "423"u8;
                case (int)HttpStatusCode.FailedDependency:
                    return "424"u8;
                case (int)HttpStatusCode.UpgradeRequired:
                    return "426"u8;
                case (int)HttpStatusCode.PreconditionRequired:
                    return "428"u8;
                case (int)HttpStatusCode.TooManyRequests:
                    return "429"u8;
                case (int)HttpStatusCode.RequestHeaderFieldsTooLarge:
                    return "431"u8;
                case (int)HttpStatusCode.UnavailableForLegalReasons:
                    return "451"u8;

                case (int)HttpStatusCode.InternalServerError:
                    return "500"u8;
                case (int)HttpStatusCode.NotImplemented:
                    return "501"u8;
                case (int)HttpStatusCode.BadGateway:
                    return "502"u8;
                case (int)HttpStatusCode.ServiceUnavailable:
                    return "503"u8;
                case (int)HttpStatusCode.GatewayTimeout:
                    return "504"u8;
                case (int)HttpStatusCode.HttpVersionNotSupported:
                    return "505"u8;
                case (int)HttpStatusCode.VariantAlsoNegotiates:
                    return "506"u8;
                case (int)HttpStatusCode.InsufficientStorage:
                    return "507"u8;
                case (int)HttpStatusCode.LoopDetected:
                    return "508"u8;
                case (int)HttpStatusCode.NotExtended:
                    return "510"u8;
                case (int)HttpStatusCode.NetworkAuthenticationRequired:
                    return "511"u8;

                default:
                    return Encoding.ASCII.GetBytes(statusCode.ToString(CultureInfo.InvariantCulture));

            }
        }
    }
}
