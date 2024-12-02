// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Http;

internal static class ProblemDetailsDefaults
{
    public static readonly Dictionary<int, (string Type, string Title)> Defaults = new()
    {
        [400] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            "Bad Request"
        ),

        [401] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.2",
            "Unauthorized"
        ),

        [403] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            "Forbidden"
        ),

        [404] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            "Not Found"
        ),

        [405] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.6",
            "Method Not Allowed"
        ),

        [406] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.7",
            "Not Acceptable"
        ),

        [407] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.8",
            "Proxy Authentication Required"
        ),

        [408] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.9",
            "Request Timeout"
        ),

        [409] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            "Conflict"
        ),

        [410] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.11",
            "Gone"
        ),

        [411] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.12",
            "Length Required"
        ),

        [412] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.13",
            "Precondition Failed"
        ),

        [413] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.14",
            "Content Too Large"
        ),

        [414] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.15",
            "URI Too Long"
        ),

        [415] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.16",
            "Unsupported Media Type"
        ),

        [416] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.17",
            "Range Not Satisfiable"
        ),

        [417] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.18",
            "Expectation Failed"
        ),

        [421] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.20",
            "Misdirected Request"
        ),

        [422] =
        (
            "https://tools.ietf.org/html/rfc4918#section-11.2",
            "Unprocessable Entity"
        ),

        [426] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.5.22",
            "Upgrade Required"
        ),

        [500] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.6.1",
            "An error occurred while processing your request."
        ),

        [501] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.6.2",
            "Not Implemented"
        ),

        [502] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.6.3",
            "Bad Gateway"
        ),

        [503] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.6.4",
            "Service Unavailable"
        ),

        [504] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.6.5",
            "Gateway Timeout"
        ),

        [505] =
        (
            "https://tools.ietf.org/html/rfc9110#section-15.6.6",
            "HTTP Version Not Supported"
        ),
    };

    public static void Apply(ProblemDetails problemDetails, int? statusCode)
    {
        // We allow StatusCode to be specified either on ProblemDetails or on the ObjectResult and use it to configure the other.
        // This lets users write <c>return Conflict(new Problem("some description"))</c>
        // or <c>return Problem("some-problem", 422)</c> and have the response have consistent fields.
        if (problemDetails.Status is null)
        {
            if (statusCode is not null)
            {
                problemDetails.Status = statusCode;
            }
            else
            {
                problemDetails.Status = problemDetails is HttpValidationProblemDetails ?
                    StatusCodes.Status400BadRequest :
                    StatusCodes.Status500InternalServerError;
            }
        }

        var status = problemDetails.Status.GetValueOrDefault();
        if (Defaults.TryGetValue(status, out var defaults))
        {
            problemDetails.Title ??= defaults.Title;
            problemDetails.Type ??= defaults.Type;
        }
        else if (problemDetails.Title is null)
        {
            var reasonPhrase = ReasonPhrases.GetReasonPhrase(status);
            if (!string.IsNullOrEmpty(reasonPhrase))
            {
                problemDetails.Title = reasonPhrase;
            }
        }
    }
}
