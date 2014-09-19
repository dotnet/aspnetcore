using System;
using Microsoft.AspNet.Mvc;

namespace UrlHelperWebSite.Controllers
{
    [Route("api/[controller]/{id?}", Name = "SimplePocoApi")]
    public class SimplePocoController
    {
        private readonly IUrlHelper _urlHelper;

        public SimplePocoController(IUrlHelper urlHelper)
        {
            _urlHelper = urlHelper;
        }

        [HttpGet]
        public string GetById(int id)
        {
            return "value:" + id;
        }
    }
}