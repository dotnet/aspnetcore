// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Security.Infrastructure
{
    // TODO: comment function documentations
    using AuthenticateCallback = Action<IIdentity, IDictionary<string, string>, IDictionary<string, object>, object>;
    using AuthenticateDelegate = Func<string[], Action<IIdentity, IDictionary<string, string>, IDictionary<string, object>, object>, object, Task>;

    internal static class OwinRequestExtensions
    {
        public static object RegisterAuthenticationHandler(this HttpRequest request, AuthenticationHandler handler)
        {
            var chained = request.Get<AuthenticateDelegate>(Constants.SecurityAuthenticate);
            var hook = new Hook(handler, chained);
            request.Set<AuthenticateDelegate>(Constants.SecurityAuthenticate, hook.AuthenticateAsync);
            return hook;
        }

        public static void UnregisterAuthenticationHandler(this HttpRequest request, object registration)
        {
            var hook = registration as Hook;
            if (hook == null)
            {
                throw new InvalidOperationException(Resources.Exception_UnhookAuthenticationStateType);
            }
            request.Set(Constants.SecurityAuthenticate, hook.Chained);
        }

        private class Hook
        {
            private readonly AuthenticationHandler _handler;

            public Hook(AuthenticationHandler handler, AuthenticateDelegate chained)
            {
                _handler = handler;
                Chained = chained;
            }

            public AuthenticateDelegate Chained { get; private set; }

            public async Task AuthenticateAsync(
                string[] authenticationTypes,
                AuthenticateCallback callback,
                object state)
            {
                if (authenticationTypes == null)
                {
                    callback(null, null, _handler.BaseOptions.Description.Properties, state);
                }
                else if (authenticationTypes.Contains(_handler.BaseOptions.AuthenticationType, StringComparer.Ordinal))
                {
                    AuthenticationTicket ticket = await _handler.AuthenticateAsync();
                    if (ticket != null && ticket.Identity != null)
                    {
                        callback(ticket.Identity, ticket.Properties.Dictionary, _handler.BaseOptions.Description.Properties, state);
                    }
                }
                if (Chained != null)
                {
                    await Chained(authenticationTypes, callback, state);
                }
            }
        }
    }
}
