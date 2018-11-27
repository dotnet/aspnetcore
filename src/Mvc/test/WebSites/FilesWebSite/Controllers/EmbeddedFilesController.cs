// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;

namespace FilesWebSite
{
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
}