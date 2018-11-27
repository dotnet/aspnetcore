// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace FilesWebSite
{
    public class DownloadFilesController : Controller
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public DownloadFilesController(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        public IActionResult DownloadFromDisk()
        {
            var path = Path.Combine(_hostingEnvironment.ContentRootPath, "sample.txt");
            return PhysicalFile(path, "text/plain", true);
        }

        public IActionResult DownloadFromDisk_WithLastModifiedAndEtag()
        {
            var path = Path.Combine(_hostingEnvironment.ContentRootPath, "sample.txt");
            var lastModified = new DateTimeOffset(year: 1999, month: 11, day: 04, hour: 3, minute: 0, second: 0, offset: new TimeSpan(0));
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            return PhysicalFile(path, "text/plain", lastModified, entityTag, true);
        }

        public IActionResult DownloadFromDiskWithFileName()
        {
            var path = Path.Combine(_hostingEnvironment.ContentRootPath, "sample.txt");
            return PhysicalFile(path, "text/plain", "downloadName.txt");
        }

        public IActionResult DownloadFromDiskWithFileName_WithLastModifiedAndEtag()
        {
            var path = Path.Combine(_hostingEnvironment.ContentRootPath, "sample.txt");
            var lastModified = new DateTimeOffset(year: 1999, month: 11, day: 04, hour: 3, minute: 0, second: 0, offset: new TimeSpan(0));
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            return PhysicalFile(path, "text/plain", "downloadName.txt", lastModified, entityTag, true);
        }

        public IActionResult DownloadFromStream()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("This is sample text from a stream");
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream, "text/plain", true);
        }

        public IActionResult DownloadFromStreamWithFileName()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("This is sample text from a stream");
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream, "text/plain", "downloadName.txt");
        }

        public IActionResult DownloadFromStreamWithFileName_WithEtag()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("This is sample text from a stream");
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            return File(stream, "text/plain", "downloadName.txt", lastModified: null, entityTag: entityTag, enableRangeProcessing: true);
        }

        public IActionResult DownloadFromBinaryData()
        {
            var data = Encoding.UTF8.GetBytes("This is a sample text from a binary array");
            return File(data, "text/plain", true);
        }

        public IActionResult DownloadFromBinaryDataWithFileName()
        {
            var data = Encoding.UTF8.GetBytes("This is a sample text from a binary array");
            return File(data, "text/plain", "downloadName.txt");
        }

        public IActionResult DownloadFromBinaryDataWithFileName_WithEtag()
        {
            var data = Encoding.UTF8.GetBytes("This is a sample text from a binary array");
            var entityTag = new EntityTagHeaderValue("\"Etag\"");
            return File(data, "text/plain", "downloadName.txt", lastModified: null, entityTag: entityTag, enableRangeProcessing: true);
        }
    }
}
