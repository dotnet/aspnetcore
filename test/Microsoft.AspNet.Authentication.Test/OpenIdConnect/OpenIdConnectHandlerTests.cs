// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.OpenIdConnect;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.WebEncoders;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Authentication.Tests.OpenIdConnect
{
    /// <summary>
    /// These tests are designed to test OpenIdConnectAuthenticationHandler.
    /// </summary>
    public class OpenIdConnectHandlerTests
    {
        private const string nonceForJwt = "abc";
        private static SecurityToken specCompliantJwt = new JwtSecurityToken("issuer", "audience", new List<Claim> { new Claim("iat", EpochTime.GetIntDate(DateTime.UtcNow).ToString()), new Claim("nonce", nonceForJwt) }, DateTime.UtcNow, DateTime.UtcNow + TimeSpan.FromDays(1));
        private const string ExpectedStateParameter = "expectedState";

        /// <summary>
        /// Sanity check that logging is filtering, hi / low water marks are checked
        /// </summary>
        [Fact]
        public void LoggingLevel()
        {
            var logger = new InMemoryLogger(LogLevel.Debug);
            logger.IsEnabled(LogLevel.Critical).ShouldBe<bool>(true);
            logger.IsEnabled(LogLevel.Debug).ShouldBe<bool>(true);
            logger.IsEnabled(LogLevel.Error).ShouldBe<bool>(true);
            logger.IsEnabled(LogLevel.Information).ShouldBe<bool>(true);
            logger.IsEnabled(LogLevel.Verbose).ShouldBe<bool>(true);
            logger.IsEnabled(LogLevel.Warning).ShouldBe<bool>(true);

            logger = new InMemoryLogger(LogLevel.Critical);
            logger.IsEnabled(LogLevel.Critical).ShouldBe<bool>(true);
            logger.IsEnabled(LogLevel.Debug).ShouldBe<bool>(false);
            logger.IsEnabled(LogLevel.Error).ShouldBe<bool>(false);
            logger.IsEnabled(LogLevel.Information).ShouldBe<bool>(false);
            logger.IsEnabled(LogLevel.Verbose).ShouldBe<bool>(false);
            logger.IsEnabled(LogLevel.Warning).ShouldBe<bool>(false);
        }

        [Theory, MemberData("AuthenticateCoreStateDataSet")]
        public async Task AuthenticateCoreState(Action<OpenIdConnectAuthenticationOptions> action, OpenIdConnectMessage message)
        {
            var handler = new OpenIdConnectAuthenticationHandlerForTestingAuthenticate();
            var server = CreateServer(new ConfigureOptions<OpenIdConnectAuthenticationOptions>(action), UrlEncoder.Default, handler);
            await server.CreateClient().PostAsync("http://localhost", new FormUrlEncodedContent(message.Parameters.Where(pair => pair.Value != null)));
        }

        public static TheoryData<Action<OpenIdConnectAuthenticationOptions>, OpenIdConnectMessage> AuthenticateCoreStateDataSet
        {
            get
            {
                var formater = new AuthenticationPropertiesFormaterKeyValue();
                var properties = new AuthenticationProperties();
                var dataset = new TheoryData<Action<OpenIdConnectAuthenticationOptions>, OpenIdConnectMessage>();

                // expected user state is added to the message.Parameters.Items[ExpectedStateParameter]
                // Userstate == null
                var message = new OpenIdConnectMessage();
                message.State = UrlEncoder.Default.UrlEncode(formater.Protect(properties));
                message.Code = Guid.NewGuid().ToString();
                message.Parameters.Add(ExpectedStateParameter, null);
                dataset.Add(SetStateOptions, message);

                // Userstate != null
                message = new OpenIdConnectMessage();
                properties.Items.Clear();
                var userstate = Guid.NewGuid().ToString();
                message.Code = Guid.NewGuid().ToString();
                properties.Items.Add(OpenIdConnectAuthenticationDefaults.UserstatePropertiesKey, userstate);
                message.State = UrlEncoder.Default.UrlEncode(formater.Protect(properties));
                message.Parameters.Add(ExpectedStateParameter, userstate);
                dataset.Add(SetStateOptions, message);
                return dataset;
            }
        }

        // Setup an event to check for expected state.
        // The state gets set by the runtime after the 'MessageReceivedContext'
        private static void SetStateOptions(OpenIdConnectAuthenticationOptions options)
        {
            options.AuthenticationScheme = "OpenIdConnectHandlerTest";
            options.ConfigurationManager = TestUtilities.DefaultOpenIdConnectConfigurationManager;
            options.ClientId = Guid.NewGuid().ToString();
            options.StateDataFormat = new AuthenticationPropertiesFormaterKeyValue();
            options.Events = new OpenIdConnectAuthenticationEvents()
            {
                OnAuthorizationCodeRedeemed = context =>
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
        }

#region Configure Options for AuthenticateCore variations

        private static void DefaultOptions(OpenIdConnectAuthenticationOptions options)
        {
            options.AuthenticationScheme = "OpenIdConnectHandlerTest";
            options.SignInScheme = "OpenIdConnectHandlerTest";
            options.ConfigurationManager = TestUtilities.DefaultOpenIdConnectConfigurationManager;
            options.ClientId = Guid.NewGuid().ToString();
            options.StateDataFormat = new AuthenticationPropertiesFormaterKeyValue();
        }

        private static void AuthorizationCodeReceivedHandledOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.SecurityTokenValidator = MockSecurityTokenValidator();
            options.ProtocolValidator = MockProtocolValidator();
            options.Events = new OpenIdConnectAuthenticationEvents()
            {
                OnAuthorizationCodeReceived = (context) =>
                {
                    context.HandleResponse();
                    return Task.FromResult<object>(null);
                }
            };
        }

        private static void AuthorizationCodeReceivedSkippedOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.SecurityTokenValidator = MockSecurityTokenValidator();
            options.ProtocolValidator = MockProtocolValidator();
            options.Events = new OpenIdConnectAuthenticationEvents()
            {
                OnAuthorizationCodeReceived = (context) =>
                {
                    context.SkipToNextMiddleware();
                    return Task.FromResult<object>(null);
                }
            };
        }

        private static void AuthenticationErrorHandledOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.SecurityTokenValidator = MockSecurityTokenValidator();
            options.ProtocolValidator = MockProtocolValidator();
            options.Events = new OpenIdConnectAuthenticationEvents()
            {
                OnAuthenticationFailed = (context) =>
                {
                    context.HandleResponse();
                    return Task.FromResult<object>(null);
                }
            };
        }

        private static void AuthenticationErrorSkippedOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.SecurityTokenValidator = MockSecurityTokenValidator();
            options.ProtocolValidator = MockProtocolValidator();
            options.Events = new OpenIdConnectAuthenticationEvents()
            {
                OnAuthenticationFailed = (context) =>
                {
                    context.SkipToNextMiddleware();
                    return Task.FromResult<object>(null);
                }
            };
        }

        private static void MessageReceivedHandledOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.Events = new OpenIdConnectAuthenticationEvents()
            {
                OnMessageReceived = (context) =>
                {
                    context.HandleResponse();
                    return Task.FromResult<object>(null);
                }
            };
        }

        private static void CodeReceivedAndRedeemedHandledOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.ResponseType = OpenIdConnectResponseTypes.Code;
            options.StateDataFormat = new AuthenticationPropertiesFormaterKeyValue();
            options.Events = new OpenIdConnectAuthenticationEvents()
            {
                OnAuthorizationCodeRedeemed = (context) =>
                {
                    context.HandleResponse();
                    return Task.FromResult<object>(null);
                }
            };
        }

        private static void CodeReceivedAndRedeemedSkippedOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.ResponseType = OpenIdConnectResponseTypes.Code;
            options.StateDataFormat = new AuthenticationPropertiesFormaterKeyValue();
            options.Events = new OpenIdConnectAuthenticationEvents()
            {
                OnAuthorizationCodeRedeemed = (context) =>
                {
                    context.SkipToNextMiddleware();
                    return Task.FromResult<object>(null);
                }
            };
        }

        private static void GetUserInfoFromUIEndpoint(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.ResponseType = OpenIdConnectResponseTypes.Code;
            options.ProtocolValidator.RequireNonce = false;
            options.StateDataFormat = new AuthenticationPropertiesFormaterKeyValue();
            options.GetClaimsFromUserInfoEndpoint = true;
            options.SecurityTokenValidator = MockSecurityTokenValidator();
            options.Events = new OpenIdConnectAuthenticationEvents()
            {
                OnSecurityTokenValidated = (context) =>
                {
                    var claimValue = context.AuthenticationTicket.Principal.FindFirst("test claim");
                    Assert.Equal(claimValue.Value, "test value");
                    context.HandleResponse();
                    return Task.FromResult<object>(null);
                }
            };
        }
        private static void MessageReceivedSkippedOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.Events = new OpenIdConnectAuthenticationEvents()
            {
                OnMessageReceived = (context) =>
                {
                    context.SkipToNextMiddleware();
                    return Task.FromResult<object>(null);
                }
            };
        }

        private static void MessageWithErrorOptions(OpenIdConnectAuthenticationOptions options)
        {
            AuthenticationErrorHandledOptions(options);
        }

        private static void SecurityTokenReceivedHandledOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.Events = new OpenIdConnectAuthenticationEvents()
            {
                OnSecurityTokenReceived = (context) =>
                {
                    context.HandleResponse();
                    return Task.FromResult<object>(null);
                }
            };
        }

        private static void SecurityTokenReceivedSkippedOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.Events = new OpenIdConnectAuthenticationEvents()
            {
                OnSecurityTokenReceived = (context) =>
                {
                    context.SkipToNextMiddleware();
                    return Task.FromResult<object>(null);
                }
            };
        }

        private static ISecurityTokenValidator MockSecurityTokenValidator()
        {
            var mockValidator = new Mock<ISecurityTokenValidator>();
            mockValidator.Setup(v => v.ValidateToken(It.IsAny<string>(), It.IsAny<TokenValidationParameters>(), out specCompliantJwt)).Returns(new ClaimsPrincipal());
            mockValidator.Setup(v => v.CanReadToken(It.IsAny<string>())).Returns(true);
            return mockValidator.Object;
        }

        private static OpenIdConnectProtocolValidator MockProtocolValidator()
        {
            var mockProtocolValidator = new Mock<OpenIdConnectProtocolValidator>();
            mockProtocolValidator.Setup(v => v.Validate(It.IsAny<JwtSecurityToken>(), It.IsAny<OpenIdConnectProtocolValidationContext>()));
            return mockProtocolValidator.Object;
        }

        private static void SecurityTokenValidatorCannotReadToken(OpenIdConnectAuthenticationOptions options)
        {
            AuthenticationErrorHandledOptions(options);
            var mockValidator = new Mock<ISecurityTokenValidator>();
            SecurityToken jwt = null;
            mockValidator.Setup(v => v.ValidateToken(It.IsAny<string>(), It.IsAny<TokenValidationParameters>(), out jwt)).Returns(new ClaimsPrincipal());
            mockValidator.Setup(v => v.CanReadToken(It.IsAny<string>())).Returns(false);
            options.SecurityTokenValidator = mockValidator.Object;
        }

        private static void SecurityTokenValidatorThrows(OpenIdConnectAuthenticationOptions options)
        {
            AuthenticationErrorHandledOptions(options);
            var mockValidator = new Mock<ISecurityTokenValidator>();
            SecurityToken jwt = null;
            mockValidator.Setup(v => v.ValidateToken(It.IsAny<string>(), It.IsAny<TokenValidationParameters>(), out jwt)).Throws<SecurityTokenSignatureKeyNotFoundException>();
            mockValidator.Setup(v => v.CanReadToken(It.IsAny<string>())).Returns(true);
            options.SecurityTokenValidator = mockValidator.Object;
        }

        private static void SecurityTokenValidatorValidatesAllTokens(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.SecurityTokenValidator = MockSecurityTokenValidator();
            options.ProtocolValidator.RequireTimeStampInNonce = false;
            options.ProtocolValidator.RequireNonce = false;
        }

        private static void SecurityTokenValidatedHandledOptions(OpenIdConnectAuthenticationOptions options)
        {
            SecurityTokenValidatorValidatesAllTokens(options);
            options.Events = new OpenIdConnectAuthenticationEvents()
            {
                OnSecurityTokenValidated = (context) =>
                {
                    context.HandleResponse();
                    return Task.FromResult<object>(null);
                }
            };
        }

        private static void SecurityTokenValidatedSkippedOptions(OpenIdConnectAuthenticationOptions options)
        {
            SecurityTokenValidatorValidatesAllTokens(options);
            options.Events = new OpenIdConnectAuthenticationEvents()
            {
                OnSecurityTokenValidated = (context) =>
                {
                    context.SkipToNextMiddleware();
                    return Task.FromResult<object>(null);
                }
            };
        }

        private static void StateNullOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
        }

        private static void StateEmptyOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
        }

        private static void StateInvalidOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
        }

#endregion

        private static Task EmptyTask() { return Task.FromResult(0); }

        private static TestServer CreateServer(ConfigureOptions<OpenIdConnectAuthenticationOptions> options, IUrlEncoder encoder, OpenIdConnectAuthenticationHandler handler = null)
        {
            return TestServer.Create(
                app =>
                {
                    app.UseMiddleware<OpenIdConnectAuthenticationMiddlewareForTestingAuthenticate>(options, encoder, handler);
                    app.Use(async (context, next) =>
                    {
                        await next();
                    });
                },
                services =>
                {
                    services.AddWebEncoders();
                    services.AddDataProtection();
                }
            );
        }

        private static TestServer CreateServer(ConfigureOptions<OpenIdConnectAuthenticationOptions> configureOptions, IUrlEncoder encoder, ILoggerFactory loggerFactory, OpenIdConnectAuthenticationHandler handler = null)
        {
            return TestServer.Create(
                app =>
                {
                    app.UseMiddleware<OpenIdConnectAuthenticationMiddlewareForTestingAuthenticate>(configureOptions, encoder, loggerFactory, handler);
                    app.Use(async (context, next) =>
                    {
                        await next();
                    });
                },
                services =>
                {
                    services.AddWebEncoders();
                    services.AddDataProtection();
                }
            );
        }
    }
}
