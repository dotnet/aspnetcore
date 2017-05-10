// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.Service.IntegratedWebClient
{
    public class IntegratedWebClientRedirectFilter : IActionFilter
    {
        private IOptions<IntegratedWebClientOptions> _webClientOptions;
        private IEnumerable<string> _parameters;

        public IntegratedWebClientRedirectFilter(
            IOptions<IntegratedWebClientOptions> webClientOptions,
            IEnumerable<string> parameters)
        {
            _webClientOptions = webClientOptions;
            _parameters = parameters;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            foreach (var parameter in _parameters)
            {
                if (context.ActionArguments.TryGetValue(parameter, out var parameterValue))
                {
                    switch (parameterValue)
                    {
                        case AuthorizationRequest authorization when IsAuthorizationForWebApplication(authorization):
                            authorization.RequestGrants.RedirectUri = ComputeRedirectUri(isLogout: false);
                            break;
                        case LogoutRequest logout when IsLogoutForWebApplication(logout):
                            logout.LogoutRedirectUri = ComputeRedirectUri(isLogout: true);
                            break;
                        default:
                            break;
                    }
                }
            }

            bool IsLogoutForWebApplication(LogoutRequest logout) =>
                logout.IsValid && logout.LogoutRedirectUri != null &&
                logout.LogoutRedirectUri.Equals(_webClientOptions.Value.TokenRedirectUrn);

            bool IsAuthorizationForWebApplication(AuthorizationRequest request) =>
                _webClientOptions.Value.ClientId.Equals(request.Message?.ClientId) &&
                request.RequestGrants.RedirectUri != null &&
                request.RequestGrants.RedirectUri.Equals(_webClientOptions.Value.TokenRedirectUrn);

            string ComputeRedirectUri(bool isLogout)
            {
                var request = context.HttpContext.Request;
                var openIdConnectOptions = context.HttpContext.RequestServices.GetRequiredService<IOptionsSnapshot<OpenIdConnectOptions>>();
                var scheme = request.Scheme;
                var host = request.Host.ToUriComponent();
                var pathBase = request.PathBase;
                var options = openIdConnectOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);
                var path = !isLogout ? options.CallbackPath : options.SignedOutCallbackPath;
                return $"{scheme}://{host}{pathBase}{path}";
            }
        }
    }
}
