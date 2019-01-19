// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.AzureADB2C.UI
{
    internal class AzureADB2COpenIDConnectEventHandlers
    {
        private IDictionary<string, string> _policyToIssuerAddress =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public AzureADB2COpenIDConnectEventHandlers(string schemeName, AzureADB2COptions options)
        {
            SchemeName = schemeName;
            Options = options;
        }

        public string SchemeName { get; }

        public AzureADB2COptions Options { get; }

        public Task OnRedirectToIdentityProvider(RedirectContext context)
        {
            var defaultPolicy = Options.DefaultPolicy;
            if (context.Properties.Items.TryGetValue(AzureADB2CDefaults.PolicyKey, out var policy) &&
                !string.IsNullOrEmpty(policy) &&
                !string.Equals(policy, defaultPolicy, StringComparison.OrdinalIgnoreCase))
            {
                context.ProtocolMessage.Scope = OpenIdConnectScope.OpenIdProfile;
                context.ProtocolMessage.ResponseType = OpenIdConnectResponseType.IdToken;
                context.ProtocolMessage.IssuerAddress = BuildIssuerAddress(context, defaultPolicy, policy);
                context.Properties.Items.Remove(AzureADB2CDefaults.PolicyKey);
            }

            return Task.CompletedTask;
        }

        private string BuildIssuerAddress(RedirectContext context, string defaultPolicy, string policy)
        {
            if (!_policyToIssuerAddress.TryGetValue(policy, out var issuerAddress))
            {
                _policyToIssuerAddress[policy] = context.ProtocolMessage.IssuerAddress.ToLowerInvariant()
                    .Replace($"/{defaultPolicy.ToLowerInvariant()}/", $"/{policy.ToLowerInvariant()}/");
            }

            return _policyToIssuerAddress[policy];
        }

        public Task OnRemoteFailure(RemoteFailureContext context)
        {
            context.HandleResponse();
            // Handle the error code that Azure Active Directory B2C throws when trying to reset a password from the login page 
            // because password reset is not supported by a "sign-up or sign-in policy".
            // Below is a sample error message:
            // 'access_denied', error_description: 'AADB2C90118: The user has forgotten their password.
            // Correlation ID: f99deff4-f43b-43cc-b4e7-36141dbaf0a0
            // Timestamp: 2018-03-05 02:49:35Z
            //', error_uri: 'error_uri is null'.
            if (context.Failure is OpenIdConnectProtocolException && context.Failure.Message.Contains("AADB2C90118"))
            {
                // If the user clicked the reset password link, redirect to the reset password route
                context.Response.Redirect($"/AzureADB2C/Account/ResetPassword/{SchemeName}");
            }
            // Access denied errors happen when a user cancels an action on the Azure Active Directory B2C UI. We just redirect back to
            // the main page in that case.
            // Message contains error: 'access_denied', error_description: 'AADB2C90091: The user has cancelled entering self-asserted information.
            // Correlation ID: d01c8878-0732-4eb2-beb8-da82a57432e0
            // Timestamp: 2018-03-05 02:56:49Z
            // ', error_uri: 'error_uri is null'.
            else if (context.Failure is OpenIdConnectProtocolException && context.Failure.Message.Contains("access_denied"))
            {
                context.Response.Redirect("/");
            }
            else
            {
                context.Response.Redirect("/AzureADB2C/Account/Error");
            }

            return Task.CompletedTask;
        }
    }
}
