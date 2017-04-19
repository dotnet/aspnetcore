// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect
{
    public class OpenIdConnectEventTests
    {
        private readonly Func<MessageReceivedContext, Task> MessageNotImpl = context => { throw new NotImplementedException("Message"); };
        private readonly Func<TokenValidatedContext, Task> TokenNotImpl = context => { throw new NotImplementedException("Token"); };
        private readonly Func<AuthorizationCodeReceivedContext, Task> CodeNotImpl = context => { throw new NotImplementedException("Code"); };
        private readonly Func<TokenResponseReceivedContext, Task> TokenResponseNotImpl = context => { throw new NotImplementedException("TokenResponse"); };
        private readonly Func<UserInformationReceivedContext, Task> UserNotImpl = context => { throw new NotImplementedException("User"); };
        private readonly Func<AuthenticationFailedContext, Task> FailedNotImpl = context => { throw new NotImplementedException("Failed", context.Exception); };
        private readonly Func<TicketReceivedContext, Task> TicketNotImpl = context => { throw new NotImplementedException("Ticket"); };
        private readonly Func<FailureContext, Task> FailureNotImpl = context => { throw new NotImplementedException("Failure", context.Failure); };
        private readonly Func<RedirectContext, Task> RedirectNotImpl = context => { throw new NotImplementedException("Redirect"); };
        private readonly Func<RemoteSignOutContext, Task> RemoteSignOutNotImpl = context => { throw new NotImplementedException("Remote"); };
        private readonly RequestDelegate AppNotImpl = context => { throw new NotImplementedException("App"); };

        [Fact]
        public async Task OnMessageReceived_Skip_NoMoreEventsRun()
        {
            var messageReceived = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    context.Skip();
                    return Task.FromResult(0);
                },
                OnTokenValidated = TokenNotImpl,
                OnAuthorizationCodeReceived = CodeNotImpl,
                OnTokenResponseReceived = TokenResponseNotImpl,
                OnUserInformationReceived = UserNotImpl,
                OnAuthenticationFailed = FailedNotImpl,
                OnTicketReceived = TicketNotImpl,
                OnRemoteFailure = FailureNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var response = await PostAsync(server, "signin-oidc", "");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
        }

        [Fact]
        public async Task OnMessageReceived_Handled_NoMoreEventsRun()
        {
            var messageReceived = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                },
                OnTokenValidated = TokenNotImpl,
                OnAuthorizationCodeReceived = CodeNotImpl,
                OnTokenResponseReceived = TokenResponseNotImpl,
                OnUserInformationReceived = UserNotImpl,
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = TicketNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            AppNotImpl);

            var response = await PostAsync(server, "signin-oidc", "");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
        }

        [Fact]
        public async Task OnTokenValidated_Skip_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    context.Skip();
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = CodeNotImpl,
                OnTokenResponseReceived = TokenResponseNotImpl,
                OnUserInformationReceived = UserNotImpl,
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = TicketNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
        }

        [Fact]
        public async Task OnTokenValidated_HandledWithoutTicket_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    context.HandleResponse();
                    context.Ticket = null;
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = CodeNotImpl,
                OnTokenResponseReceived = TokenResponseNotImpl,
                OnUserInformationReceived = UserNotImpl,
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = TicketNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            AppNotImpl);

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
        }

        // TODO: Do any other events depend on the presence of the ticket? It's strange we have to double handle this event.
        [Fact]
        public async Task OnTokenValidated_HandledWithTicket_SkipToTicketReceived()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var ticketReceived = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    context.HandleResponse();
                    // context.Ticket = null;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = CodeNotImpl,
                OnTokenResponseReceived = TokenResponseNotImpl,
                OnUserInformationReceived = UserNotImpl,
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = context =>
                {
                    ticketReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                },

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            AppNotImpl);

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(ticketReceived);
        }

        [Fact]
        public async Task OnAuthorizationCodeReceived_Skip_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    context.Skip();
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = TokenResponseNotImpl,
                OnUserInformationReceived = UserNotImpl,
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = TicketNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
        }

        [Fact]
        public async Task OnAuthorizationCodeReceived_HandledWithoutTicket_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    context.HandleResponse();
                    context.Ticket = null;
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = TokenResponseNotImpl,
                OnUserInformationReceived = UserNotImpl,
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = TicketNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            AppNotImpl);

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
        }

        [Fact]
        public async Task OnAuthorizationCodeReceived_HandledWithTicket_SkipToTicketReceived()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var ticketReceived = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    context.HandleResponse();
                    // context.Ticket = null;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = TokenResponseNotImpl,
                OnUserInformationReceived = UserNotImpl,
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = context =>
                {
                    ticketReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                },

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            AppNotImpl);

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(ticketReceived);
        }

        [Fact]
        public async Task OnTokenResponseReceived_Skip_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    context.Skip();
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = UserNotImpl,
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = TicketNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
        }

        [Fact]
        public async Task OnTokenResponseReceived_HandledWithoutTicket_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    context.Ticket = null;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = UserNotImpl,
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = TicketNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            AppNotImpl);

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
        }

        [Fact]
        public async Task OnTokenResponseReceived_HandledWithTicket_SkipToTicketReceived()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var ticketReceived = false;
            var tokenResponseReceived = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    // context.Ticket = null;
                    context.HandleResponse();
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = UserNotImpl,
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = context =>
                {
                    ticketReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                },

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            AppNotImpl);

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(ticketReceived);
        }

        [Fact]
        public async Task OnTokenValidatedBackchannel_Skip_NoMoreEventsRun()
        {
            var messageReceived = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var tokenValidated = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    context.Skip();
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = UserNotImpl,
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = TicketNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var response = await PostAsync(server, "signin-oidc", "state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(tokenValidated);
        }

        [Fact]
        public async Task OnTokenValidatedBackchannel_HandledWithoutTicket_NoMoreEventsRun()
        {
            var messageReceived = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var tokenValidated = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    context.Ticket = null;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = UserNotImpl,
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = TicketNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            AppNotImpl);

            var response = await PostAsync(server, "signin-oidc", "state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(tokenValidated);
        }

        [Fact]
        public async Task OnTokenValidatedBackchannel_HandledWithTicket_SkipToTicketReceived()
        {
            var messageReceived = false;
            var codeReceived = false;
            var ticketReceived = false;
            var tokenResponseReceived = false;
            var tokenValidated = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    // context.Ticket = null;
                    context.HandleResponse();
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = UserNotImpl,
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = context =>
                {
                    ticketReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                },

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            AppNotImpl);

            var response = await PostAsync(server, "signin-oidc", "state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(tokenValidated);
            Assert.True(ticketReceived);
        }

        [Fact]
        public async Task OnUserInformationReceived_Skip_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var userInfoReceived = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    context.Skip();
                    return Task.FromResult(0);
                },
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = TicketNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(userInfoReceived);
        }

        [Fact]
        public async Task OnUserInformationReceived_HandledWithoutTicket_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var userInfoReceived = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    context.Ticket = null;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                },
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = TicketNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            AppNotImpl);

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(userInfoReceived);
        }

        [Fact]
        public async Task OnUserInformationReceived_HandledWithTicket_SkipToTicketReceived()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var ticketReceived = false;
            var tokenResponseReceived = false;
            var userInfoReceived = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    // context.Ticket = null;
                    context.HandleResponse();
                    return Task.FromResult(0);
                },
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = context =>
                {
                    ticketReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                },

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            AppNotImpl);

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(userInfoReceived);
            Assert.True(ticketReceived);
        }

        [Fact]
        public async Task OnAuthenticationFailed_Skip_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var userInfoReceived = false;
            var authFailed = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    throw new NotImplementedException("TestException");
                },
                OnAuthenticationFailed = context =>
                {
                    authFailed = true;
                    Assert.Equal("TestException", context.Exception.Message);
                    context.Skip();
                    return Task.FromResult(0);
                },
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = TicketNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(userInfoReceived);
            Assert.True(authFailed);
        }

        [Fact]
        public async Task OnAuthenticationFailed_HandledWithoutTicket_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var userInfoReceived = false;
            var authFailed = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    throw new NotImplementedException("TestException");
                },
                OnAuthenticationFailed = context =>
                {
                    authFailed = true;
                    Assert.Equal("TestException", context.Exception.Message);
                    Assert.Null(context.Ticket);
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                },
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = TicketNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            AppNotImpl);

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(userInfoReceived);
            Assert.True(authFailed);
        }

        [Fact]
        public async Task OnAuthenticationFailed_HandledWithTicket_SkipToTicketReceived()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var ticketReceived = false;
            var tokenResponseReceived = false;
            var userInfoReceived = false;
            var authFailed = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    throw new NotImplementedException("TestException");
                },
                OnAuthenticationFailed = context =>
                {
                    authFailed = true;
                    Assert.Equal("TestException", context.Exception.Message);
                    Assert.Null(context.Ticket);

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "Bob le Magnifique"),
                        new Claim(ClaimTypes.Email, "bob@contoso.com"),
                        new Claim(ClaimsIdentity.DefaultNameClaimType, "bob")
                    };

                    context.Ticket = new AuthenticationTicket(
                        new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name)),
                        new AuthenticationProperties(), context.Scheme.Name);

                    context.HandleResponse();
                    return Task.FromResult(0);
                },
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = context =>
                {
                    ticketReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                },

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            AppNotImpl);

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(userInfoReceived);
            Assert.True(authFailed);
            Assert.True(ticketReceived);
        }

        [Fact]
        public async Task OnRemoteFailure_Skip_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var userInfoReceived = false;
            var authFailed = false;
            var remoteFailure = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    throw new NotImplementedException("TestException");
                },
                OnAuthenticationFailed = context =>
                {
                    authFailed = true;
                    Assert.Equal("TestException", context.Exception.Message);
                    return Task.FromResult(0);
                },
                OnRemoteFailure = context =>
                {
                    remoteFailure = true;
                    Assert.Equal("TestException", context.Failure.Message);
                    context.Skip();
                    return Task.FromResult(0);
                },
                OnTicketReceived = TicketNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(userInfoReceived);
            Assert.True(authFailed);
            Assert.True(remoteFailure);
        }

        [Fact]
        public async Task OnRemoteFailure_Handled_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var userInfoReceived = false;
            var authFailed = false;
            var remoteFailure = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    throw new NotImplementedException("TestException");
                },
                OnAuthenticationFailed = context =>
                {
                    authFailed = true;
                    Assert.Equal("TestException", context.Exception.Message);
                    return Task.FromResult(0);
                },
                OnRemoteFailure = context =>
                {
                    remoteFailure = true;
                    Assert.Equal("TestException", context.Failure.Message);
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                },
                OnTicketReceived = TicketNotImpl,

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            AppNotImpl);

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(userInfoReceived);
            Assert.True(authFailed);
            Assert.True(remoteFailure);
        }

        [Fact]
        public async Task OnTicketReceived_Skip_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var userInfoReceived = false;
            var ticektReceived = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    return Task.FromResult(0);
                },
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = context =>
                {
                    ticektReceived = true;
                    context.Skip();
                    return Task.FromResult(0);
                },

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(userInfoReceived);
            Assert.True(ticektReceived);
        }

        [Fact]
        public async Task OnTicketReceived_Handled_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var userInfoReceived = false;
            var ticektReceived = false;
            var server = CreateServer(new OpenIdConnectEvents()
            {
                OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                },
                OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                },
                OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                },
                OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    return Task.FromResult(0);
                },
                OnAuthenticationFailed = FailedNotImpl,
                OnRemoteFailure = FailureNotImpl,
                OnTicketReceived = context =>
                {
                    ticektReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                },

                OnRedirectToIdentityProvider = RedirectNotImpl,
                OnRedirectToIdentityProviderForSignOut = RedirectNotImpl,
                OnRemoteSignOut = RemoteSignOutNotImpl,
            },
            AppNotImpl);

            var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Equal("", await response.Content.ReadAsStringAsync());
            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(userInfoReceived);
            Assert.True(ticektReceived);
        }

        private TestServer CreateServer(OpenIdConnectEvents events, RequestDelegate appCode)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddCookieAuthentication();
                    services.AddOpenIdConnectAuthentication(o =>
                    {
                        o.Events = events;
                        o.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        o.ClientId = "ClientId";
                        o.GetClaimsFromUserInfoEndpoint = true;
                        o.Configuration = new OpenIdConnectConfiguration()
                        {
                            TokenEndpoint = "http://testhost/tokens",
                            UserInfoEndpoint = "http://testhost/user",
                        };
                        o.StateDataFormat = new TestStateDataFormat();
                        o.SecurityTokenValidator = new TestTokenValidator();
                        o.ProtocolValidator = new TestProtocolValidator();
                        o.BackchannelHttpHandler = new TestBackchannel();
                    });
                })
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Run(appCode);
                });

            return new TestServer(builder);
        }

        private Task<HttpResponseMessage> PostAsync(TestServer server, string path, string form)
        {
            var client = server.CreateClient();
            var cookie = ".AspNetCore.Correlation." + OpenIdConnectDefaults.AuthenticationScheme + ".corrilationId=N";
            client.DefaultRequestHeaders.Add("Cookie", cookie);
            return client.PostAsync("signin-oidc",
                new StringContent(form, Encoding.ASCII, "application/x-www-form-urlencoded"));
        }

        private class TestStateDataFormat : ISecureDataFormat<AuthenticationProperties>
        {
            private AuthenticationProperties Data { get; set; }

            public string Protect(AuthenticationProperties data)
            {
                throw new NotImplementedException();
            }

            public string Protect(AuthenticationProperties data, string purpose)
            {
                throw new NotImplementedException();
            }

            public AuthenticationProperties Unprotect(string protectedText)
            {
                Assert.Equal("protected_state", protectedText);
                return new AuthenticationProperties(new Dictionary<string, string>()
                {
                    { ".xsrf", "corrilationId" },
                    { OpenIdConnectDefaults.RedirectUriForCodePropertiesKey, "redirect_uri" }
                });
            }

            public AuthenticationProperties Unprotect(string protectedText, string purpose)
            {
                throw new NotImplementedException();
            }
        }

        private class TestTokenValidator : ISecurityTokenValidator
        {
            public bool CanValidateToken => true;

            public int MaximumTokenSizeInBytes
            {
                get { return 1024; }
                set { throw new NotImplementedException(); }
            }

            public bool CanReadToken(string securityToken)
            {
                Assert.Equal("my_id_token", securityToken);
                return true;
            }

            public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
            {
                Assert.Equal("my_id_token", securityToken);
                validatedToken = new JwtSecurityToken();
                return new ClaimsPrincipal(new ClaimsIdentity("customAuthType"));
            }
        }

        private class TestProtocolValidator : OpenIdConnectProtocolValidator
        {
            public override void ValidateAuthenticationResponse(OpenIdConnectProtocolValidationContext validationContext)
            {
            }

            public override void ValidateTokenResponse(OpenIdConnectProtocolValidationContext validationContext)
            {
            }

            public override void ValidateUserInfoResponse(OpenIdConnectProtocolValidationContext validationContext)
            {
            }
        }

        private class TestBackchannel : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                if (string.Equals("/tokens", request.RequestUri.AbsolutePath, StringComparison.Ordinal))
                {
                    return Task.FromResult(new HttpResponseMessage() { Content =
                       new StringContent("{ \"id_token\": \"my_id_token\", \"access_token\": \"my_access_token\" }", Encoding.ASCII, "application/json") });
                }
                if (string.Equals("/user", request.RequestUri.AbsolutePath, StringComparison.Ordinal))
                {
                    return Task.FromResult(new HttpResponseMessage() { Content = new StringContent("{ }", Encoding.ASCII, "application/json") });
                }

                throw new NotImplementedException(request.RequestUri.ToString());
            }
        }
    }
}
