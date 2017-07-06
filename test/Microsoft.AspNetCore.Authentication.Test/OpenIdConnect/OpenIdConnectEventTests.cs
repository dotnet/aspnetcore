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
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
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
        private readonly Func<RemoteFailureContext, Task> FailureNotImpl = context => { throw new NotImplementedException("Failure", context.Failure); };
        private readonly Func<RedirectContext, Task> RedirectNotImpl = context => { throw new NotImplementedException("Redirect"); };
        private readonly Func<RemoteSignOutContext, Task> RemoteSignOutNotImpl = context => { throw new NotImplementedException("Remote"); };
        private readonly Func<RemoteSignOutContext, Task> SignedOutCallbackNotImpl = context => { throw new NotImplementedException("SingedOut"); };
        private readonly RequestDelegate AppNotImpl = context => { throw new NotImplementedException("App"); };

        [Fact]
        public async Task OnMessageReceived_Skip_NoMoreEventsRun()
        {
            var messageReceived = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    context.SkipHandler();
                    return Task.FromResult(0);
                };
            }),
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
        public async Task OnMessageReceived_Fail_NoMoreEventsRun()
        {
            var messageReceived = false;
            var remoteFailure = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    context.Fail("Authentication was aborted from user code.");
                    return Task.FromResult(0);
                };
                events.OnRemoteFailure = context =>
                {
                    remoteFailure = true;
                    return Task.FromResult(0);
                };
            }),
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var exception = await Assert.ThrowsAsync<Exception>(delegate
            {
                return PostAsync(server, "signin-oidc", "");
            });

            Assert.Equal("Authentication was aborted from user code.", exception.InnerException.Message);

            Assert.True(messageReceived);
            Assert.True(remoteFailure);
        }

        [Fact]
        public async Task OnMessageReceived_Handled_NoMoreEventsRun()
        {
            var messageReceived = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    context.SkipHandler();
                    return Task.FromResult(0);
                };
            }),
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
        public async Task OnTokenValidated_Fail_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var remoteFailure = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    context.Fail("Authentication was aborted from user code.");
                    return Task.FromResult(0);
                };
                events.OnRemoteFailure = context =>
                {
                    remoteFailure = true;
                    return Task.FromResult(0);
                };
            }),
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var exception = await Assert.ThrowsAsync<Exception>(delegate
            {
                return PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state");
            });

            Assert.Equal("Authentication was aborted from user code.", exception.InnerException.Message);

            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(remoteFailure);
        }

        [Fact]
        public async Task OnTokenValidated_HandledWithoutTicket_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    context.HandleResponse();
                    context.Principal = null;
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    context.Success();
                    return Task.FromResult(0);
                };
                events.OnTicketReceived = context =>
                {
                    ticketReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    context.SkipHandler();
                    return Task.FromResult(0);
                };
            }),
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
        public async Task OnAuthorizationCodeReceived_Fail_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var remoteFailure = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    context.Fail("Authentication was aborted from user code.");
                    return Task.FromResult(0);
                };
                events.OnRemoteFailure = context =>
                {
                    remoteFailure = true;
                    return Task.FromResult(0);
                };
            }),
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var exception = await Assert.ThrowsAsync<Exception>(delegate
            {
                return PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");
            });

            Assert.Equal("Authentication was aborted from user code.", exception.InnerException.Message);

            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(remoteFailure);
        }

        [Fact]
        public async Task OnAuthorizationCodeReceived_HandledWithoutTicket_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    context.HandleResponse();
                    context.Principal = null;
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    context.Success();
                    return Task.FromResult(0);
                };
                events.OnTicketReceived = context =>
                {
                    ticketReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    context.SkipHandler();
                    return Task.FromResult(0);
                };
            }),
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
        public async Task OnTokenResponseReceived_Fail_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var remoteFailure = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    context.Fail("Authentication was aborted from user code.");
                    return Task.FromResult(0);
                };
                events.OnRemoteFailure = context =>
                {
                    remoteFailure = true;
                    return Task.FromResult(0);
                };
            }),
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var exception = await Assert.ThrowsAsync<Exception>(delegate
            {
                return PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");
            });

            Assert.Equal("Authentication was aborted from user code.", exception.InnerException.Message);

            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(remoteFailure);
        }

        [Fact]
        public async Task OnTokenResponseReceived_HandledWithoutTicket_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    context.Principal = null;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    context.Success();
                    return Task.FromResult(0);
                };
                events.OnTicketReceived = context =>
                {
                    ticketReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    context.SkipHandler();
                    return Task.FromResult(0);
                };
            }),
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
        public async Task OnTokenValidatedBackchannel_Fail_NoMoreEventsRun()
        {
            var messageReceived = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var tokenValidated = false;
            var remoteFailure = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    context.Fail("Authentication was aborted from user code.");
                    return Task.FromResult(0);
                };
                events.OnRemoteFailure = context =>
                {
                    remoteFailure = true;
                    return Task.FromResult(0);
                };
            }),
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var exception = await Assert.ThrowsAsync<Exception>(delegate
            {
                return PostAsync(server, "signin-oidc", "state=protected_state&code=my_code");
            });

            Assert.Equal("Authentication was aborted from user code.", exception.InnerException.Message);

            Assert.True(messageReceived);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(tokenValidated);
            Assert.True(remoteFailure);
        }

        [Fact]
        public async Task OnTokenValidatedBackchannel_HandledWithoutTicket_NoMoreEventsRun()
        {
            var messageReceived = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var tokenValidated = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    context.Principal = null;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    context.Success();
                    return Task.FromResult(0);
                };
                events.OnTicketReceived = context =>
                {
                    ticketReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    context.SkipHandler();
                    return Task.FromResult(0);
                };
            }),
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
        public async Task OnUserInformationReceived_Fail_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var userInfoReceived = false;
            var remoteFailure = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    context.Fail("Authentication was aborted from user code.");
                    return Task.FromResult(0);
                };
                events.OnRemoteFailure = context =>
                {
                    remoteFailure = true;
                    return Task.FromResult(0);
                };
            }),
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var exception = await Assert.ThrowsAsync<Exception>(delegate
            {
                return PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");
            });

            Assert.Equal("Authentication was aborted from user code.", exception.InnerException.Message);

            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(userInfoReceived);
            Assert.True(remoteFailure);
        }

        [Fact]
        public async Task OnUserInformationReceived_HandledWithoutTicket_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var userInfoReceived = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    context.Principal = null;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    // context.Ticket = null;
                    context.Success();
                    return Task.FromResult(0);
                };
                events.OnTicketReceived = context =>
                {
                    ticketReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    throw new NotImplementedException("TestException");
                };
                events.OnAuthenticationFailed = context =>
                {
                    authFailed = true;
                    Assert.Equal("TestException", context.Exception.Message);
                    context.SkipHandler();
                    return Task.FromResult(0);
                };
            }),
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
        public async Task OnAuthenticationFailed_Fail_NoMoreEventsRun()
        {
            var messageReceived = false;
            var tokenValidated = false;
            var codeReceived = false;
            var tokenResponseReceived = false;
            var userInfoReceived = false;
            var authFailed = false;
            var remoteFailure = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    throw new NotImplementedException("TestException");
                };
                events.OnAuthenticationFailed = context =>
                {
                    authFailed = true;
                    Assert.Equal("TestException", context.Exception.Message);
                    context.Fail("Authentication was aborted from user code.");
                    return Task.FromResult(0);
                };
                events.OnRemoteFailure = context =>
                {
                    remoteFailure = true;
                    return Task.FromResult(0);
                };
            }),
            context =>
            {
                return context.Response.WriteAsync(context.Request.Path);
            });

            var exception = await Assert.ThrowsAsync<Exception>(delegate
            {
                return PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");
            });

            Assert.Equal("Authentication was aborted from user code.", exception.InnerException.Message);

            Assert.True(messageReceived);
            Assert.True(tokenValidated);
            Assert.True(codeReceived);
            Assert.True(tokenResponseReceived);
            Assert.True(userInfoReceived);
            Assert.True(authFailed);
            Assert.True(remoteFailure);
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    throw new NotImplementedException("TestException");
                };
                events.OnAuthenticationFailed = context =>
                {
                    authFailed = true;
                    Assert.Equal("TestException", context.Exception.Message);
                    Assert.Null(context.Principal);
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    throw new NotImplementedException("TestException");
                };
                events.OnAuthenticationFailed = context =>
                {
                    authFailed = true;
                    Assert.Equal("TestException", context.Exception.Message);
                    Assert.Null(context.Principal);

                    var claims = new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "Bob le Magnifique"),
                        new Claim(ClaimTypes.Email, "bob@contoso.com"),
                        new Claim(ClaimsIdentity.DefaultNameClaimType, "bob")
                    };

                    context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                    context.Success();
                    return Task.FromResult(0);
                };
                events.OnTicketReceived = context =>
                {
                    ticketReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    throw new NotImplementedException("TestException");
                };
                events.OnAuthenticationFailed = context =>
                {
                    authFailed = true;
                    Assert.Equal("TestException", context.Exception.Message);
                    return Task.FromResult(0);
                };
                events.OnRemoteFailure = context =>
                {
                    remoteFailure = true;
                    Assert.Equal("TestException", context.Failure.Message);
                    context.SkipHandler();
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    throw new NotImplementedException("TestException");
                };
                events.OnAuthenticationFailed = context =>
                {
                    authFailed = true;
                    Assert.Equal("TestException", context.Exception.Message);
                    return Task.FromResult(0);
                };
                events.OnRemoteFailure = context =>
                {
                    remoteFailure = true;
                    Assert.Equal("TestException", context.Failure.Message);
                    Assert.Equal("testvalue", context.Properties.Items["testkey"]);
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTicketReceived = context =>
                {
                    ticektReceived = true;
                    context.SkipHandler();
                    return Task.FromResult(0);
                };
            }),
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
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnMessageReceived = context =>
                {
                    messageReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenValidated = context =>
                {
                    tokenValidated = true;
                    return Task.FromResult(0);
                };
                events.OnAuthorizationCodeReceived = context =>
                {
                    codeReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTokenResponseReceived = context =>
                {
                    tokenResponseReceived = true;
                    return Task.FromResult(0);
                };
                events.OnUserInformationReceived = context =>
                {
                    userInfoReceived = true;
                    return Task.FromResult(0);
                };
                events.OnTicketReceived = context =>
                {
                    ticektReceived = true;
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    return Task.FromResult(0);
                };
            }),
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

        [Fact]
        public async Task OnRedirectToIdentityProviderForSignOut_Invoked()
        {
            var forSignOut = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnRedirectToIdentityProviderForSignOut = context =>
                {
                    forSignOut = true;
                    return Task.CompletedTask;
                };
            }),
            context =>
            {
                return context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            });

            var client = server.CreateClient();
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal("http://testhost/end", response.Headers.Location.GetLeftPart(UriPartial.Path));
            Assert.True(forSignOut);
        }

        [Fact]
        public async Task OnRedirectToIdentityProviderForSignOut_Handled_RedirectNotInvoked()
        {
            var forSignOut = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnRedirectToIdentityProviderForSignOut = context =>
                {
                    forSignOut = true;
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            }),
            context =>
            {
                return context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
            });

            var client = server.CreateClient();
            var response = await client.GetAsync("/");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Null(response.Headers.Location);
            Assert.True(forSignOut);
        }

        [Fact]
        public async Task OnRemoteSignOut_Invoked()
        {
            var forSignOut = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnRemoteSignOut = context =>
                {
                    forSignOut = true;
                    return Task.CompletedTask;
                };
            }),
            AppNotImpl);

            var client = server.CreateClient();
            var response = await client.GetAsync("/signout-oidc");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(forSignOut);
            Assert.True(response.Headers.TryGetValues(HeaderNames.SetCookie, out var values));
            Assert.True(SetCookieHeaderValue.TryParseStrictList(values.ToList(), out var parsedValues));
            Assert.Equal(1, parsedValues.Count);
            Assert.True(StringSegment.IsNullOrEmpty(parsedValues.Single().Value));
        }

        [Fact]
        public async Task OnRemoteSignOut_Handled_NoSignout()
        {
            var forSignOut = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnRemoteSignOut = context =>
                {
                    forSignOut = true;
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            }),
            AppNotImpl);

            var client = server.CreateClient();
            var response = await client.GetAsync("/signout-oidc");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.True(forSignOut);
            Assert.False(response.Headers.TryGetValues(HeaderNames.SetCookie, out var values));
        }

        [Fact]
        public async Task OnRemoteSignOut_Skip_NoSignout()
        {
            var forSignOut = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnRemoteSignOut = context =>
                {
                    forSignOut = true;
                    context.SkipHandler();
                    return Task.CompletedTask;
                };
            }),
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status202Accepted;
                return Task.CompletedTask;
            });

            var client = server.CreateClient();
            var response = await client.GetAsync("/signout-oidc");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.True(forSignOut);
            Assert.False(response.Headers.TryGetValues(HeaderNames.SetCookie, out var values));
        }

        [Fact]
        public async Task OnRedirectToSignedOutRedirectUri_Invoked()
        {
            var forSignOut = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnSignedOutCallbackRedirect = context =>
                {
                    forSignOut = true;
                    return Task.CompletedTask;
                };
            }),
            AppNotImpl);

            var client = server.CreateClient();
            var response = await client.GetAsync("/signout-callback-oidc?state=protected_state");

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            Assert.Equal("http://testhost/redirect", response.Headers.Location.AbsoluteUri);
            Assert.True(forSignOut);
        }

        [Fact]
        public async Task OnRedirectToSignedOutRedirectUri_Handled_NoRedirect()
        {
            var forSignOut = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnSignedOutCallbackRedirect = context =>
                {
                    forSignOut = true;
                    context.Response.StatusCode = StatusCodes.Status202Accepted;
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            }),
            AppNotImpl);

            var client = server.CreateClient();
            var response = await client.GetAsync("/signout-callback-oidc?state=protected_state");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Null(response.Headers.Location);
            Assert.True(forSignOut);
        }

        [Fact]
        public async Task OnRedirectToSignedOutRedirectUri_Skipped_NoRedirect()
        {
            var forSignOut = false;
            var server = CreateServer(CreateNotImpEvents(events =>
            {
                events.OnSignedOutCallbackRedirect = context =>
                {
                    forSignOut = true;
                    context.SkipHandler();
                    return Task.CompletedTask;
                };
            }),
            context =>
            {
                context.Response.StatusCode = StatusCodes.Status202Accepted;
                return Task.CompletedTask;
            });

            var client = server.CreateClient();
            var response = await client.GetAsync("/signout-callback-oidc?state=protected_state");

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            Assert.Null(response.Headers.Location);
            Assert.True(forSignOut);
        }

        private OpenIdConnectEvents CreateNotImpEvents(Action<OpenIdConnectEvents> configureEvents)
        {
            var events = new OpenIdConnectEvents()
            {
                OnMessageReceived = MessageNotImpl,
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
                OnSignedOutCallbackRedirect = SignedOutCallbackNotImpl,
            };
            configureEvents(events);
            return events;
        }

        private TestServer CreateServer(OpenIdConnectEvents events, RequestDelegate appCode)
        {
            var builder = new WebHostBuilder()
                .ConfigureServices(services =>
                {
                    services.AddAuthentication(auth =>
                    {
                        auth.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                        auth.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    })
                        .AddCookie()
                        .AddOpenIdConnect(o =>
                    {
                        o.Events = events;
                        o.ClientId = "ClientId";
                        o.GetClaimsFromUserInfoEndpoint = true;
                        o.Configuration = new OpenIdConnectConfiguration()
                        {
                            TokenEndpoint = "http://testhost/tokens",
                            UserInfoEndpoint = "http://testhost/user",
                            EndSessionEndpoint = "http://testhost/end"
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
                return "protected_state";
            }

            public string Protect(AuthenticationProperties data, string purpose)
            {
                throw new NotImplementedException();
            }

            public AuthenticationProperties Unprotect(string protectedText)
            {
                Assert.Equal("protected_state", protectedText);
                var properties = new AuthenticationProperties(new Dictionary<string, string>()
                {
                    { ".xsrf", "corrilationId" },
                    { OpenIdConnectDefaults.RedirectUriForCodePropertiesKey, "redirect_uri" },
                    { "testkey", "testvalue" }
                });
                properties.RedirectUri = "http://testhost/redirect";
                return properties;
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
