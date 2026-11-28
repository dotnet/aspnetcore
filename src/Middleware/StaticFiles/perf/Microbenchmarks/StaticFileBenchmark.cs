// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.StaticFiles.Microbenchmarks;

[MemoryDiagnoser]
public class StaticFileBenchmark
{
    private const string FilePath = "/css/site.css";

    private FileExtensionContentTypeProvider _defaultProvider = default!;
    private FileExtensionContentTypeProvider _nonDictionaryProvider = default!;
    private StaticFileMiddleware _middleware = default!;
    private string _etag = default!;

    [GlobalSetup]
    public void Setup()
    {
        _defaultProvider = new FileExtensionContentTypeProvider();
        // Mappings that are not a Dictionary<string, string> take the string-based lookup path.
        _nonDictionaryProvider = new FileExtensionContentTypeProvider(
            new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [".css"] = "text/css" });

        var fileProvider = new InMemoryFileProvider(FilePath, new byte[1024], new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
        _middleware = new StaticFileMiddleware(
            static _ => Task.CompletedTask,
            new BenchmarkWebHostEnvironment(fileProvider),
            Options.Create(new StaticFileOptions { FileProvider = fileProvider }),
            NullLoggerFactory.Instance);

        var context = CreateContext(HttpMethods.Head);
        _middleware.Invoke(context).GetAwaiter().GetResult();
        _etag = context.Response.Headers.ETag.ToString();
        if (context.Response.StatusCode != StatusCodes.Status200OK || string.IsNullOrEmpty(_etag))
        {
            throw new InvalidOperationException("The static file was not served.");
        }
    }

    [Benchmark]
    public bool TryGetContentType_Default() => _defaultProvider.TryGetContentType(FilePath, out _);

    [Benchmark]
    public bool TryGetContentType_NonDictionaryMappings() => _nonDictionaryProvider.TryGetContentType(FilePath, out _);

    [Benchmark]
    public Task Head() => _middleware.Invoke(CreateContext(HttpMethods.Head));

    [Benchmark]
    public Task Get() => _middleware.Invoke(CreateContext(HttpMethods.Get));

    [Benchmark]
    public Task GetIfNoneMatch()
    {
        var context = CreateContext(HttpMethods.Get);
        context.Request.Headers.IfNoneMatch = _etag;
        return _middleware.Invoke(context);
    }

    private static DefaultHttpContext CreateContext(string method)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = FilePath;
        context.Response.Body = Stream.Null;
        return context;
    }

    private sealed class BenchmarkWebHostEnvironment(IFileProvider fileProvider) : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = fileProvider;
        public string ApplicationName { get; set; } = "Benchmark";
        public IFileProvider ContentRootFileProvider { get; set; } = fileProvider;
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Production";
    }

    private sealed class InMemoryFileProvider(string path, byte[] content, DateTimeOffset lastModified) : IFileProvider
    {
        private readonly InMemoryFileInfo _file = new(Path.GetFileName(path), content, lastModified);

        public IFileInfo GetFileInfo(string subpath) =>
            string.Equals(subpath, path, StringComparison.Ordinal) ? _file : new NotFoundFileInfo(subpath);

        public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;

        public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
    }

    private sealed class InMemoryFileInfo(string name, byte[] content, DateTimeOffset lastModified) : IFileInfo
    {
        public bool Exists => true;
        public long Length => content.Length;
        public string PhysicalPath => null;
        public string Name => name;
        public DateTimeOffset LastModified => lastModified;
        public bool IsDirectory => false;
        public Stream CreateReadStream() => new MemoryStream(content, writable: false);
    }
}
