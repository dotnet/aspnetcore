// Copyright (c) .NET Foundation. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Authentication
{
    public class AuthenticationMiddlewareTests
    {
        [Fact]
        public async Task OnlyInvokesCanHandleRequestHandlers()
        {
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UseAuthentication();
                })
                .ConfigureServices(services => services.AddAuthentication(o =>
                {
                    o.AddScheme("Skip", s =>
                    {
                        s.HandlerType = typeof(SkipHandler);
                    });
                    // Won't get hit since CanHandleRequests is false
                    o.AddScheme("throws", s =>
                    {
                        s.HandlerType = typeof(ThrowsHandler);
                    });
                    o.AddScheme("607", s =>
                    {
                        s.HandlerType = typeof(SixOhSevenHandler);
                    });
                    // Won't get run since 607 will finish
                    o.AddScheme("305", s =>
                    {
                        s.HandlerType = typeof(ThreeOhFiveHandler);
                    });
                }));
            var server = new TestServer(builder);
            var response = await server.CreateClient().GetAsync("http://example.com/");
            Assert.Equal(607, (int)response.StatusCode);
        }

        private class ThreeOhFiveHandler : StatusCodeHandler {
            public ThreeOhFiveHandler() : base(305) { }
        }

        private class SixOhSevenHandler : StatusCodeHandler
        {
            public SixOhSevenHandler() : base(607) { }
        }

        private class SevenOhSevenHandler : StatusCodeHandler
        {
            public SevenOhSevenHandler() : base(707) { }
        }

        private class StatusCodeHandler : IAuthenticationRequestHandler
        {
            private HttpContext _context;
            private int _code;

            public StatusCodeHandler(int code)
            {
                _code = code;
            }
            
            public Task<AuthenticateResult> AuthenticateAsync()
            {
                throw new NotImplementedException();
            }

            public Task ChallengeAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task ForbidAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task<bool> HandleRequestAsync()
            {
                _context.Response.StatusCode = _code;
                return Task.FromResult(true);
            }

            public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
            {
                _context = context;
                return Task.FromResult(0);
            }

            public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task SignOutAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }
        }

        private class ThrowsHandler : IAuthenticationHandler
        {
            private HttpContext _context;

            public Task<AuthenticateResult> AuthenticateAsync()
            {
                throw new NotImplementedException();
            }

            public Task ChallengeAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task ForbidAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task<bool> HandleRequestAsync()
            {
                throw new NotImplementedException();
            }

            public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
            {
                _context = context;
                return Task.FromResult(0);
            }

            public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task SignOutAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }
        }

        private class SkipHandler : IAuthenticationRequestHandler
        {
            private HttpContext _context;

            public Task<AuthenticateResult> AuthenticateAsync()
            {
                throw new NotImplementedException();
            }

            public Task ChallengeAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task ForbidAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task<bool> HandleRequestAsync()
            {
                return Task.FromResult(false);
            }

            public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
            {
                _context = context;
                return Task.FromResult(0);
            }

            public Task SignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }

            public Task SignOutAsync(AuthenticationProperties properties)
            {
                throw new NotImplementedException();
            }
        }

    }
}
