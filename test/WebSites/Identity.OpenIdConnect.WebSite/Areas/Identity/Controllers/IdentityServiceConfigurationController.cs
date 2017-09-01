// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Service;
using Microsoft.AspNetCore.Mvc;

namespace Identity.OpenIdConnect.WebSite.Identity.Controllers
{
    [Area("Identity")]
    public class IdentityServiceConfigurationController : Controller
    {
        private readonly IConfigurationManager _configurationProvider;
        private readonly IKeySetMetadataProvider _keySetProvider;

        public IdentityServiceConfigurationController(
            IConfigurationManager configurationProvider,
            IKeySetMetadataProvider keySetProvider)
        {
            _configurationProvider = configurationProvider;
            _keySetProvider = keySetProvider;
        }

        [HttpGet("tfp/Identity/signinsignup/v2.0/.well-known/openid-configuration")]
        [Produces("application/json")]
        public async Task<IActionResult> Metadata()
        {
            var configurationContext = new ConfigurationContext
            {
                Id = "IdentityService:signinsignup",
                HttpContext = HttpContext,
                AuthorizationEndpoint = EndpointLink("Authorize", "IdentityService"),
                TokenEndpoint = EndpointLink("Token", "IdentityService"),
                JwksUriEndpoint = EndpointLink("Keys", "IdentityServiceConfiguration"),
                EndSessionEndpoint = EndpointLink("Logout", "IdentityService"),
            };

            return Ok(await _configurationProvider.GetConfigurationAsync(configurationContext));
        }

        [HttpGet("tfp/Identity/signinsignup/discovery/v2.0/keys")]
        [Produces("application/json")]
        public async Task<IActionResult> Keys()
        {
            return Ok(await _keySetProvider.GetKeysAsync());
        }

        private string EndpointLink(string action, string controller) =>
            Url.Action(action, controller, null, Request.Scheme, Request.Host.Value);
    }
}
