// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Components.TestServer.Controllers;

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
