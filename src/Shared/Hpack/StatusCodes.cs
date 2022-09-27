// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace System.Net.Http.HPack;

internal static partial class StatusCodes
{
    public static string ToStatusString(int statusCode)
    {
        switch (statusCode)
        {
            case (int)HttpStatusCode.Continue:
                return "100";
            case (int)HttpStatusCode.SwitchingProtocols:
                return "101";
            case (int)HttpStatusCode.Processing:
                return "102";

            case (int)HttpStatusCode.OK:
                return "200";
            case (int)HttpStatusCode.Created:
                return "201";
            case (int)HttpStatusCode.Accepted:
                return "202";
            case (int)HttpStatusCode.NonAuthoritativeInformation:
                return "203";
            case (int)HttpStatusCode.NoContent:
                return "204";
            case (int)HttpStatusCode.ResetContent:
                return "205";
            case (int)HttpStatusCode.PartialContent:
                return "206";
            case (int)HttpStatusCode.MultiStatus:
                return "207";
            case (int)HttpStatusCode.AlreadyReported:
                return "208";
            case (int)HttpStatusCode.IMUsed:
                return "226";

            case (int)HttpStatusCode.MultipleChoices:
                return "300";
            case (int)HttpStatusCode.MovedPermanently:
                return "301";
            case (int)HttpStatusCode.Found:
                return "302";
            case (int)HttpStatusCode.SeeOther:
                return "303";
            case (int)HttpStatusCode.NotModified:
                return "304";
            case (int)HttpStatusCode.UseProxy:
                return "305";
            case (int)HttpStatusCode.Unused:
                return "306";
            case (int)HttpStatusCode.TemporaryRedirect:
                return "307";
            case (int)HttpStatusCode.PermanentRedirect:
                return "308";

            case (int)HttpStatusCode.BadRequest:
                return "400";
            case (int)HttpStatusCode.Unauthorized:
                return "401";
            case (int)HttpStatusCode.PaymentRequired:
                return "402";
            case (int)HttpStatusCode.Forbidden:
                return "403";
            case (int)HttpStatusCode.NotFound:
                return "404";
            case (int)HttpStatusCode.MethodNotAllowed:
                return "405";
            case (int)HttpStatusCode.NotAcceptable:
                return "406";
            case (int)HttpStatusCode.ProxyAuthenticationRequired:
                return "407";
            case (int)HttpStatusCode.RequestTimeout:
                return "408";
            case (int)HttpStatusCode.Conflict:
                return "409";
            case (int)HttpStatusCode.Gone:
                return "410";
            case (int)HttpStatusCode.LengthRequired:
                return "411";
            case (int)HttpStatusCode.PreconditionFailed:
                return "412";
            case (int)HttpStatusCode.RequestEntityTooLarge:
                return "413";
            case (int)HttpStatusCode.RequestUriTooLong:
                return "414";
            case (int)HttpStatusCode.UnsupportedMediaType:
                return "415";
            case (int)HttpStatusCode.RequestedRangeNotSatisfiable:
                return "416";
            case (int)HttpStatusCode.ExpectationFailed:
                return "417";
            case (int)418:
                return "418";
            case (int)419:
                return "419";
            case (int)HttpStatusCode.MisdirectedRequest:
                return "421";
            case (int)HttpStatusCode.UnprocessableEntity:
                return "422";
            case (int)HttpStatusCode.Locked:
                return "423";
            case (int)HttpStatusCode.FailedDependency:
                return "424";
            case (int)HttpStatusCode.UpgradeRequired:
                return "426";
            case (int)HttpStatusCode.PreconditionRequired:
                return "428";
            case (int)HttpStatusCode.TooManyRequests:
                return "429";
            case (int)HttpStatusCode.RequestHeaderFieldsTooLarge:
                return "431";
            case (int)HttpStatusCode.UnavailableForLegalReasons:
                return "451";

            case (int)HttpStatusCode.InternalServerError:
                return "500";
            case (int)HttpStatusCode.NotImplemented:
                return "501";
            case (int)HttpStatusCode.BadGateway:
                return "502";
            case (int)HttpStatusCode.ServiceUnavailable:
                return "503";
            case (int)HttpStatusCode.GatewayTimeout:
                return "504";
            case (int)HttpStatusCode.HttpVersionNotSupported:
                return "505";
            case (int)HttpStatusCode.VariantAlsoNegotiates:
                return "506";
            case (int)HttpStatusCode.InsufficientStorage:
                return "507";
            case (int)HttpStatusCode.LoopDetected:
                return "508";
            case (int)HttpStatusCode.NotExtended:
                return "510";
            case (int)HttpStatusCode.NetworkAuthenticationRequired:
                return "511";

            default:
                return statusCode.ToString(CultureInfo.InvariantCulture);
        }
    }
}
