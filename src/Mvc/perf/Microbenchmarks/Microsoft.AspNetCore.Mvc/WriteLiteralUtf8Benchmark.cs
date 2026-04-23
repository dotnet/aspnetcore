// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

/// <summary>
/// Benchmarks comparing <c>WriteLiteral(string)</c> vs <c>WriteLiteral(ReadOnlyMemory&lt;byte&gt;)</c>
/// through the full MVC view rendering pipeline: ViewBuffer → PagedBufferedTextWriter →
/// HttpResponseStreamWriter → Stream.
/// </summary>
[MemoryDiagnoser]
public class WriteLiteralUtf8Benchmark
{
    private StringWriteLiteralView _stringView;
    private Utf8WriteLiteralView _utf8View;
    private MemoryStream _outputStream;

    [GlobalSetup]
    public void Setup()
    {
        _stringView = new StringWriteLiteralView();
        _utf8View = new Utf8WriteLiteralView();
        _outputStream = new MemoryStream(capacity: 16 * 1024);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _outputStream.Dispose();
    }

    /// <summary>
    /// Baseline: renders a view using the existing <c>WriteLiteral(string)</c> path.
    /// HTML literals are stored as strings and go through char-to-byte encoding at flush time.
    /// </summary>
    [Benchmark(Description = "WriteLiteral(string)", Baseline = true)]
    public async Task WriteLiteral_String()
    {
        _outputStream.Position = 0;
        _outputStream.SetLength(0);
        await RenderViewAsync(_stringView, _outputStream);
    }

    /// <summary>
    /// New: renders a view using <c>WriteLiteral(ReadOnlyMemory&lt;byte&gt;)</c>.
    /// UTF-8 literal bytes flow directly to the response stream with zero string conversion.
    /// </summary>
    [Benchmark(Description = "WriteLiteral(ROM<byte>)")]
    public async Task WriteLiteral_Utf8()
    {
        _outputStream.Position = 0;
        _outputStream.SetLength(0);
        await RenderViewAsync(_utf8View, _outputStream);
    }

    private static async Task RenderViewAsync(RazorPage page, Stream outputStream)
    {
        var bufferScope = new BenchmarkViewBufferScope();
        var buffer = new ViewBuffer(bufferScope, "benchmark-view", ViewBuffer.ViewPageSize);
        var viewBufferWriter = new ViewBufferTextWriter(buffer, Encoding.UTF8);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton<IViewBufferScope>(bufferScope)
            .BuildServiceProvider();

        var viewContext = new ViewContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            Mock.Of<IView>(),
            new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            Mock.Of<ITempDataDictionary>(),
            viewBufferWriter,
            new HtmlHelperOptions());

        page.ViewContext = viewContext;
        page.HtmlEncoder = HtmlEncoder.Default;

        // Execute the view (populates the ViewBuffer)
        await page.ExecuteAsync();

        // Flush through the real writer chain: ViewBuffer → PagedBufferedTextWriter → HttpResponseStreamWriter → Stream
        using var responseWriter = new HttpResponseStreamWriter(outputStream, Encoding.UTF8);
        await using var pagedWriter = new PagedBufferedTextWriter(ArrayPool<char>.Shared, responseWriter);
        await buffer.WriteToAsync(pagedWriter, HtmlEncoder.Default);
        await pagedWriter.FlushAsync();
    }

    // Simulated view using WriteLiteral(string) — the existing path
    [CompilerGenerated]
    private sealed class StringWriteLiteralView : RazorPage
    {
        // Simulates a typical product listing page with repeated HTML structure
        public override Task ExecuteAsync()
        {
            WriteLiteral("<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"utf-8\" />\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />\r\n    <title>Product Listing</title>\r\n    <link rel=\"stylesheet\" href=\"/css/site.css\" />\r\n</head>\r\n<body>\r\n    <header>\r\n        <nav class=\"navbar navbar-expand-sm navbar-light bg-white border-bottom box-shadow mb-3\">\r\n            <div class=\"container\">\r\n                <a class=\"navbar-brand\" href=\"/\">My Store</a>\r\n            </div>\r\n        </nav>\r\n    </header>\r\n    <div class=\"container\">\r\n        <main role=\"main\" class=\"pb-3\">\r\n            <h1>Products</h1>\r\n            <div class=\"row\">\r\n");

            for (var i = 0; i < 20; i++)
            {
                WriteLiteral("                <div class=\"col-md-4 mb-3\">\r\n                    <div class=\"card\">\r\n                        <div class=\"card-body\">\r\n                            <h5 class=\"card-title\">");
                Write(HtmlString.Empty); // Simulates @Model.Name
                WriteLiteral("</h5>\r\n                            <p class=\"card-text text-muted\">");
                Write(HtmlString.Empty); // Simulates @Model.Description
                WriteLiteral("</p>\r\n                            <div class=\"d-flex justify-content-between align-items-center\">\r\n                                <span class=\"h5 mb-0\">");
                Write(HtmlString.Empty); // Simulates @Model.Price
                WriteLiteral("</span>\r\n                                <a href=\"/products/details/");
                Write(HtmlString.Empty); // Simulates @Model.Id
                WriteLiteral("\" class=\"btn btn-primary\">View Details</a>\r\n                            </div>\r\n                        </div>\r\n                    </div>\r\n                </div>\r\n");
            }

            WriteLiteral("            </div>\r\n        </main>\r\n    </div>\r\n    <footer class=\"border-top footer text-muted\">\r\n        <div class=\"container\">\r\n            &copy; 2026 - My Store - <a href=\"/Home/Privacy\">Privacy</a>\r\n        </div>\r\n    </footer>\r\n    <script src=\"/js/site.js\"></script>\r\n</body>\r\n</html>");

            return Task.CompletedTask;
        }
    }

    // Simulated view using WriteLiteral(ReadOnlyMemory<byte>) — the new UTF-8 path
    [CompilerGenerated]
    private sealed class Utf8WriteLiteralView : RazorPage
    {
        private static class __Literals
        {
            public static readonly byte[] Literal_0 = "<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"utf-8\" />\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />\r\n    <title>Product Listing</title>\r\n    <link rel=\"stylesheet\" href=\"/css/site.css\" />\r\n</head>\r\n<body>\r\n    <header>\r\n        <nav class=\"navbar navbar-expand-sm navbar-light bg-white border-bottom box-shadow mb-3\">\r\n            <div class=\"container\">\r\n                <a class=\"navbar-brand\" href=\"/\">My Store</a>\r\n            </div>\r\n        </nav>\r\n    </header>\r\n    <div class=\"container\">\r\n        <main role=\"main\" class=\"pb-3\">\r\n            <h1>Products</h1>\r\n            <div class=\"row\">\r\n"u8.ToArray();
            public static readonly byte[] Literal_1 = "                <div class=\"col-md-4 mb-3\">\r\n                    <div class=\"card\">\r\n                        <div class=\"card-body\">\r\n                            <h5 class=\"card-title\">"u8.ToArray();
            public static readonly byte[] Literal_2 = "</h5>\r\n                            <p class=\"card-text text-muted\">"u8.ToArray();
            public static readonly byte[] Literal_3 = "</p>\r\n                            <div class=\"d-flex justify-content-between align-items-center\">\r\n                                <span class=\"h5 mb-0\">"u8.ToArray();
            public static readonly byte[] Literal_4 = "</span>\r\n                                <a href=\"/products/details/"u8.ToArray();
            public static readonly byte[] Literal_5 = "\" class=\"btn btn-primary\">View Details</a>\r\n                            </div>\r\n                        </div>\r\n                    </div>\r\n                </div>\r\n"u8.ToArray();
            public static readonly byte[] Literal_6 = "            </div>\r\n        </main>\r\n    </div>\r\n    <footer class=\"border-top footer text-muted\">\r\n        <div class=\"container\">\r\n            &copy; 2026 - My Store - <a href=\"/Home/Privacy\">Privacy</a>\r\n        </div>\r\n    </footer>\r\n    <script src=\"/js/site.js\"></script>\r\n</body>\r\n</html>"u8.ToArray();
        }

        public override Task ExecuteAsync()
        {
            WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_0));

            for (var i = 0; i < 20; i++)
            {
                WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_1));
                Write(HtmlString.Empty); // Simulates @Model.Name
                WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_2));
                Write(HtmlString.Empty); // Simulates @Model.Description
                WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_3));
                Write(HtmlString.Empty); // Simulates @Model.Price
                WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_4));
                Write(HtmlString.Empty); // Simulates @Model.Id
                WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_5));
            }

            WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_6));

            return Task.CompletedTask;
        }
    }

    // Minimal IViewBufferScope for benchmarks — avoids DI container overhead
    private sealed class BenchmarkViewBufferScope : IViewBufferScope
    {
        public ViewBufferValue[] GetPage(int size) => new ViewBufferValue[size];

        public void ReturnSegment(ViewBufferValue[] segment)
        {
            Array.Clear(segment, 0, segment.Length);
        }

        public TextWriter CreateWriter(TextWriter writer) =>
            new PagedBufferedTextWriter(ArrayPool<char>.Shared, writer);
    }
}
