// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CustomAuthorizationFailureResponse.Authorization.Requirements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace CustomAuthorizationFailureResponse.Authorization;

public class SampleAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly IAuthorizationMiddlewareResultHandler _handler;

    public SampleAuthorizationMiddlewareResultHandler()
    {
        _handler = new AuthorizationMiddlewareResultHandler();
    }

    public async Task HandleAsync(
        RequestDelegate requestDelegate,
        HttpContext httpContext,
        AuthorizationPolicy authorizationPolicy,
        PolicyAuthorizationResult policyAuthorizationResult)
    {
        // if the authorization was forbidden, let's use custom logic to handle that.
        if (policyAuthorizationResult.Forbidden && policyAuthorizationResult.AuthorizationFailure != null)
        {
            if (policyAuthorizationResult.AuthorizationFailure.FailureReasons.Any())
            {
                await httpContext.Response.WriteAsync(policyAuthorizationResult.AuthorizationFailure.FailureReasons.First().Message);

                // return right away as the default implementation would overwrite the status code
                return;
            }

            // as an example, let's return 404 if specific requirement has failed
            if (policyAuthorizationResult.AuthorizationFailure.FailedRequirements.Any(requirement => requirement is SampleRequirement))
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await httpContext.Response.WriteAsync(Startup.CustomForbiddenMessage);

                // return right away as the default implementation would overwrite the status code
                return;
            }
            else if (policyAuthorizationResult.AuthorizationFailure.FailedRequirements.Any(requirement => requirement is SampleWithCustomMessageRequirement))
            {
                // if other requirements failed, let's just use a custom message
                // but we have to use OnStarting callback because the default handlers will want to modify i.e. status code of the response
                // and modifications of the response are not allowed once the writing has started
                var message = Startup.CustomForbiddenMessage;

                httpContext.Response.OnStarting(() => httpContext.Response.BodyWriter.WriteAsync(Encoding.UTF8.GetBytes(message)).AsTask());
            }
        }

        await _handler.HandleAsync(requestDelegate, httpContext, authorizationPolicy, policyAuthorizationResult);
    }
}
