// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace TestServer.Controllers;

[EnableCors("AllowAll")]
[Route("api/[controller]")]
public class DataController : Controller
{
    // GET api/data
    [HttpGet]
    public FileContentResult Get()
    {
        var bytes = new byte[byte.MaxValue + 1];
        for (int i = 0; i <= byte.MaxValue; i++)
        {
            bytes[i] = (byte)i;
        }

        return File(bytes, "application/octet-stream");
    }

    // POST api/data
    [HttpPost]
    public async Task<IActionResult> PostAsync()
    {
        var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms);
        var bytes = ms.ToArray();
        Array.Reverse(bytes);

        for (int i = 0; i <= byte.MaxValue; i++)
        {
            if (bytes[i] != (byte)i)
            {
                return BadRequest();
            }
        }

        return File(bytes, "application/octet-stream");
    }
}
