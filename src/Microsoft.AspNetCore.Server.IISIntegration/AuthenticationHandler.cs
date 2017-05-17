// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    internal class AuthenticationHandler : IAuthenticationHandler
    {
        private const string MSAspNetCoreWinAuthToken = "MS-ASPNETCORE-WINAUTHTOKEN";
        private WindowsPrincipal _user;
        private HttpContext _context;

        internal AuthenticationScheme Scheme { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync() 
        {
            var user = GetUser();
            if (user != null)
            {
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(user, Scheme.Name)));
            }
            else
            {
                return Task.FromResult(AuthenticateResult.None());
            }
        }

        private WindowsPrincipal GetUser()
        {
            if (_user == null)
            {
                var tokenHeader = _context.Request.Headers[MSAspNetCoreWinAuthToken];

                int hexHandle;
                if (!StringValues.IsNullOrEmpty(tokenHeader)
                    && int.TryParse(tokenHeader, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hexHandle))
                {
                    // Always create the identity if the handle exists, we need to dispose it so it does not leak.
                    var handle = new IntPtr(hexHandle);
                    var winIdentity = new WindowsIdentity(handle);

                    // WindowsIdentity just duplicated the handle so we need to close the original.
                    NativeMethods.CloseHandle(handle);

                    _context.Response.RegisterForDispose(winIdentity);
                    _user = new WindowsPrincipal(winIdentity);
                }
            }

            return _user;
        }


        public Task ChallengeAsync(ChallengeContext context)
        {
            switch (context.Behavior)
            {
                case ChallengeBehavior.Automatic:
                    // If there is a principal already, invoke the forbidden code path
                    if (GetUser() == null)
                    {
                        goto case ChallengeBehavior.Unauthorized;
                    }
                    else
                    {
                        goto case ChallengeBehavior.Forbidden;
                    }
                case ChallengeBehavior.Unauthorized:
                    context.HttpContext.Response.StatusCode = 401;
                    // We would normally set the www-authenticate header here, but IIS does that for us.
                    break;
                case ChallengeBehavior.Forbidden:
                    context.HttpContext.Response.StatusCode = 403;
                    break;
            }
            return TaskCache.CompletedTask;
        }

        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            Scheme = scheme;
            _context = context;
            return TaskCache.CompletedTask;
        }

        public Task SignInAsync(SignInContext context)
        {
            throw new NotSupportedException();
        }

        public Task SignOutAsync(SignOutContext context)
        {
            return TaskCache.CompletedTask;
        }
    }
}
