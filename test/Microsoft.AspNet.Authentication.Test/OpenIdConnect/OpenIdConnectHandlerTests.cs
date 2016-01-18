// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.OpenIdConnect;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.AspNet.Authentication.Tests.OpenIdConnect
{
    /// <summary>
    /// These tests are designed to test OpenIdConnectAuthenticationHandler.
    /// </summary>
    public class OpenIdConnectHandlerTests
    {
        private const string nonceForOpenIdConnect = "abc";
        private static SecurityToken specCompliantOpenIdConnect = new JwtSecurityToken("issuer", "audience", new List<Claim> { new Claim("iat", EpochTime.GetIntDate(DateTime.UtcNow).ToString()), new Claim("nonce", nonceForOpenIdConnect) }, DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromDays(1));
        private const string ExpectedStateParameter = "expectedState";

        [Theory, MemberData(nameof(AuthenticateCoreStateDataSet))]
        public async Task AuthenticateCoreState(OpenIdConnectOptions option, OpenIdConnectMessage message)
        {
            var handler = new OpenIdConnectHandlerForTestingAuthenticate();
            var server = CreateServer(option, UrlEncoder.Default, handler);
            await server.CreateClient().PostAsync("http://localhost", new FormUrlEncodedContent(message.Parameters.Where(pair => pair.Value != null)));
        }

        public static TheoryData<OpenIdConnectOptions, OpenIdConnectMessage> AuthenticateCoreStateDataSet
        {
            get
            {
                var formater = new AuthenticationPropertiesFormaterKeyValue();
                var properties = new AuthenticationProperties();
                var dataset = new TheoryData<OpenIdConnectOptions, OpenIdConnectMessage>();

                // expected user state is added to the message.Parameters.Items[ExpectedStateParameter]
                // Userstate == null
                var message = new OpenIdConnectMessage();
                message.State = UrlEncoder.Default.Encode(formater.Protect(properties));
                message.Code = Guid.NewGuid().ToString();
                message.Parameters.Add(ExpectedStateParameter, null);
                dataset.Add(GetStateOptions(), message);

                // Userstate != null
                message = new OpenIdConnectMessage();
                properties.Items.Clear();
                var userstate = Guid.NewGuid().ToString();
                message.Code = Guid.NewGuid().ToString();
                properties.Items.Add(OpenIdConnectDefaults.UserstatePropertiesKey, userstate);
                message.State = UrlEncoder.Default.Encode(formater.Protect(properties));
                message.Parameters.Add(ExpectedStateParameter, userstate);
                dataset.Add(GetStateOptions(), message);
                return dataset;
            }
        }

        // Setup an event to check for expected state.
        // The state gets set by the runtime after the 'MessageReceivedContext'
        private static OpenIdConnectOptions GetStateOptions()
        {
            var options = new OpenIdConnectOptions();
            options.AuthenticationScheme = "OpenIdConnectHandlerTest";
            options.ConfigurationManager = TestUtilities.DefaultOpenIdConnectConfigurationManager;
            options.ClientId = Guid.NewGuid().ToString();
            options.StateDataFormat = new AuthenticationPropertiesFormaterKeyValue();
            options.SignInScheme = "Cookies";
            options.Events = new OpenIdConnectEvents()
            {
                OnTokenResponseReceived = context =>
                {
                    context.HandleResponse();
                    if (context.ProtocolMessage.State == null && !context.ProtocolMessage.Parameters.ContainsKey(ExpectedStateParameter))
                        return Task.FromResult<object>(null);

                    if (context.ProtocolMessage.State == null || !context.ProtocolMessage.Parameters.ContainsKey(ExpectedStateParameter))
                        Assert.True(false, "(context.ProtocolMessage.State=!= null || !context.ProtocolMessage.Parameters.ContainsKey(expectedState)");

                    Assert.Equal(context.ProtocolMessage.State, context.ProtocolMessage.Parameters[ExpectedStateParameter]);
                    return Task.FromResult<object>(null);
                }
            };
            return options;
        }

        private static TestServer CreateServer(OpenIdConnectOptions options, UrlEncoder encoder, OpenIdConnectHandler handler = null)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseMiddleware<OpenIdConnectMiddlewareForTestingAuthenticate>(Options.Create(options), encoder, handler);
                    app.Use(async (context, next) =>
                    {
                        await next();
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddWebEncoders();
                    services.AddDataProtection();
                });
            return new TestServer(builder);
        }
    }
}
