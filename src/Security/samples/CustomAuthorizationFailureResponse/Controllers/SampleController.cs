using CustomAuthorizationFailureResponse.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomAuthorizationFailureResponse.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SampleController : ControllerBase
    {
        [HttpGet("customPolicyWithCustomForbiddenMessage")]
        [Authorize(Policy = SamplePolicyNames.CustomPolicyWithCustomForbiddenMessage)]
        public string GetWithCustomPolicyWithCustomForbiddenMessage()
        {
            return "Hello world from GetWithCustomPolicyWithCustomForbiddenMessage";
        }

        [HttpGet("customPolicy")]
        [Authorize(Policy = SamplePolicyNames.CustomPolicy)]
        public string GetWithCustomPolicy()
        {
            return "Hello world from GetWithCustomPolicy";
        }
    }
}
