using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.Mvc;

namespace angular.Controllers
{
    public class ConfigurationController : Controller
    {
        public ConfigurationController(IClientRequestParametersProvider clientRequestParametersProvider)
        {
            ClientRequestParametersProvider = clientRequestParametersProvider;
        }

        public IClientRequestParametersProvider ClientRequestParametersProvider { get; }

        [HttpGet("_configuration/{clientId}")]
        public IActionResult GetClientRequestParameters([FromRoute]string clientId)
        {
            var parameters = ClientRequestParametersProvider.GetClientParameters(HttpContext, clientId);
            parameters["post_logout_redirect_uri"] = parameters["post_logout_redirect_uri"].Replace("login-callback", "logout-callback");
            return Ok(parameters);
        }
    }
}
