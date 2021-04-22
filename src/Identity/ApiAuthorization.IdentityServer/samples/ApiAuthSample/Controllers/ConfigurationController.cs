using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.AspNetCore.Mvc;

namespace ApiAuthSample.Controllers
{
    public class ConfigurationController : ControllerBase
    {
        private readonly IClientRequestParametersProvider _clientRequestParametersProvider;

        public ConfigurationController(IClientRequestParametersProvider clientRequestParametersProvider)
        {
            _clientRequestParametersProvider = clientRequestParametersProvider;
        }

        [HttpGet("/_configuration/{clientId}")]
        public IActionResult GetClientParameters(string clientId)
        {
            var parameters = _clientRequestParametersProvider.GetClientParameters(HttpContext, clientId);
            if (parameters == null)
            {
                return BadRequest($"Parameters for client '{clientId}' not found.");
            }
            else
            {
                return Ok(parameters);
            }
        }
    }
}
