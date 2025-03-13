// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints.Forms;

internal class EndpointAntiforgeryStateProvider(IAntiforgery antiforgery) : DefaultAntiforgeryStateProvider()
{
    private HttpContext? _context;

    internal void SetRequestContext(HttpContext context)
    {
        _context = context;
    }

    public override AntiforgeryRequestToken? GetAntiforgeryToken()
    {
        if (_context == null)
        {
            // We're in an interactive context. Use the token persisted during static rendering.
            return _currentToken;
        }

        // We already have a callback setup to generate the token when the response starts if needed.
        // If we need the tokens before we start streaming the response, we'll generate and store them;
        // otherwise we'll just retrieve them.
        // In case there are no tokens available, we are going to return null and no-op.
        var tokens = !_context.Response.HasStarted ? antiforgery.GetAndStoreTokens(_context) : antiforgery.GetTokens(_context);
        if (tokens.RequestToken is null)
        {
            return null;
        }

        _currentToken = new AntiforgeryRequestToken(tokens.RequestToken, tokens.FormFieldName);
        return _currentToken;
    }
}
