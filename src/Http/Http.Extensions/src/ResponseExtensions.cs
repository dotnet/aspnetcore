// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Extension methods for <see cref="HttpResponse"/>.
/// </summary>
public static class ResponseExtensions
{
    /// <summary>
    /// Clears the HTTP response.
    /// <para>
    /// This invocation resets the response headers, response status code, and response body.
    /// </para>
    /// </summary>
    /// <param name="response">The <see cref="HttpResponse"/> to clear.</param>
    public static void Clear(this HttpResponse response)
    {
        if (response.HasStarted)
        {
            throw new InvalidOperationException("The response cannot be cleared, it has already started sending.");
        }
        response.StatusCode = 200;
        response.HttpContext.Features.GetRequiredFeature<IHttpResponseFeature>().ReasonPhrase = null;
        response.Headers.Clear();
        if (response.Body.CanSeek)
        {
            response.Body.SetLength(0);
        }
    }

    /// <summary>
    /// Returns a redirect response (HTTP 301, HTTP 302, HTTP 307 or HTTP 308) to the client.
    /// </summary>
    /// <param name="response">The <see cref="HttpResponse"/> to redirect.</param>
    /// <param name="location">The URL to redirect the client to. This must be properly encoded for use in http headers where only ASCII characters are allowed.</param>
    /// <param name="permanent"><c>True</c> if the redirect is permanent (301 or 308), otherwise <c>false</c> (302 or 307).</param>
    /// <param name="preserveMethod"><c>True</c> if the redirect needs to reuse the method and body (308 or 307), otherwise <c>false</c> (301 or 302).</param>
    public static void Redirect(this HttpResponse response, string location, bool permanent, bool preserveMethod)
    {
        if (preserveMethod)
        {
            response.StatusCode = permanent ? StatusCodes.Status308PermanentRedirect : StatusCodes.Status307TemporaryRedirect;
        }
        else
        {
            response.StatusCode = permanent ? StatusCodes.Status301MovedPermanently : StatusCodes.Status302Found;
        }

        response.Headers.Location = location;
    }
}
