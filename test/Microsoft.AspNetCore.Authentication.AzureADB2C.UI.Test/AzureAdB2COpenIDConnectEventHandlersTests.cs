// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.AzureADB2C.UI
{
    public class AzureADB2COpenIDConnectEventHandlersTests
    {
        [Fact]
        public async Task OnRedirectToIdentityProviderHandler_DoesNothingForTheDefaultPolicy()
        {
            // Arrange
            var handlers = new AzureADB2COpenIDConnectEventHandlers(
                AzureADB2CDefaults.AuthenticationScheme,
                new AzureADB2COptions() { SignUpSignInPolicyId = "B2C_1_SiUpIn" });

            var authenticationProperties = new AuthenticationProperties(new Dictionary<string, string>
            {
                [AzureADB2CDefaults.PolicyKey] = "B2C_1_SiUpIn"
            });
            var redirectContext = new RedirectContext(
                new DefaultHttpContext(),
                new AuthenticationScheme(AzureADB2CDefaults.AuthenticationScheme, "", typeof(OpenIdConnectHandler)),
                new OpenIdConnectOptions(),
                authenticationProperties)
            {
                ProtocolMessage = new OpenIdConnectMessage
                {
                    Scope = OpenIdConnectScope.OpenId,
                    ResponseType = OpenIdConnectResponseType.Code,
                    IssuerAddress = "https://login.microsoftonline.com/tfp/domain.onmicrosoft.com/B2C_1_SiUpIn/v2.0"
                }
            };

            // Act
            await handlers.OnRedirectToIdentityProvider(redirectContext);

            // Assert
            Assert.Equal(OpenIdConnectScope.OpenId, redirectContext.ProtocolMessage.Scope);
            Assert.Equal(OpenIdConnectResponseType.Code, redirectContext.ProtocolMessage.ResponseType);
            Assert.Equal(
                "https://login.microsoftonline.com/tfp/domain.onmicrosoft.com/B2C_1_SiUpIn/v2.0",
                redirectContext.ProtocolMessage.IssuerAddress);
            Assert.True(authenticationProperties.Items.ContainsKey(AzureADB2CDefaults.PolicyKey));
        }

        [Fact]
        public async Task OnRedirectToIdentityProviderHandler_UpdatesRequestForOtherPolicies()
        {
            // Arrange

            var handlers = new AzureADB2COpenIDConnectEventHandlers(
                AzureADB2CDefaults.AuthenticationScheme,
                new AzureADB2COptions() { SignUpSignInPolicyId = "B2C_1_SiUpIn" });

            var authenticationProperties = new AuthenticationProperties(new Dictionary<string, string>
            {
                [AzureADB2CDefaults.PolicyKey] = "B2C_1_EP"
            });
            var redirectContext = new RedirectContext(
                new DefaultHttpContext(),
                new AuthenticationScheme(AzureADB2CDefaults.AuthenticationScheme, "", typeof(OpenIdConnectHandler)),
                new OpenIdConnectOptions(),
                authenticationProperties)
            {
                ProtocolMessage = new OpenIdConnectMessage
                {
                    Scope = OpenIdConnectScope.OpenId,
                    ResponseType = OpenIdConnectResponseType.Code,
                    IssuerAddress = "https://login.microsoftonline.com/tfp/domain.onmicrosoft.com/B2C_1_EP/v2.0"
                }
            };

            // Act
            await handlers.OnRedirectToIdentityProvider(redirectContext);

            // Assert
            Assert.Equal(OpenIdConnectScope.OpenIdProfile, redirectContext.ProtocolMessage.Scope);
            Assert.Equal(OpenIdConnectResponseType.IdToken, redirectContext.ProtocolMessage.ResponseType);
            Assert.Equal(
                "https://login.microsoftonline.com/tfp/domain.onmicrosoft.com/b2c_1_ep/v2.0",
                redirectContext.ProtocolMessage.IssuerAddress);
            Assert.False(authenticationProperties.Items.ContainsKey(AzureADB2CDefaults.PolicyKey));
        }

        [Fact]
        public async Task OnRemoteError_HandlesResponseWhenTryingToResetPasswordFromTheLoginPage()
        {
            // Arrange

            var handlers = new AzureADB2COpenIDConnectEventHandlers(
                AzureADB2CDefaults.AuthenticationScheme,
                new AzureADB2COptions() { SignUpSignInPolicyId = "B2C_1_SiUpIn" });

            var remoteFailureContext = new RemoteFailureContext(
                new DefaultHttpContext(),
                new AuthenticationScheme(
                    AzureADB2CDefaults.AuthenticationScheme,
                    displayName: null,
                    handlerType: typeof(OpenIdConnectHandler)),
                new OpenIdConnectOptions(),
                new OpenIdConnectProtocolException("AADB2C90118"));

            // Act
            await handlers.OnRemoteFailure(remoteFailureContext);

            // Assert
            Assert.Equal(StatusCodes.Status302Found, remoteFailureContext.Response.StatusCode);
            Assert.Equal("/AzureADB2C/Account/ResetPassword/AzureADB2C", remoteFailureContext.Response.Headers[HeaderNames.Location]);
            Assert.True(remoteFailureContext.Result.Handled);
        }

        [Fact]
        public async Task OnRemoteError_HandlesResponseWhenUserCancelsFlowFromTheAzureADB2CUserInterface()
        {
            // Arrange

            var handlers = new AzureADB2COpenIDConnectEventHandlers(
                AzureADB2CDefaults.AuthenticationScheme,
                new AzureADB2COptions() { SignUpSignInPolicyId = "B2C_1_SiUpIn" });

            var remoteFailureContext = new RemoteFailureContext(
                new DefaultHttpContext(),
                new AuthenticationScheme(
                    AzureADB2CDefaults.AuthenticationScheme,
                    displayName: null,
                    handlerType: typeof(OpenIdConnectHandler)),
                new OpenIdConnectOptions(),
                new OpenIdConnectProtocolException("access_denied"));

            // Act
            await handlers.OnRemoteFailure(remoteFailureContext);

            // Assert
            Assert.Equal(StatusCodes.Status302Found, remoteFailureContext.Response.StatusCode);
            Assert.Equal("/", remoteFailureContext.Response.Headers[HeaderNames.Location]);
            Assert.True(remoteFailureContext.Result.Handled);
        }

        [Fact]
        public async Task OnRemoteError_HandlesResponseWhenErrorIsUnknown()
        {
            // Arrange

            var handlers = new AzureADB2COpenIDConnectEventHandlers(
                AzureADB2CDefaults.AuthenticationScheme,
                new AzureADB2COptions() { SignUpSignInPolicyId = "B2C_1_SiUpIn" });

            var remoteFailureContext = new RemoteFailureContext(
                new DefaultHttpContext(),
                new AuthenticationScheme(
                    AzureADB2CDefaults.AuthenticationScheme,
                    displayName: null,
                    handlerType: typeof(OpenIdConnectHandler)),
                new OpenIdConnectOptions(),
                new OpenIdConnectProtocolException("some_other_error"));

            // Act
            await handlers.OnRemoteFailure(remoteFailureContext);

            // Assert
            Assert.Equal(StatusCodes.Status302Found, remoteFailureContext.Response.StatusCode);
            Assert.Equal("/AzureADB2C/Account/Error", remoteFailureContext.Response.Headers[HeaderNames.Location]);
            Assert.True(remoteFailureContext.Result.Handled);
        }
    }
}
