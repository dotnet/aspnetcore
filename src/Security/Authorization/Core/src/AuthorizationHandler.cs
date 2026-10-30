// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// Base class for authorization handlers that need to be called for a specific requirement type.
/// </summary>
/// <typeparam name="TRequirement">The type of the requirement to handle.</typeparam>
public abstract class AuthorizationHandler<TRequirement> : IAuthorizationHandler
        where TRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Makes a decision if authorization is allowed.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    public virtual async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var requirements = context.Requirements;

        // AuthorizationPolicy.Requirements is a ReadOnlyCollection; index into it to avoid allocating an iterator.
        if (requirements is ReadOnlyCollection<IAuthorizationRequirement> requirementsList)
        {
            for (var i = 0; i < requirementsList.Count; i++)
            {
                if (requirementsList[i] is TRequirement req)
                {
                    await HandleRequirementAsync(context, req).ConfigureAwait(false);
                }
            }
        }
        else
        {
            foreach (var req in requirements.OfType<TRequirement>())
            {
                await HandleRequirementAsync(context, req).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Makes a decision if authorization is allowed based on a specific requirement.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <param name="requirement">The requirement to evaluate.</param>
    protected abstract Task HandleRequirementAsync(AuthorizationHandlerContext context, TRequirement requirement);
}

/// <summary>
/// Base class for authorization handlers that need to be called for specific requirement and
/// resource types.
/// </summary>
/// <typeparam name="TRequirement">The type of the requirement to evaluate.</typeparam>
/// <typeparam name="TResource">The type of the resource to evaluate.</typeparam>
public abstract class AuthorizationHandler<TRequirement, TResource> : IAuthorizationHandler
    where TRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Makes a decision if authorization is allowed.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    public virtual async Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (context.Resource is TResource resource)
        {
            var requirements = context.Requirements;

            // AuthorizationPolicy.Requirements is a ReadOnlyCollection; index into it to avoid allocating an iterator.
            if (requirements is ReadOnlyCollection<IAuthorizationRequirement> requirementsList)
            {
                for (var i = 0; i < requirementsList.Count; i++)
                {
                    if (requirementsList[i] is TRequirement req)
                    {
                        await HandleRequirementAsync(context, req, resource).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                foreach (var req in requirements.OfType<TRequirement>())
                {
                    await HandleRequirementAsync(context, req, resource).ConfigureAwait(false);
                }
            }
        }
    }

    /// <summary>
    /// Makes a decision if authorization is allowed based on a specific requirement and resource.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <param name="requirement">The requirement to evaluate.</param>
    /// <param name="resource">The resource to evaluate.</param>
    protected abstract Task HandleRequirementAsync(AuthorizationHandlerContext context, TRequirement requirement, TResource resource);
}
