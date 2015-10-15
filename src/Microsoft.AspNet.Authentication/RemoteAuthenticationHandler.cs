// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Authentication
{
    public abstract class RemoteAuthenticationHandler<TOptions> : AuthenticationHandler<TOptions> where TOptions : RemoteAuthenticationOptions
    {
        public override async Task<bool> HandleRequestAsync()
        {
            if (Options.CallbackPath == Request.Path)
            {
                return await HandleRemoteCallbackAsync();
            }
            return false;
        }

        protected virtual async Task<bool> HandleRemoteCallbackAsync()
        {
            var authResult = await HandleRemoteAuthenticateAsync();
            if (authResult == null || !authResult.Succeeded)
            {
                var errorContext = new ErrorContext(Context, authResult?.Error ?? new Exception("Invalid return state, unable to redirect."));
                Logger.LogInformation("Error from RemoteAuthentication: " + errorContext.Error.Message);
                await Options.Events.RemoteError(errorContext);
                if (errorContext.HandledResponse)
                {
                    return true;
                }
                if (errorContext.Skipped)
                {
                    return false;
                }

                Context.Response.StatusCode = 500;
                return true;
            }

            // We have a ticket if we get here
            var ticket = authResult.Ticket;
            var context = new TicketReceivedContext(Context, Options, ticket)
            {
                ReturnUri = ticket.Properties.RedirectUri,
            };
            // REVIEW: is this safe or good?
            ticket.Properties.RedirectUri = null;

            await Options.Events.TicketReceived(context);

            if (context.HandledResponse)
            {
                Logger.LogVerbose("The SigningIn event returned Handled.");
                return true;
            }
            else if (context.Skipped)
            {
                Logger.LogVerbose("The SigningIn event returned Skipped.");
                return false;
            }

            await Context.Authentication.SignInAsync(Options.SignInScheme, context.Principal, context.Properties);

            // Default redirect path is the base path
            if (string.IsNullOrEmpty(context.ReturnUri))
            {
                context.ReturnUri = "/";
            }

            Response.Redirect(context.ReturnUri);
            return true;
        }

        protected abstract Task<AuthenticateResult> HandleRemoteAuthenticateAsync();

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            return Task.FromResult(AuthenticateResult.Failed("Remote authentication does not support authenticate"));
        }

        protected override Task HandleSignOutAsync(SignOutContext context)
        {
            throw new NotSupportedException();
        }

        protected override Task HandleSignInAsync(SignInContext context)
        {
            throw new NotSupportedException();
        }

        protected override Task<bool> HandleForbiddenAsync(ChallengeContext context)
        {
            throw new NotSupportedException();
        }
    }
}