using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ReactExample.Controllers
{
    public class PeopleApiController : Controller
    {
        [HttpPut("api/people/{personId:int}")]
        public ActionResult UpdatePerson([FromBody] PersonDto person)
        {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            } else {
                return new OkResult();
            }
        }
    }

    public class PersonDto {
        public string name { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string company { get; set; }

        [Range(1, 10)]
        public int favoriteNumber { get; set; }
    }
}
