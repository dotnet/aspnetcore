// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace FileManagerSample;

[ApiController]
[Route("controller")]
public class FileController : ControllerBase
{
    // curl -X POST -F "file=@D:\.other\big-files\bigfile.dat" http://localhost:5000/controller/upload
    [HttpPost]
    [Route("upload")]
    public async Task<IActionResult> Upload()
    {
        // 1. endpoint handler
        // 2. form feature initialization
        // 3. calling `Request.Form.Files.First()`
        // 4. calling `FormFeature.InnerReadFormAsync()`

        if (!Request.HasFormContentType)
        {
            return BadRequest("The request does not contain a valid form.");
        }

        // calling ReadFormAsync allows to await for form read (not blocking file read, opposite to `Request.Form.Files.`)
        var formFeature = Request.HttpContext.Features.GetRequiredFeature<IFormFeature>();
        await formFeature.ReadFormAsync(HttpContext.RequestAborted);

        var file = Request.Form.Files.First();
        return Ok($"File '{file.Name}' uploaded.");
    }

    // curl -X POST -F "file=@D:\.other\big-files\bigfile.dat" http://localhost:5000/controller/upload-cts
    [HttpPost]
    [Route("upload-cts")]
    public async Task<IActionResult> UploadCts(CancellationToken cancellationToken)
    {
        // 1. form feature initialization
        // 2.calling `FormFeature.InnerReadFormAsync()`
        // 3. endpoint handler
        // 4. calling `Request.Form.Files.First()`

        if (!Request.HasFormContentType)
        {
            return BadRequest("The request does not contain a valid form.");
        }

        var formFeature = Request.HttpContext.Features.GetRequiredFeature<IFormFeature>();
        await formFeature.ReadFormAsync(cancellationToken);

        var file = Request.Form.Files.First();
        return Ok($"File '{file.Name}' uploaded.");
    }

    // curl -X POST -F "file=@D:\.other\big-files\bigfile.dat" http://localhost:5000/controller/upload-cts-noop
    [HttpPost]
    [Route("upload-cts-noop")]
    public IActionResult UploadCtsNoop(CancellationToken cancellationToken)
    {
        // 1. form feature initialization
        // 2.calling `FormFeature.InnerReadFormAsync()`
        // 3. endpoint handler

        return Ok($"Noop completed.");
    }

    // curl -X POST -F "file=@D:\.other\big-files\bigfile.dat" http://localhost:5000/controller/upload-str-noop
    [HttpPost]
    [Route("upload-str-noop")]
    public IActionResult UploadStrNoop(string str)
    {
        // 1. form feature initialization
        // 2.calling `FormFeature.InnerReadFormAsync()`
        // 3. endpoint handler

        return Ok($"Str completed.");
    }

    // curl -X POST -F "file=@D:\.other\big-files\bigfile.dat" http://localhost:5000/controller/upload-str-query
    [HttpPost]
    [Route("upload-str-query")]
    public IActionResult UploadStrQuery([FromQuery] string str)
    {
        // 1. form feature initialization
        // 2.calling `FormFeature.InnerReadFormAsync()`
        // 3. endpoint handler

        return Ok($"Query completed.");
    }

    // curl -X POST -F "file=@D:\.other\big-files\bigfile_50mb.dat" http://localhost:5000/controller/upload-route-param/1
    [HttpPost]
    [Route("upload-route-param/{id}")]
    public IActionResult UploadQueryParam(string id)
    {
        // 1. form feature initialization
        // 2.calling `FormFeature.InnerReadFormAsync()`
        // 3. endpoint handler

        return Ok($"Query completed: query id = {id}");
    }

    // curl -X POST -F "file=@D:\.other\big-files\bigfile_50mb.dat" http://localhost:5000/controller/upload-file
    [HttpPost]
    [Route("upload-file")]
    public IActionResult UploadForm([FromForm] IFormFile file)
    {
        // 1. form feature initialization
        // 2.calling `FormFeature.InnerReadFormAsync()`
        // 3. endpoint handler

        return Ok($"File '{file.FileName}' uploaded.");
    }
}
