using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BasicWebSite.Controllers
{
    public class TestingController : Controller
    {
        public TestingController(TestService service)
        {
            Service = service;
        }

        public TestService Service { get; }

        [HttpGet("Testing/Builder")]
        public string Get() => Service.Message;

        [HttpPost("Testing/RedirectHandler/{value}")]
        public IActionResult RedirectHandler(
            [FromRoute]int value,
            [FromBody] Number number,
            [FromHeader(Name = "X-Pass-Thru")] string passThruValue)
        {
            Response.Headers.Add("X-Pass-Thru", passThruValue);
            if (value < number.Value)
            {
                return RedirectToActionPreserveMethod(
                    nameof(RedirectHandler),
                    "Testing",
                    new { value = value + 1 });
            }

            return Ok(new RedirectHandlerResponse { Url = value, Body = number.Value });
        }

        [HttpGet("Testing/RedirectHandler/Headers")]
        public IActionResult RedirectHandlerHeaders()
        {
            if (!Request.Headers.TryGetValue("X-Added-Header", out var value))
            {
                return Content("No header present");
            }
            else
            {
                return RedirectToAction(nameof(RedirectHandlerHeadersRedirect));
            }
        }

        [HttpGet("Testing/RedirectHandler/Headers/Redirect")]
        public IActionResult RedirectHandlerHeadersRedirect()
        {
            if (Request.Headers.TryGetValue("X-Added-Header", out var value))
            {
                return Content("true");
            }
            else
            {
                return Content("false");
            }
        }

        [HttpGet("Testing/AntiforgerySimulator/{value}")]
        public IActionResult AntiforgerySimulator([FromRoute]int value)
        {
            Response.Cookies.Append(
                "AntiforgerySimulator",
                $"Cookie-{value.ToString(CultureInfo.InvariantCulture)}");

            return Ok();
        }


        [HttpPost("Testing/PostRedirectGet/Post/{value}")]
        public IActionResult PostRedirectGetPost([FromRoute]int value)
        {
            var compareValue = $"Cookie-{value.ToString(CultureInfo.InvariantCulture)}";
            if (!Request.Cookies.ContainsKey("AntiforgerySimulator"))
            {
                return BadRequest("Missing AntiforgerySimulator cookie");
            }

            if (!string.Equals(compareValue, Request.Cookies["AntiforgerySimulator"]))
            {
                return BadRequest("Values don't match");
            }

            TempData["Value"] = value + 1;
            Response.Cookies.Append("Message", $"Value-{(value + 1).ToString(CultureInfo.InvariantCulture)}");

            return RedirectToAction(nameof(PostRedirectGetGet));
        }

        [HttpGet("Testing/PostRedirectGet/Get/{value}")]
        public IActionResult PostRedirectGetGet([FromRoute]int value)
        {
            return Ok(new PostRedirectGetGetResponse
            {
                TempDataValue = (int)TempData["Value"],
                CookieValue = Request.Cookies["Message"]
            });
        }

        [HttpPut("Testing/Put/{value}")]
        public IActionResult PutNoBody([FromRoute]int value)
        {
            if (value < 5)
            {
                return RedirectToActionPermanentPreserveMethod(nameof(PutNoBody), "Testing", new { value = value + 1 });
            }
            else
            {
                return Ok(value);
            }
        }
    }

    public class PostRedirectGetGetResponse
    {
        public int TempDataValue { get; set; }
        public string CookieValue { get; set; }
    }

    public class RedirectHandlerResponse
    {
        public int Url { get; set; }
        public int Body { get; set; }
    }

    public class Number
    {
        public int Value { get; set; }
    }
}
