
[assembly: Microsoft.AspNetCore.Mvc.ApiConventionType(typeof(Microsoft.AspNetCore.Mvc.DefaultApiConventions))]

namespace TestApp._OUTPUT_
{
    using Microsoft.AspNetCore.Mvc;

    [ApiController]
    [Route("[controller]/[action]")]
    public class BaseController : ControllerBase
    {

    }
}

namespace TestApp._OUTPUT_
{
    public class CodeFixAddsFullyQualifiedProducesResponseType : BaseController
    {
        [Microsoft.AspNetCore.Mvc.ProducesResponseType(202)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseType(400)]
        [Microsoft.AspNetCore.Mvc.ProducesResponseType(404)]
        [Microsoft.AspNetCore.Mvc.ProducesDefaultResponseType]
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
