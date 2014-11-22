// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.AspNet.Mvc;
using Microsoft.Framework.Runtime;

namespace FilesWebSite
{
    public class DownloadFilesController : Controller
    {
        private readonly IApplicationEnvironment _appEnvironment;

        public DownloadFilesController(IApplicationEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
        }

        public IActionResult DowloadFromDisk()
        {
            var path = Path.Combine(_appEnvironment.ApplicationBasePath, "sample.txt");
            return File(path, "text/plain");
        }

        public IActionResult DowloadFromDiskWithFileName()
        {
            var path = Path.Combine(_appEnvironment.ApplicationBasePath, "sample.txt");
            return File(path, "text/plain", "downloadName.txt");
        }

        public IActionResult DowloadFromStream()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("This is sample text from a stream");
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream, "text/plain");
        }

        public IActionResult DowloadFromStreamWithFileName()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("This is sample text from a stream");
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            return File(stream, "text/plain", "downloadName.txt");
        }

        public IActionResult DowloadFromBinaryData()
        {
            var data = Encoding.UTF8.GetBytes("This is a sample text from a binary array");
            return File(data, "text/plain");
        }

        public IActionResult DowloadFromBinaryDataWithFileName()
        {
            var data = Encoding.UTF8.GetBytes("This is a sample text from a binary array");
            return File(data, "text/plain", "downloadName.txt");
        }
    }
}