// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace FilesWebSite;

public class DownloadFilesController : Controller
{
    private readonly IWebHostEnvironment _hostingEnvironment;
    private readonly string _testFilesPath;

    public DownloadFilesController(IWebHostEnvironment hostingEnvironment)
    {
        _hostingEnvironment = hostingEnvironment;
        _testFilesPath = Path.Combine(Path.GetTempPath(), "test-files");
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

    public IActionResult DownloadFromDiskSymlink()
    {
        var path = Path.Combine(_hostingEnvironment.ContentRootPath, "sample.txt");
        var symlink = Path.Combine(_testFilesPath, Path.GetRandomFileName());

        if (!Directory.Exists(_testFilesPath))
        {
            Directory.CreateDirectory(_testFilesPath);
        }

        var fileInfo = System.IO.File.CreateSymbolicLink(symlink, path);

        return PhysicalFile(fileInfo.FullName, "text/plain");
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

    protected override void Dispose(bool disposing)
    {
        try
        {
            Directory.Delete(Path.Combine(_testFilesPath), recursive: true);
        }
        catch { }

        base.Dispose(disposing);
    }
}
