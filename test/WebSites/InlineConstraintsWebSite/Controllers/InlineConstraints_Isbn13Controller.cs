using Microsoft.AspNet.Mvc;
using System;

namespace InlineConstraintsWebSite.Controllers
{
    [Route("book/[action]")]
    public class InlineConstraints_Isbn13Controller : Controller
    {
        [HttpGet("{isbnNumber:IsbnDigitScheme13}")]
        public string Index(string isbnNumber)
        {
            return "13 Digit ISBN Number " + isbnNumber;
        }
    }
}