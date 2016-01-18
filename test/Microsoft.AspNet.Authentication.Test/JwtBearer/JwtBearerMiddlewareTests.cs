// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.AspNet.TestHost;
using Microsoft.AspNet.Testing.xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Microsoft.AspNet.Authentication.JwtBearer
{
    public class JwtBearerMiddlewareTests
    {
        [ConditionalFact(Skip = "Need to remove dependency on AAD since the generated tokens will expire")]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        // https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/issues/179
        public async Task BearerTokenValidation()
        {
            var options = new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                Authority = "https://login.windows.net/tushartest.onmicrosoft.com",
                Audience = "https://TusharTest.onmicrosoft.com/TodoListService-ManualJwt"
            };
            options.TokenValidationParameters.ValidateLifetime = false;
            var server = CreateServer(options);

            var newBearerToken = "Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6ImtyaU1QZG1Cdng2OHNrVDgtbVBBQjNCc2VlQSJ9.eyJhdWQiOiJodHRwczovL1R1c2hhclRlc3Qub25taWNyb3NvZnQuY29tL1RvZG9MaXN0U2VydmljZS1NYW51YWxKd3QiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hZmJlY2UwMy1hZWFhLTRmM2YtODVlNy1jZTA4ZGQyMGNlNTAvIiwiaWF0IjoxNDE4MzMwNjE0LCJuYmYiOjE0MTgzMzA2MTQsImV4cCI6MTQxODMzNDUxNCwidmVyIjoiMS4wIiwidGlkIjoiYWZiZWNlMDMtYWVhYS00ZjNmLTg1ZTctY2UwOGRkMjBjZTUwIiwiYW1yIjpbInB3ZCJdLCJvaWQiOiI1Mzk3OTdjMi00MDE5LTQ2NTktOWRiNS03MmM0Yzc3NzhhMzMiLCJ1cG4iOiJWaWN0b3JAVHVzaGFyVGVzdC5vbm1pY3Jvc29mdC5jb20iLCJ1bmlxdWVfbmFtZSI6IlZpY3RvckBUdXNoYXJUZXN0Lm9ubWljcm9zb2Z0LmNvbSIsInN1YiI6IkQyMm9aMW9VTzEzTUFiQXZrdnFyd2REVE80WXZJdjlzMV9GNWlVOVUwYnciLCJmYW1pbHlfbmFtZSI6Ikd1cHRhIiwiZ2l2ZW5fbmFtZSI6IlZpY3RvciIsImFwcGlkIjoiNjEzYjVhZjgtZjJjMy00MWI2LWExZGMtNDE2Yzk3ODAzMGI3IiwiYXBwaWRhY3IiOiIwIiwic2NwIjoidXNlcl9pbXBlcnNvbmF0aW9uIiwiYWNyIjoiMSJ9.N_Kw1EhoVGrHbE6hOcm7ERdZ7paBQiNdObvp2c6T6n5CE8p0fZqmUd-ya_EqwElcD6SiKSiP7gj0gpNUnOJcBl_H2X8GseaeeMxBrZdsnDL8qecc6_ygHruwlPltnLTdka67s1Ow4fDSHaqhVTEk6lzGmNEcbNAyb0CxQxU6o7Fh0yHRiWoLsT8yqYk8nKzsHXfZBNby4aRo3_hXaa4i0SZLYfDGGYPdttG4vT_u54QGGd4Wzbonv2gjDlllOVGOwoJS6kfl1h8mk0qxdiIaT_ChbDWgkWvTB7bTvBE-EgHgV0XmAo0WtJeSxgjsG3KhhEPsONmqrSjhIUV4IVnF2w";
            var response = await SendAsync(server, "http://example.com/oauth", newBearerToken);
            Assert.Equal(HttpStatusCode.OK, response.Response.StatusCode);
        }

        [Fact]
        public async Task SignInThrows()
        {
            var server = CreateServer(new JwtBearerOptions
            {
                AutomaticAuthenticate = true
            });
            var transaction = await server.SendAsync("https://example.com/signIn");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }

        [Fact]
        public async Task SignOutThrows()
        {
            var server = CreateServer(new JwtBearerOptions
            {
                AutomaticAuthenticate = true
            });
            var transaction = await server.SendAsync("https://example.com/signOut");
            Assert.Equal(HttpStatusCode.OK, transaction.Response.StatusCode);
        }


        [Fact]
        public async Task CustomHeaderReceived()
        {
            var server = CreateServer(new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                Events = new JwtBearerEvents()
                {
                    OnReceivingToken = context =>
                    {
                        var claims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "Bob le Magnifique"),
                            new Claim(ClaimTypes.Email, "bob@contoso.com"),
                            new Claim(ClaimsIdentity.DefaultNameClaimType, "bob")
                        };

                        context.Ticket = new AuthenticationTicket(
                            new ClaimsPrincipal(new ClaimsIdentity(claims, context.Options.AuthenticationScheme)),
                            new AuthenticationProperties(), context.Options.AuthenticationScheme);

                        context.HandleResponse();

                        return Task.FromResult<object>(null);
                    }
                }
            });

            var response = await SendAsync(server, "http://example.com/oauth", "someHeader someblob");
            Assert.Equal(HttpStatusCode.OK, response.Response.StatusCode);
            Assert.Equal("Bob le Magnifique", response.ResponseText);
        }

        [Fact]
        public async Task NoHeaderReceived()
        {
            var server = CreateServer(new JwtBearerOptions());
            var response = await SendAsync(server, "http://example.com/oauth");
            Assert.Equal(HttpStatusCode.Unauthorized, response.Response.StatusCode);
        }

        [Fact]
        public async Task HeaderWithoutBearerReceived()
        {
            var server = CreateServer(new JwtBearerOptions());
            var response = await SendAsync(server, "http://example.com/oauth","Token");
            Assert.Equal(HttpStatusCode.Unauthorized, response.Response.StatusCode);
        }

        [Fact]
        public async Task UnrecognizedTokenReceived()
        {
            var server = CreateServer(new JwtBearerOptions
            {
                AutomaticAuthenticate = true
            });

            var response = await SendAsync(server, "http://example.com/oauth", "Bearer someblob");
            Assert.Equal(HttpStatusCode.Unauthorized, response.Response.StatusCode);
            Assert.Equal("", response.ResponseText);
        }

        [Fact]
        public async Task InvalidTokenReceived()
        {
            var options = new JwtBearerOptions
            {
                AutomaticAuthenticate = true
            };
            options.SecurityTokenValidators.Clear();
            options.SecurityTokenValidators.Add(new InvalidTokenValidator());
            var server = CreateServer(options);

            var response = await SendAsync(server, "http://example.com/oauth", "Bearer someblob");
            Assert.Equal(HttpStatusCode.Unauthorized, response.Response.StatusCode);
            Assert.Equal("", response.ResponseText);
        }

        [Fact]
        public async Task CustomTokenReceived()
        {
            var server = CreateServer(new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                Events = new JwtBearerEvents()
                {
                    OnReceivedToken = context =>
                    {
                        var claims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "Bob le Magnifique"),
                            new Claim(ClaimTypes.Email, "bob@contoso.com"),
                            new Claim(ClaimsIdentity.DefaultNameClaimType, "bob")
                        };

                        context.Ticket = new AuthenticationTicket(
                            new ClaimsPrincipal(new ClaimsIdentity(claims, context.Options.AuthenticationScheme)),
                            new AuthenticationProperties(), context.Options.AuthenticationScheme);

                        context.HandleResponse();

                        return Task.FromResult<object>(null);
                    }
                }
            });

            var response = await SendAsync(server, "http://example.com/oauth", "Bearer someblob");
            Assert.Equal(HttpStatusCode.OK, response.Response.StatusCode);
            Assert.Equal("Bob le Magnifique", response.ResponseText);
        }

        [Fact]
        public async Task CustomTokenValidated()
        {
            var options = new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                Events = new JwtBearerEvents()
                {
                    OnValidatedToken = context =>
                    {
                        // Retrieve the NameIdentifier claim from the identity
                        // returned by the custom security token validator.
                        var identity = (ClaimsIdentity)context.Ticket.Principal.Identity;
                        var identifier = identity.FindFirst(ClaimTypes.NameIdentifier);

                        Assert.Equal("Bob le Tout Puissant", identifier.Value);

                        // Remove the existing NameIdentifier claim and replace it
                        // with a new one containing a different value.
                        identity.RemoveClaim(identifier);
                        // Make sure to use a different name identifier
                        // than the one defined by BlobTokenValidator.
                        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "Bob le Magnifique"));

                        return Task.FromResult<object>(null);
                    }
                }
            };
            options.SecurityTokenValidators.Add(new BlobTokenValidator(options.AuthenticationScheme));
            var server = CreateServer(options);

            var response = await SendAsync(server, "http://example.com/oauth", "Bearer someblob");
            Assert.Equal(HttpStatusCode.OK, response.Response.StatusCode);
            Assert.Equal("Bob le Magnifique", response.ResponseText);
        }

        [Fact]
        public async Task RetrievingTokenFromAlternateLocation()
        {
            var server = CreateServer(new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                Events = new JwtBearerEvents()
                {
                    OnReceivingToken = context =>
                    {
                        context.Token = "CustomToken";
                        return Task.FromResult<object>(null);
                    },
                    OnReceivedToken = context =>
                    {
                        var claims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "Bob le Magnifique"),
                            new Claim(ClaimTypes.Email, "bob@contoso.com"),
                            new Claim(ClaimsIdentity.DefaultNameClaimType, "bob")
                        };

                        context.Ticket = new AuthenticationTicket(
                            new ClaimsPrincipal(new ClaimsIdentity(claims, context.Options.AuthenticationScheme)),
                            new AuthenticationProperties(), context.Options.AuthenticationScheme);

                        context.HandleResponse();

                        return Task.FromResult<object>(null);
                    }
                }
            });

            var response = await SendAsync(server, "http://example.com/oauth", "Bearer Token");
            Assert.Equal(HttpStatusCode.OK, response.Response.StatusCode);
            Assert.Equal("Bob le Magnifique", response.ResponseText);
        }

        [Fact]
        public async Task BearerTurns401To403IfAuthenticated()
        {
            var server = CreateServer(new JwtBearerOptions
            {
                Events = new JwtBearerEvents()
                {
                    OnReceivedToken = context =>
                    {
                        var claims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "Bob le Magnifique"),
                            new Claim(ClaimTypes.Email, "bob@contoso.com"),
                            new Claim(ClaimsIdentity.DefaultNameClaimType, "bob")
                        };

                        context.Ticket = new AuthenticationTicket(
                            new ClaimsPrincipal(new ClaimsIdentity(claims, context.Options.AuthenticationScheme)),
                            new AuthenticationProperties(), context.Options.AuthenticationScheme);

                        context.HandleResponse();

                        return Task.FromResult<object>(null);
                    }
                }
            });

            var response = await SendAsync(server, "http://example.com/unauthorized", "Bearer Token");
            Assert.Equal(HttpStatusCode.Forbidden, response.Response.StatusCode);
        }
        
        [Fact]
        public async Task BearerDoesNothingTo401IfNotAuthenticated()
        {
            var server = CreateServer(new JwtBearerOptions
            {
                Events = new JwtBearerEvents()
                {
                    OnReceivedToken = context =>
                    {
                        var claims = new[]
                        {
                            new Claim(ClaimTypes.NameIdentifier, "Bob le Magnifique"),
                            new Claim(ClaimTypes.Email, "bob@contoso.com"),
                            new Claim(ClaimsIdentity.DefaultNameClaimType, "bob")
                        };

                        context.Ticket = new AuthenticationTicket(
                            new ClaimsPrincipal(new ClaimsIdentity(claims, context.Options.AuthenticationScheme)),
                            new AuthenticationProperties(), context.Options.AuthenticationScheme);

                        context.HandleResponse();

                        return Task.FromResult<object>(null);
                    }
                }
            });

            var response = await SendAsync(server, "http://example.com/unauthorized");
            Assert.Equal(HttpStatusCode.Unauthorized, response.Response.StatusCode);
        }

        [Fact]
        public async Task EventOnReceivingTokenSkipped_NoMoreEventsExecuted()
        {
            var server = CreateServer(new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                Events = new JwtBearerEvents()
                {
                    OnReceivingToken = context =>
                    {
                        context.SkipToNextMiddleware();
                        return Task.FromResult(0);
                    },
                    OnReceivedToken = context =>
                    {
                        throw new NotImplementedException();
                    },
                    OnValidatedToken = context =>
                    {
                        throw new NotImplementedException();
                    },
                    OnAuthenticationFailed = context =>
                    {
                        throw new NotImplementedException(context.Exception.ToString());
                    },
                    OnChallenge = context =>
                    {
                        throw new NotImplementedException();
                    },
                }
            });

            var response = await SendAsync(server, "http://example.com/checkforerrors", "Bearer Token");
            Assert.Equal(HttpStatusCode.OK, response.Response.StatusCode);
            Assert.Equal(string.Empty, response.ResponseText);
        }

        [Fact]
        public async Task EventOnReceivedTokenSkipped_NoMoreEventsExecuted()
        {
            var server = CreateServer(new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                Events = new JwtBearerEvents()
                {
                    OnReceivedToken = context =>
                    {
                        context.SkipToNextMiddleware();
                        return Task.FromResult(0);
                    },
                    OnValidatedToken = context =>
                    {
                        throw new NotImplementedException();
                    },
                    OnAuthenticationFailed = context =>
                    {
                        throw new NotImplementedException(context.Exception.ToString());
                    },
                    OnChallenge = context =>
                    {
                        throw new NotImplementedException();
                    },
                }
            });

            var response = await SendAsync(server, "http://example.com/checkforerrors", "Bearer Token");
            Assert.Equal(HttpStatusCode.OK, response.Response.StatusCode);
            Assert.Equal(string.Empty, response.ResponseText);
        }

        [Fact]
        public async Task EventOnValidatedTokenSkipped_NoMoreEventsExecuted()
        {
            var options = new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                Events = new JwtBearerEvents()
                {
                    OnValidatedToken = context =>
                    {
                        context.SkipToNextMiddleware();
                        return Task.FromResult(0);
                    },
                    OnAuthenticationFailed = context =>
                    {
                        throw new NotImplementedException(context.Exception.ToString());
                    },
                    OnChallenge = context =>
                    {
                        throw new NotImplementedException();
                    },
                }
            };
            options.SecurityTokenValidators.Clear();
            options.SecurityTokenValidators.Add(new BlobTokenValidator("JWT"));
            var server = CreateServer(options);

            var response = await SendAsync(server, "http://example.com/checkforerrors", "Bearer Token");
            Assert.Equal(HttpStatusCode.OK, response.Response.StatusCode);
            Assert.Equal(string.Empty, response.ResponseText);
        }

        [Fact]
        public async Task EventOnAuthenticationFailedSkipped_NoMoreEventsExecuted()
        {
            var options = new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                Events = new JwtBearerEvents()
                {
                    OnValidatedToken = context =>
                    {
                        throw new Exception("Test Exception");
                    },
                    OnAuthenticationFailed = context =>
                    {
                        context.SkipToNextMiddleware();
                        return Task.FromResult(0);
                    },
                    OnChallenge = context =>
                    {
                        throw new NotImplementedException();
                    },
                }
            };
            options.SecurityTokenValidators.Clear();
            options.SecurityTokenValidators.Add(new BlobTokenValidator("JWT"));
            var server = CreateServer(options);

            var response = await SendAsync(server, "http://example.com/checkforerrors", "Bearer Token");
            Assert.Equal(HttpStatusCode.OK, response.Response.StatusCode);
            Assert.Equal(string.Empty, response.ResponseText);
        }

        [Fact]
        public async Task EventOnChallengeSkipped_ResponseNotModified()
        {
            var server = CreateServer(new JwtBearerOptions
            {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                Events = new JwtBearerEvents()
                {
                    OnChallenge = context =>
                    {
                        context.SkipToNextMiddleware();
                        return Task.FromResult(0);
                    },
                }
            });

            var response = await SendAsync(server, "http://example.com/unauthorized", "Bearer Token");
            Assert.Equal(HttpStatusCode.OK, response.Response.StatusCode);
            Assert.Empty(response.Response.Headers.WwwAuthenticate);
            Assert.Equal(string.Empty, response.ResponseText);
        }

        class InvalidTokenValidator : ISecurityTokenValidator
        {
            public InvalidTokenValidator()
            {
            }

            public bool CanValidateToken => true;

            public int MaximumTokenSizeInBytes
            {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }

            public bool CanReadToken(string securityToken) => true;

            public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
            {
                throw new SecurityTokenException("InvalidToken");
            }
        }

        class BlobTokenValidator : ISecurityTokenValidator
        {
            public BlobTokenValidator(string authenticationScheme)
            {
                AuthenticationScheme = authenticationScheme;
            }

            public string AuthenticationScheme { get; }

            public bool CanValidateToken => true;
            
            public int MaximumTokenSizeInBytes
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public bool CanReadToken(string securityToken) => true;

            public ClaimsPrincipal ValidateToken(string securityToken, TokenValidationParameters validationParameters, out SecurityToken validatedToken)
            {
                validatedToken = null;

                var claims = new[]
                {
                    // Make sure to use a different name identifier
                    // than the one defined by CustomTokenValidated.
                    new Claim(ClaimTypes.NameIdentifier, "Bob le Tout Puissant"),
                    new Claim(ClaimTypes.Email, "bob@contoso.com"),
                    new Claim(ClaimsIdentity.DefaultNameClaimType, "bob"),
                };

                return new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationScheme));
            }
        }

        private static TestServer CreateServer(JwtBearerOptions options, Func<HttpContext, bool> handler = null)
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    if (options != null)
                    {
                        app.UseJwtBearerAuthentication(options);
                    }

                    app.Use(async (context, next) =>
                    {
                        if (context.Request.Path == new PathString("/checkforerrors"))
                        {
                            var authContext = new AuthenticateContext(Http.Authentication.AuthenticationManager.AutomaticScheme);
                            await context.Authentication.AuthenticateAsync(authContext);
                            if (authContext.Error != null)
                            {
                                throw new Exception("Failed to authenticate", authContext.Error);
                            }
                            return;
                        }
                        else if (context.Request.Path == new PathString("/oauth"))
                        {
                            if (context.User == null ||
                                context.User.Identity == null ||
                                !context.User.Identity.IsAuthenticated)
                            {
                                context.Response.StatusCode = 401;

                                return;
                            }

                            var identifier = context.User.FindFirst(ClaimTypes.NameIdentifier);
                            if (identifier == null)
                            {
                                context.Response.StatusCode = 500;

                                return;
                            }

                            await context.Response.WriteAsync(identifier.Value);
                        }
                        else if (context.Request.Path == new PathString("/unauthorized"))
                        {
                            // Simulate Authorization failure 
                            var result = await context.Authentication.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
                            await context.Authentication.ChallengeAsync(JwtBearerDefaults.AuthenticationScheme);
                        }
                        else if (context.Request.Path == new PathString("/signIn"))
                        {
                            await Assert.ThrowsAsync<NotSupportedException>(() => context.Authentication.SignInAsync(JwtBearerDefaults.AuthenticationScheme, new ClaimsPrincipal()));
                        }
                        else if (context.Request.Path == new PathString("/signOut"))
                        {
                            await Assert.ThrowsAsync<NotSupportedException>(() => context.Authentication.SignOutAsync(JwtBearerDefaults.AuthenticationScheme));
                        }
                        else
                        {
                            await next();
                        }
                    });
                })
                .ConfigureServices(services => services.AddAuthentication());
            return new TestServer(builder);
        }

        // TODO: see if we can share the TestExtensions SendAsync method (only diff is auth header)
        private static async Task<Transaction> SendAsync(TestServer server, string uri, string authorizationHeader = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                request.Headers.Add("Authorization", authorizationHeader);
            }

            var transaction = new Transaction
            {
                Request = request,
                Response = await server.CreateClient().SendAsync(request),
            };

            transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();

            if (transaction.Response.Content != null &&
                transaction.Response.Content.Headers.ContentType != null &&
                transaction.Response.Content.Headers.ContentType.MediaType == "text/xml")
            {
                transaction.ResponseElement = XElement.Parse(transaction.ResponseText);
            }

            return transaction;
        }
    }
}
