// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect;

public class OpenIdConnectEventTests_Handlers
{
    private readonly RequestDelegate AppWritePath = context => context.Response.WriteAsync(context.Request.Path);
    private readonly RequestDelegate AppNotImpl = context => { throw new NotImplementedException("App"); };

    [Fact]
    public async Task OnMessageReceived_Skip_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
        };
        events.OnMessageReceived = context =>
        {
            context.SkipHandler();
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var response = await PostAsync(server, "signin-oidc", "");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnMessageReceived_Fail_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectRemoteFailure = true,
        };
        events.OnMessageReceived = context =>
        {
            context.Fail("Authentication was aborted from user code.");
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var exception = await Assert.ThrowsAsync<AuthenticationFailureException>(delegate
        {
            return PostAsync(server, "signin-oidc", "");
        });

        Assert.Equal("Authentication was aborted from user code.", exception.InnerException.Message);
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnMessageReceived_Handled_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
        };
        events.OnMessageReceived = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnTokenValidated_Skip_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
        };
        events.OnTokenValidated = context =>
        {
            context.SkipHandler();
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnTokenValidated_Fail_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectRemoteFailure = true,
        };
        events.OnTokenValidated = context =>
        {
            context.Fail("Authentication was aborted from user code.");
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var exception = await Assert.ThrowsAsync<AuthenticationFailureException>(delegate
        {
            return PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state");
        });

        Assert.Equal("Authentication was aborted from user code.", exception.InnerException.Message);
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnTokenValidated_HandledWithoutTicket_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
        };
        events.OnTokenValidated = context =>
        {
            context.HandleResponse();
            context.Principal = null;
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnTokenValidated_HandledWithTicket_SkipToTicketReceived()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectTicketReceived = true,
        };
        events.OnTokenValidated = context =>
        {
            context.HandleResponse();
            context.Principal = null;
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        events.OnTokenValidated = context =>
        {
            context.Success();
            return Task.FromResult(0);
        };
        events.OnTicketReceived = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnAuthorizationCodeReceived_Skip_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
        };
        events.OnAuthorizationCodeReceived = context =>
        {
            context.SkipHandler();
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnAuthorizationCodeReceived_Fail_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectRemoteFailure = true,
        };
        events.OnAuthorizationCodeReceived = context =>
        {
            context.Fail("Authentication was aborted from user code.");
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var exception = await Assert.ThrowsAsync<AuthenticationFailureException>(delegate
        {
            return PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");
        });

        Assert.Equal("Authentication was aborted from user code.", exception.InnerException.Message);
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnAuthorizationCodeReceived_HandledWithoutTicket_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
        };
        events.OnAuthorizationCodeReceived = context =>
        {
            context.HandleResponse();
            context.Principal = null;
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnAuthorizationCodeReceived_HandledWithTicket_SkipToTicketReceived()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTicketReceived = true,
        };
        events.OnAuthorizationCodeReceived = context =>
        {
            context.Success();
            return Task.FromResult(0);
        };
        events.OnTicketReceived = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnTokenResponseReceived_Skip_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
        };
        events.OnTokenResponseReceived = context =>
        {
            context.SkipHandler();
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnTokenResponseReceived_Fail_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectRemoteFailure = true,
        };
        events.OnTokenResponseReceived = context =>
        {
            context.Fail("Authentication was aborted from user code.");
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var exception = await Assert.ThrowsAsync<AuthenticationFailureException>(delegate
        {
            return PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");
        });

        Assert.Equal("Authentication was aborted from user code.", exception.InnerException.Message);
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnTokenResponseReceived_HandledWithoutTicket_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
        };
        events.OnTokenResponseReceived = context =>
        {
            context.Principal = null;
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnTokenResponseReceived_HandledWithTicket_SkipToTicketReceived()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectTicketReceived = true,
        };
        events.OnTokenResponseReceived = context =>
        {
            context.Success();
            return Task.FromResult(0);
        };
        events.OnTicketReceived = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnTokenValidatedBackchannel_Skip_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
        };
        events.OnTokenValidated = context =>
        {
            context.SkipHandler();
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var response = await PostAsync(server, "signin-oidc", "state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnTokenValidatedBackchannel_Fail_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectRemoteFailure = true,
        };
        events.OnTokenValidated = context =>
        {
            context.Fail("Authentication was aborted from user code.");
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var exception = await Assert.ThrowsAsync<AuthenticationFailureException>(delegate
        {
            return PostAsync(server, "signin-oidc", "state=protected_state&code=my_code");
        });

        Assert.Equal("Authentication was aborted from user code.", exception.InnerException.Message);
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnTokenValidatedBackchannel_HandledWithoutTicket_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
        };
        events.OnTokenValidated = context =>
        {
            context.Principal = null;
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnTokenValidatedBackchannel_HandledWithTicket_SkipToTicketReceived()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectTicketReceived = true,
        };
        events.OnTokenValidated = context =>
        {
            context.Success();
            return Task.FromResult(0);
        };
        events.OnTicketReceived = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnUserInformationReceived_Skip_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectUserInfoReceived = true,
        };
        events.OnUserInformationReceived = context =>
        {
            context.SkipHandler();
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnUserInformationReceived_Fail_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectUserInfoReceived = true,
            ExpectRemoteFailure = true,
        };
        events.OnUserInformationReceived = context =>
        {
            context.Fail("Authentication was aborted from user code.");
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var exception = await Assert.ThrowsAsync<AuthenticationFailureException>(delegate
        {
            return PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");
        });

        Assert.Equal("Authentication was aborted from user code.", exception.InnerException.Message);
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnUserInformationReceived_HandledWithoutTicket_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectUserInfoReceived = true,
        };
        events.OnUserInformationReceived = context =>
        {
            context.Principal = null;
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnUserInformationReceived_HandledWithTicket_SkipToTicketReceived()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectUserInfoReceived = true,
            ExpectTicketReceived = true,
        };
        events.OnUserInformationReceived = context =>
        {
            context.Success();
            return Task.FromResult(0);
        };
        events.OnTicketReceived = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnAuthenticationFailed_Skip_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectUserInfoReceived = true,
            ExpectAuthenticationFailed = true,
        };
        events.OnUserInformationReceived = context =>
        {
            throw new NotImplementedException("TestException");
        };
        events.OnAuthenticationFailed = context =>
        {
            Assert.Equal("TestException", context.Exception.Message);
            context.SkipHandler();
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnAuthenticationFailed_Fail_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectUserInfoReceived = true,
            ExpectAuthenticationFailed = true,
            ExpectRemoteFailure = true,
        };
        events.OnUserInformationReceived = context =>
        {
            throw new NotImplementedException("TestException");
        };
        events.OnAuthenticationFailed = context =>
        {
            Assert.Equal("TestException", context.Exception.Message);
            context.Fail("Authentication was aborted from user code.");
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var exception = await Assert.ThrowsAsync<AuthenticationFailureException>(delegate
        {
            return PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");
        });

        Assert.Equal("Authentication was aborted from user code.", exception.InnerException.Message);
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnAuthenticationFailed_HandledWithoutTicket_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectUserInfoReceived = true,
            ExpectAuthenticationFailed = true,
        };
        events.OnUserInformationReceived = context =>
        {
            throw new NotImplementedException("TestException");
        };
        events.OnAuthenticationFailed = context =>
        {
            Assert.Equal("TestException", context.Exception.Message);
            Assert.Null(context.Principal);
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnAuthenticationFailed_HandledWithTicket_SkipToTicketReceived()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectUserInfoReceived = true,
            ExpectAuthenticationFailed = true,
            ExpectTicketReceived = true,
        };
        events.OnUserInformationReceived = context =>
        {
            throw new NotImplementedException("TestException");
        };
        events.OnAuthenticationFailed = context =>
        {
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
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnAccessDenied_Skip_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectAccessDenied = true
        };
        events.OnAccessDenied = context =>
        {
            context.SkipHandler();
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var response = await PostAsync(server, "signin-oidc", "error=access_denied&state=protected_state");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnAccessDenied_Handled_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectAccessDenied = true
        };
        events.OnAccessDenied = context =>
        {
            Assert.Equal("testvalue", context.Properties.Items["testkey"]);
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "error=access_denied&state=protected_state");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnRemoteFailure_Skip_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectUserInfoReceived = true,
            ExpectAuthenticationFailed = true,
            ExpectRemoteFailure = true,
        };
        events.OnUserInformationReceived = context =>
        {
            throw new NotImplementedException("TestException");
        };
        events.OnAuthenticationFailed = context =>
        {
            Assert.Equal("TestException", context.Exception.Message);
            return Task.FromResult(0);
        };
        events.OnRemoteFailure = context =>
        {
            Assert.Equal("TestException", context.Failure.Message);
            context.SkipHandler();
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnRemoteFailure_Handled_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectUserInfoReceived = true,
            ExpectAuthenticationFailed = true,
            ExpectRemoteFailure = true,
        };
        events.OnUserInformationReceived = context =>
        {
            throw new NotImplementedException("TestException");
        };
        events.OnRemoteFailure = context =>
        {
            Assert.Equal("TestException", context.Failure.Message);
            Assert.Equal("testvalue", context.Properties.Items["testkey"]);
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnTicketReceived_Skip_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectUserInfoReceived = true,
            ExpectTicketReceived = true,
        };
        events.OnTicketReceived = context =>
        {
            context.SkipHandler();
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppWritePath);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("/signin-oidc", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnTicketReceived_Handled_NoMoreEventsRun()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectMessageReceived = true,
            ExpectTokenValidated = true,
            ExpectAuthorizationCodeReceived = true,
            ExpectTokenResponseReceived = true,
            ExpectUserInfoReceived = true,
            ExpectTicketReceived = true,
        };
        events.OnTicketReceived = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.FromResult(0);
        };
        var server = CreateServer(events, AppNotImpl);

        var response = await PostAsync(server, "signin-oidc", "id_token=my_id_token&state=protected_state&code=my_code");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal("", await response.Content.ReadAsStringAsync());
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnRedirectToIdentityProviderForSignOut_Invoked()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectRedirectForSignOut = true,
        };
        var server = CreateServer(events,
        context =>
        {
            return context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        });

        var client = server.CreateClient();
        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("http://testhost/end", response.Headers.Location.GetLeftPart(UriPartial.Path));
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnRedirectToIdentityProviderForSignOut_Handled_RedirectNotInvoked()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectRedirectForSignOut = true,
        };
        events.OnRedirectToIdentityProviderForSignOut = context =>
        {
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            context.HandleResponse();
            return Task.CompletedTask;
        };
        var server = CreateServer(events,
        context =>
        {
            return context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        });

        var client = server.CreateClient();
        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Null(response.Headers.Location);
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnRemoteSignOut_Invoked()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectRemoteSignOut = true,
        };
        var server = CreateServer(events, AppNotImpl);

        var client = server.CreateClient();
        var response = await client.GetAsync("/signout-oidc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        events.ValidateExpectations();
        Assert.True(response.Headers.TryGetValues(HeaderNames.SetCookie, out var values));
        Assert.True(SetCookieHeaderValue.TryParseStrictList(values.ToList(), out var parsedValues));
        Assert.Equal(1, parsedValues.Count);
        Assert.True(StringSegment.IsNullOrEmpty(parsedValues.Single().Value));
    }

    [Fact]
    public async Task OnRemoteSignOut_Handled_NoSignout()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectRemoteSignOut = true,
        };
        events.OnRemoteSignOut = context =>
        {
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            context.HandleResponse();
            return Task.CompletedTask;
        };
        var server = CreateServer(events, AppNotImpl);

        var client = server.CreateClient();
        var response = await client.GetAsync("/signout-oidc");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        events.ValidateExpectations();
        Assert.False(response.Headers.TryGetValues(HeaderNames.SetCookie, out var values));
    }

    [Fact]
    public async Task OnRemoteSignOut_Skip_NoSignout()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectRemoteSignOut = true,
        };
        events.OnRemoteSignOut = context =>
        {
            context.SkipHandler();
            return Task.CompletedTask;
        };
        var server = CreateServer(events, context =>
        {
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.CompletedTask;
        });

        var client = server.CreateClient();
        var response = await client.GetAsync("/signout-oidc");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        events.ValidateExpectations();
        Assert.False(response.Headers.TryGetValues(HeaderNames.SetCookie, out var values));
    }

    [Fact]
    public async Task OnRedirectToSignedOutRedirectUri_Invoked()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectRedirectToSignedOut = true,
        };
        var server = CreateServer(events, AppNotImpl);

        var client = server.CreateClient();
        var response = await client.GetAsync("/signout-callback-oidc?state=protected_state");

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("http://testhost/redirect", response.Headers.Location.AbsoluteUri);
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnRedirectToSignedOutRedirectUri_Handled_NoRedirect()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectRedirectToSignedOut = true,
        };
        events.OnSignedOutCallbackRedirect = context =>
        {
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            context.HandleResponse();
            return Task.CompletedTask;
        };
        var server = CreateServer(events, AppNotImpl);

        var client = server.CreateClient();
        var response = await client.GetAsync("/signout-callback-oidc?state=protected_state");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Null(response.Headers.Location);
        events.ValidateExpectations();
    }

    [Fact]
    public async Task OnRedirectToSignedOutRedirectUri_Skipped_NoRedirect()
    {
        var events = new ExpectedOidcEvents()
        {
            ExpectRedirectToSignedOut = true,
        };
        events.OnSignedOutCallbackRedirect = context =>
        {
            context.SkipHandler();
            return Task.CompletedTask;
        };
        var server = CreateServer(events,
        context =>
        {
            context.Response.StatusCode = StatusCodes.Status202Accepted;
            return Task.CompletedTask;
        });

        var client = server.CreateClient();
        var response = await client.GetAsync("/signout-callback-oidc?state=protected_state");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Null(response.Headers.Location);
        events.ValidateExpectations();
    }

    private class ExpectedOidcEvents : OpenIdConnectEvents
    {
        public bool ExpectMessageReceived { get; set; }
        public bool InvokedMessageReceived { get; set; }

        public bool ExpectTokenValidated { get; set; }
        public bool InvokedTokenValidated { get; set; }

        public bool ExpectAccessDenied { get; set; }
        public bool InvokedAccessDenied { get; set; }

        public bool ExpectRemoteFailure { get; set; }
        public bool InvokedRemoteFailure { get; set; }

        public bool ExpectTicketReceived { get; set; }
        public bool InvokedTicketReceived { get; set; }

        public bool ExpectAuthorizationCodeReceived { get; set; }
        public bool InvokedAuthorizationCodeReceived { get; set; }

        public bool ExpectTokenResponseReceived { get; set; }
        public bool InvokedTokenResponseReceived { get; set; }

        public bool ExpectUserInfoReceived { get; set; }
        public bool InvokedUserInfoReceived { get; set; }

        public bool ExpectAuthenticationFailed { get; set; }
        public bool InvokeAuthenticationFailed { get; set; }

        public bool ExpectRedirectForSignOut { get; set; }
        public bool InvokedRedirectForSignOut { get; set; }

        public bool ExpectRemoteSignOut { get; set; }
        public bool InvokedRemoteSignOut { get; set; }

        public bool ExpectRedirectToSignedOut { get; set; }
        public bool InvokedRedirectToSignedOut { get; set; }

        public override Task MessageReceived(MessageReceivedContext context)
        {
            InvokedMessageReceived = true;
            return base.MessageReceived(context);
        }

        public override Task TokenValidated(TokenValidatedContext context)
        {
            InvokedTokenValidated = true;
            return base.TokenValidated(context);
        }

        public override Task AuthorizationCodeReceived(AuthorizationCodeReceivedContext context)
        {
            InvokedAuthorizationCodeReceived = true;
            return base.AuthorizationCodeReceived(context);
        }

        public override Task TokenResponseReceived(TokenResponseReceivedContext context)
        {
            InvokedTokenResponseReceived = true;
            return base.TokenResponseReceived(context);
        }

        public override Task UserInformationReceived(UserInformationReceivedContext context)
        {
            InvokedUserInfoReceived = true;
            return base.UserInformationReceived(context);
        }

        public override Task AuthenticationFailed(AuthenticationFailedContext context)
        {
            InvokeAuthenticationFailed = true;
            return base.AuthenticationFailed(context);
        }

        public override Task TicketReceived(TicketReceivedContext context)
        {
            InvokedTicketReceived = true;
            return base.TicketReceived(context);
        }

        public override Task AccessDenied(AccessDeniedContext context)
        {
            InvokedAccessDenied = true;
            return base.AccessDenied(context);
        }

        public override Task RemoteFailure(RemoteFailureContext context)
        {
            InvokedRemoteFailure = true;
            return base.RemoteFailure(context);
        }

        public override Task RedirectToIdentityProviderForSignOut(RedirectContext context)
        {
            InvokedRedirectForSignOut = true;
            return base.RedirectToIdentityProviderForSignOut(context);
        }

        public override Task RemoteSignOut(RemoteSignOutContext context)
        {
            InvokedRemoteSignOut = true;
            return base.RemoteSignOut(context);
        }

        public override Task SignedOutCallbackRedirect(RemoteSignOutContext context)
        {
            InvokedRedirectToSignedOut = true;
            return base.SignedOutCallbackRedirect(context);
        }

        public void ValidateExpectations()
        {
            Assert.Equal(ExpectMessageReceived, InvokedMessageReceived);
            Assert.Equal(ExpectTokenValidated, InvokedTokenValidated);
            Assert.Equal(ExpectAuthorizationCodeReceived, InvokedAuthorizationCodeReceived);
            Assert.Equal(ExpectTokenResponseReceived, InvokedTokenResponseReceived);
            Assert.Equal(ExpectUserInfoReceived, InvokedUserInfoReceived);
            Assert.Equal(ExpectAuthenticationFailed, InvokeAuthenticationFailed);
            Assert.Equal(ExpectTicketReceived, InvokedTicketReceived);
            Assert.Equal(ExpectAccessDenied, InvokedAccessDenied);
            Assert.Equal(ExpectRemoteFailure, InvokedRemoteFailure);
            Assert.Equal(ExpectRedirectForSignOut, InvokedRedirectForSignOut);
            Assert.Equal(ExpectRemoteSignOut, InvokedRemoteSignOut);
            Assert.Equal(ExpectRedirectToSignedOut, InvokedRedirectToSignedOut);
        }
    }

    private TestServer CreateServer(OpenIdConnectEvents events, RequestDelegate appCode)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
                builder.UseTestServer()
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
                        o.UseSecurityTokenValidator = false;
                        o.TokenHandler = new TestTokenHandler();
                        o.ProtocolValidator = new TestProtocolValidator();
                        o.BackchannelHttpHandler = new TestBackchannel();
                    });
                })
                .Configure(app =>
                {
                    app.UseAuthentication();
                    app.Run(appCode);
                }))
            .Build();

        host.Start();
        return host.GetTestServer();
    }

    private Task<HttpResponseMessage> PostAsync(TestServer server, string path, string form)
    {
        var client = server.CreateClient();
        var cookie = ".AspNetCore.Correlation.correlationId=N";
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
                    { ".xsrf", "correlationId" },
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

    private class TestTokenHandler : TokenHandler
    {
        public override Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters validationParameters)
        {
            Assert.Equal("my_id_token", token);
            var jwt = new JwtSecurityToken();
            return Task.FromResult(new TokenValidationResult()
            {
                SecurityToken = new JsonWebToken(jwt.EncodedHeader + "." + jwt.EncodedPayload + "."),
                ClaimsIdentity = new ClaimsIdentity("customAuthType"),
                IsValid = true
            });
        }

        public override SecurityToken ReadToken(string token)
        {
            Assert.Equal("my_id_token", token);
            return new JsonWebToken(token);
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
                return Task.FromResult(new HttpResponseMessage()
                {
                    Content =
                   new StringContent("{ \"id_token\": \"my_id_token\", \"access_token\": \"my_access_token\" }", Encoding.ASCII, "application/json")
                });
            }
            if (string.Equals("/user", request.RequestUri.AbsolutePath, StringComparison.Ordinal))
            {
                return Task.FromResult(new HttpResponseMessage() { Content = new StringContent("{ }", Encoding.ASCII, "application/json") });
            }

            throw new NotImplementedException(request.RequestUri.ToString());
        }
    }
}
