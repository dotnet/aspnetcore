// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Implementation of <see cref="IAuthenticationHandlerProvider"/>.
/// </summary>
public class AuthenticationHandlerProvider : IAuthenticationHandlerProvider
{
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="schemes">The <see cref="IAuthenticationHandlerProvider"/>.</param>
    public AuthenticationHandlerProvider(IAuthenticationSchemeProvider schemes)
    {
        Schemes = schemes;
    }

    /// <summary>
    /// The <see cref="IAuthenticationHandlerProvider"/>.
    /// </summary>
    public IAuthenticationSchemeProvider Schemes { get; }

    // handler instance cache, need to initialize once per request
    private readonly Dictionary<string, IAuthenticationHandler> _handlerMap = new Dictionary<string, IAuthenticationHandler>(StringComparer.Ordinal);

    /// <summary>
    /// Returns the handler instance that will be used.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="authenticationScheme">The name of the authentication scheme being handled.</param>
    /// <returns>The handler instance.</returns>
    public async Task<IAuthenticationHandler?> GetHandlerAsync(HttpContext context, string authenticationScheme)
    {
        if (_handlerMap.TryGetValue(authenticationScheme, out var value))
        {
            return value;
        }

        var scheme = await Schemes.GetSchemeAsync(authenticationScheme);
        if (scheme == null)
        {
            return null;
        }
        var handler = (context.RequestServices.GetService(scheme.HandlerType) ??
            ActivatorUtilities.CreateInstance(context.RequestServices, scheme.HandlerType))
            as IAuthenticationHandler;
        if (handler != null)
        {
            await handler.InitializeAsync(scheme, context);
            _handlerMap[authenticationScheme] = handler;
        }
        return handler;
    }
}
