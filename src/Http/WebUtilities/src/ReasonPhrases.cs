// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.WebUtilities;

/// <summary>
/// Provides access to HTTP status code reason phrases as listed in
/// http://www.iana.org/assignments/http-status-codes/http-status-codes.xhtml.
/// </summary>
public static class ReasonPhrases
{
    // Status Codes listed at http://www.iana.org/assignments/http-status-codes/http-status-codes.xhtml
    private static readonly string[][] HttpReasonPhrases = [
        [],
        [
            /* 100 */ "Continue",
            /* 101 */ "Switching Protocols",
            /* 102 */ "Processing"
        ],
        [
            /* 200 */ "OK",
            /* 201 */ "Created",
            /* 202 */ "Accepted",
            /* 203 */ "Non-Authoritative Information",
            /* 204 */ "No Content",
            /* 205 */ "Reset Content",
            /* 206 */ "Partial Content",
            /* 207 */ "Multi-Status",
            /* 208 */ "Already Reported",
            /* 209 */ string.Empty,
            /* 210 */ string.Empty,
            /* 211 */ string.Empty,
            /* 212 */ string.Empty,
            /* 213 */ string.Empty,
            /* 214 */ string.Empty,
            /* 215 */ string.Empty,
            /* 216 */ string.Empty,
            /* 217 */ string.Empty,
            /* 218 */ string.Empty,
            /* 219 */ string.Empty,
            /* 220 */ string.Empty,
            /* 221 */ string.Empty,
            /* 222 */ string.Empty,
            /* 223 */ string.Empty,
            /* 224 */ string.Empty,
            /* 225 */ string.Empty,
            /* 226 */ "IM Used"
        ],
        [
            /* 300 */ "Multiple Choices",
            /* 301 */ "Moved Permanently",
            /* 302 */ "Found",
            /* 303 */ "See Other",
            /* 304 */ "Not Modified",
            /* 305 */ "Use Proxy",
            /* 306 */ "Switch Proxy",
            /* 307 */ "Temporary Redirect",
            /* 308 */ "Permanent Redirect"
        ],
        [
            /* 400 */ "Bad Request",
            /* 401 */ "Unauthorized",
            /* 402 */ "Payment Required",
            /* 403 */ "Forbidden",
            /* 404 */ "Not Found",
            /* 405 */ "Method Not Allowed",
            /* 406 */ "Not Acceptable",
            /* 407 */ "Proxy Authentication Required",
            /* 408 */ "Request Timeout",
            /* 409 */ "Conflict",
            /* 410 */ "Gone",
            /* 411 */ "Length Required",
            /* 412 */ "Precondition Failed",
            /* 413 */ "Payload Too Large",
            /* 414 */ "URI Too Long",
            /* 415 */ "Unsupported Media Type",
            /* 416 */ "Range Not Satisfiable",
            /* 417 */ "Expectation Failed",
            /* 418 */ "I'm a teapot",
            /* 419 */ "Authentication Timeout",
            /* 420 */ string.Empty,
            /* 421 */ "Misdirected Request",
            /* 422 */ "Unprocessable Entity",
            /* 423 */ "Locked",
            /* 424 */ "Failed Dependency",
            /* 425 */ string.Empty,
            /* 426 */ "Upgrade Required",
            /* 427 */ string.Empty,
            /* 428 */ "Precondition Required",
            /* 429 */ "Too Many Requests",
            /* 430 */ string.Empty,
            /* 431 */ "Request Header Fields Too Large",
            /* 432 */ string.Empty,
            /* 433 */ string.Empty,
            /* 434 */ string.Empty,
            /* 435 */ string.Empty,
            /* 436 */ string.Empty,
            /* 437 */ string.Empty,
            /* 438 */ string.Empty,
            /* 439 */ string.Empty,
            /* 440 */ string.Empty,
            /* 441 */ string.Empty,
            /* 442 */ string.Empty,
            /* 443 */ string.Empty,
            /* 444 */ string.Empty,
            /* 445 */ string.Empty,
            /* 446 */ string.Empty,
            /* 447 */ string.Empty,
            /* 448 */ string.Empty,
            /* 449 */ string.Empty,
            /* 450 */ string.Empty,
            /* 451 */ "Unavailable For Legal Reasons",
            /* 452 */ string.Empty,
            /* 453 */ string.Empty,
            /* 454 */ string.Empty,
            /* 455 */ string.Empty,
            /* 456 */ string.Empty,
            /* 457 */ string.Empty,
            /* 458 */ string.Empty,
            /* 459 */ string.Empty,
            /* 460 */ string.Empty,
            /* 461 */ string.Empty,
            /* 462 */ string.Empty,
            /* 463 */ string.Empty,
            /* 464 */ string.Empty,
            /* 465 */ string.Empty,
            /* 466 */ string.Empty,
            /* 467 */ string.Empty,
            /* 468 */ string.Empty,
            /* 469 */ string.Empty,
            /* 470 */ string.Empty,
            /* 471 */ string.Empty,
            /* 472 */ string.Empty,
            /* 473 */ string.Empty,
            /* 474 */ string.Empty,
            /* 475 */ string.Empty,
            /* 476 */ string.Empty,
            /* 477 */ string.Empty,
            /* 478 */ string.Empty,
            /* 479 */ string.Empty,
            /* 480 */ string.Empty,
            /* 481 */ string.Empty,
            /* 482 */ string.Empty,
            /* 483 */ string.Empty,
            /* 484 */ string.Empty,
            /* 485 */ string.Empty,
            /* 486 */ string.Empty,
            /* 487 */ string.Empty,
            /* 488 */ string.Empty,
            /* 489 */ string.Empty,
            /* 490 */ string.Empty,
            /* 491 */ string.Empty,
            /* 492 */ string.Empty,
            /* 493 */ string.Empty,
            /* 494 */ string.Empty,
            /* 495 */ string.Empty,
            /* 496 */ string.Empty,
            /* 497 */ string.Empty,
            /* 498 */ string.Empty,
            /* 499 */ "Client Closed Request"
        ],
        [
            /* 500 */ "Internal Server Error",
            /* 501 */ "Not Implemented",
            /* 502 */ "Bad Gateway",
            /* 503 */ "Service Unavailable",
            /* 504 */ "Gateway Timeout",
            /* 505 */ "HTTP Version Not Supported",
            /* 506 */ "Variant Also Negotiates",
            /* 507 */ "Insufficient Storage",
            /* 508 */ "Loop Detected",
            /* 509 */ string.Empty,
            /* 510 */ "Not Extended",
            /* 511 */ "Network Authentication Required"
        ]
    ];

    /// <summary>
    /// Gets the reason phrase for the specified status code.
    /// </summary>
    /// <param name="statusCode">The status code.</param>
    /// <returns>The reason phrase, or <see cref="string.Empty"/> if the status code is unknown.</returns>
    public static string GetReasonPhrase(int statusCode)
    {
        if ((uint)(statusCode - 100) < 500)
        {
            var (i, j) = Math.DivRem((uint)statusCode, 100);
            string[] phrases = HttpReasonPhrases[i];
            if (j < (uint)phrases.Length)
            {
                return phrases[j];
            }
        }
        return string.Empty;
    }
}
