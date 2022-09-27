[assembly: Microsoft.AspNetCore.Mvc.ApiConventionType(typeof(Microsoft.AspNetCore.Mvc.DefaultApiConventions))]

namespace TestApp._INPUT_
{
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("[controller]/[action]")]
    public class BaseController : ControllerBase
    {
    }
}

namespace TestApp._INPUT_
{
    public class CodeFixAddsFullyQualifiedProducesResponseType : BaseController
    {
        public object GetItem(int id)
        {
            if (id == 0)
            {
                return NotFound();
            }

            if (id == 1)
            {
                return BadRequest();
            }

            return Accepted(new object());
        }
    }
}
