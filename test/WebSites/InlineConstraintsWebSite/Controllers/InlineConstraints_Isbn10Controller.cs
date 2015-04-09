using Microsoft.AspNet.Mvc;
using System;

namespace InlineConstraintsWebSite.Controllers
{
    public class InlineConstraints_Isbn10Controller : Controller
    {
        public string Index(string isbnNumber)
        {
            return "10 Digit ISBN Number " + isbnNumber;
        }
    }
}