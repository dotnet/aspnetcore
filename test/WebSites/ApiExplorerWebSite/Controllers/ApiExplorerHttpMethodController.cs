using Microsoft.AspNet.Mvc;
using System;

namespace ApiExplorer
{
    [Route("ApiExplorerHttpMethod")]
    public class ApiExplorerHttpMethodController : Controller
    {
        [Route("All")]
        public void All()
        {
        }

        [HttpGet("Get")]
        public void Get()
        {
        }

        [AcceptVerbs("PUT", "POST", Route = "Single")]
        public void PutOrPost()
        {
        }

        [HttpGet("MultipleActions")]
        [HttpPut("MultipleActions")]
        public void MultipleActions()
        {
        }
    }
}