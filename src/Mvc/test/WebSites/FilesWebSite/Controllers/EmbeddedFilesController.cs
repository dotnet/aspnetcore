// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace FilesWebSite;

public class EmbeddedFilesController : Controller
{
    public IActionResult DownloadFileWithFileName()
    {
        return new VirtualFileResult("/Greetings.txt", "text/plain")
        {
            FileProvider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly, "FilesWebSite.EmbeddedResources"),
            FileDownloadName = "downloadName.txt",
            EnableRangeProcessing = true,
        };
    }

    public IActionResult DownloadFileWithFileName_RangeProcessingNotEnabled()
    {
        return new VirtualFileResult("/Greetings.txt", "text/plain")
        {
            FileProvider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly, "FilesWebSite.EmbeddedResources"),
            FileDownloadName = "downloadName.txt",
        };
    }

    public IActionResult DownloadFileWithFileName_WithEtag()
    {
        var file = new VirtualFileResult("/Greetings.txt", "text/plain")
        {
            FileProvider = new EmbeddedFileProvider(GetType().GetTypeInfo().Assembly, "FilesWebSite.EmbeddedResources"),
            FileDownloadName = "downloadName.txt",
            EnableRangeProcessing = true,
        };

        file.EntityTag = new Microsoft.Net.Http.Headers.EntityTagHeaderValue("\"Etag\"");
        return file;
    }
}
