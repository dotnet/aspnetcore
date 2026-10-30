// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
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
        var requirements = context.Requirements;

        // AuthorizationPolicy.Requirements is a ReadOnlyCollection; index into it to avoid allocating an iterator.
        if (requirements is ReadOnlyCollection<IAuthorizationRequirement> requirementsList)
        {
            for (var i = 0; i < requirementsList.Count; i++)
            {
                if (requirementsList[i] is IAuthorizationHandler handler)
                {
                    await handler.HandleAsync(context).ConfigureAwait(false);
                    if (!_options.InvokeHandlersAfterFailure && context.HasFailed)
                    {
                        break;
                    }
                }
            }
        }
        else
        {
            foreach (var handler in requirements.OfType<IAuthorizationHandler>())
            {
                await handler.HandleAsync(context).ConfigureAwait(false);
                if (!_options.InvokeHandlersAfterFailure && context.HasFailed)
                {
                    break;
                }
            }
        }
    }
}
