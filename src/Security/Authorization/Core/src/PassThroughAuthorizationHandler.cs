// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authorization.Infrastructure;

/// <summary>
/// Infrastructure class which allows an <see cref="IAuthorizationRequirement"/> to
/// be its own <see cref="IAuthorizationHandler"/>.
/// </summary>
public class PassThroughAuthorizationHandler : IAuthorizationHandler
{
    private readonly AuthorizationOptions _options;

    /// <summary>
    /// Creates a new instance of <see cref="PassThroughAuthorizationHandler"/>.
    /// </summary>
    public PassThroughAuthorizationHandler() : this(Options.Create(new AuthorizationOptions()))
    { }

    /// <summary>
    /// Creates a new instance of <see cref="PassThroughAuthorizationHandler"/>.
    /// </summary>
    /// <param name="options">The <see cref="AuthorizationOptions"/> used.</param>
    public PassThroughAuthorizationHandler(IOptions<AuthorizationOptions> options)
        => _options = options.Value;

    /// <summary>
    /// Makes a decision if authorization is allowed.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        foreach (var handler in context.Requirements.OfType<IAuthorizationHandler>())
        {
            await handler.HandleAsync(context).ConfigureAwait(false);
            if (!_options.InvokeHandlersAfterFailure && context.HasFailed)
            {
                break;
            }
        }
    }
}
