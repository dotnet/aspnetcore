using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Components.TestServer.Controllers
{
    public class DownloadController : Controller
    {

        [HttpGet("~/download")]
        public FileStreamResult Download()
        {
            var buffer = Encoding.UTF8.GetBytes("The quick brown fox jumped over the lazy dog.");
            var stream = new MemoryStream(buffer);

            var result = new FileStreamResult(stream, "text/plain");
            result.FileDownloadName = "test.txt";
            return result;
        }
    }
}
