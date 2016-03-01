using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace ReactExample.Controllers
{
    public class PeopleApiController : Controller
    {
        [HttpPut("api/people/{personId:int}")]
        public ActionResult UpdatePerson([FromBody] PersonDto person)
        {
            if (!ModelState.IsValid) {
                return HttpBadRequest(ModelState);
            } else {
                return new HttpOkResult();
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
