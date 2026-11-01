// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authorization;

internal sealed class DefaultAuthorizationServiceImpl(
    IAuthorizationPolicyProvider policyProvider,
    IAuthorizationHandlerProvider handlers,
    ILogger<DefaultAuthorizationService> logger,
    IAuthorizationHandlerContextFactory contextFactory,
    IAuthorizationEvaluator evaluator,
    IOptions<AuthorizationOptions> options,
    AuthorizationMetrics metrics)
    : DefaultAuthorizationService(policyProvider, handlers, logger, contextFactory, evaluator, options)
{
    public override Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
    {
        var task = base.AuthorizeAsync(user, resource, requirements);

        // Authorization handlers typically complete synchronously. Avoid allocating another task in that case.
        if (task.Status == TaskStatus.RanToCompletion)
        {
            try
            {
                metrics.AuthorizeAttemptCompleted(user, policyName: null, task.Result, exception: null);
            }
            catch (Exception ex)
            {
                // Surface the exception through the returned task, as the async path does.
                return FromException(ExceptionDispatchInfo.Capture(ex));
            }

            return task;
        }

        return AwaitAuthorizeAsync(task, user);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators: used so the exception is propagated exactly as an async method would.
    private static async Task<AuthorizationResult> FromException(ExceptionDispatchInfo exception)
#pragma warning restore CS1998
    {
        exception.Throw();
        return null!;
    }

    private async Task<AuthorizationResult> AwaitAuthorizeAsync(Task<AuthorizationResult> task, ClaimsPrincipal user)
    {
        AuthorizationResult result;
        try
        {
            result = await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            metrics.AuthorizeAttemptCompleted(user, policyName: null, result: null, ex);
            throw;
        }

        metrics.AuthorizeAttemptCompleted(user, policyName: null, result, exception: null);
        return result;
    }

    public override async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
    {
        AuthorizationResult result;
        try
        {
            var policy = await GetPolicyAsync(policyName).ConfigureAwait(false);

            // Note that we deliberately call the base method of the other overload here.
            // This is because the base implementation for this overload dispatches to the other overload,
            // which would cause metrics to be recorded twice.
            result = await base.AuthorizeAsync(user, resource, policy.Requirements).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            metrics.AuthorizeAttemptCompleted(user, policyName, result: null, ex);
            throw;
        }

        metrics.AuthorizeAttemptCompleted(user, policyName, result, exception: null);
        return result;
    }
}
