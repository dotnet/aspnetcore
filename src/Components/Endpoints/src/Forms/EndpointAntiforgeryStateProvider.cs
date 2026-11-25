// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Components.Endpoints.Forms;

internal class EndpointAntiforgeryStateProvider(IAntiforgery antiforgery) : DefaultAntiforgeryStateProvider()
{
    private HttpContext? _context;
    private bool _canGenerateToken;

    internal void SetRequestContext(HttpContext context)
    {
        _context = context;
        _canGenerateToken = context.Items.ContainsKey(MiddlewareInvokedKeys.Antiforgery);
    }

    public override AntiforgeryRequestToken? GetAntiforgeryToken()
    {
        if (_context == null)
        {
            // We're in an interactive context. Use the token persisted during static rendering.
            return _currentToken;
        }

        if (_currentToken == null && _canGenerateToken)
        {
            var tokens = !_context.Response.HasStarted ? antiforgery.GetAndStoreTokens(_context) : antiforgery.GetTokens(_context);
            // A token requiring a new cookie needs a separate response once headers have been sent.
            if (tokens.RequestToken is null || (_context.Response.HasStarted && tokens.CookieToken is not null))
            {
                return null;
            }

            _currentToken = new AntiforgeryRequestToken(tokens.RequestToken, tokens.FormFieldName);
        }

        return _currentToken;
    }
}
