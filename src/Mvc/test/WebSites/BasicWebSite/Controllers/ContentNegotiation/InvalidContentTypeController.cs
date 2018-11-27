using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers.ContentNegotiation
{
    public class InvalidContentTypeController : Controller
    {
        [HttpGet("InvalidContentType/SetResponseContentTypeJson")]
        public IActionResult SetResponseContentTypeJson()
        {
            HttpContext.Response.ContentType = "json";
            return Ok(0);
        }
    }
}
