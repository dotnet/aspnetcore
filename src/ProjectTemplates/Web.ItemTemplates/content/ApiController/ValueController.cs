using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    #if NameIsController
    public class ValueController : Microsoft.AspNetCore.Mvc.ControllerBase
    #else
    public class ValueController : ControllerBase
    #endif
    {
        #if(actions)
        // GET: api/<ValueController>
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/<ValueController>/5
        [HttpGet("{id}")]
        public string Get(int id)
        {
            return "value";
        }

        // POST api/<ValueController>
        [HttpPost]
        public void Post([FromBody] string value)
        {
        }

        // PUT api/<ValueController>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE api/<ValueController>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
        #endif
    }
}
