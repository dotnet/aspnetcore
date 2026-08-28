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
/// Benchmarks comparing <c>WriteLiteral(string)</c> with static UTF-8 backed <see cref="IHtmlContent"/> instances
/// through the full MVC view rendering pipeline: ViewBuffer → PagedBufferedTextWriter →
/// HttpResponseStreamWriter → Stream.
/// </summary>
[MemoryDiagnoser]
public class WriteLiteralUtf8Benchmark
{
    private MemoryStream _outputStream;

    [GlobalSetup]
    public void Setup()
    {
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

        var view = new StringWriteLiteralView();
        await RenderViewAsync(view, _outputStream);
    }

    /// <summary>
    /// Renders a view using static UTF-8 backed <see cref="IHtmlContent"/> instances.
    /// </summary>
    [Benchmark(Description = "WriteLiteral(static IHtmlContent)")]
    public async Task WriteLiteral_Utf8()
    {
        _outputStream.Position = 0;
        _outputStream.SetLength(0);

        var view = new Utf8WriteLiteralView();
        await RenderViewAsync(view, _outputStream);
    }

    [Benchmark(Description = "WriteLiteral(string, mixed sink)")]
    public async Task WriteLiteral_String_MixedSink()
    {
        _outputStream.Position = 0;
        _outputStream.SetLength(0);

        var view = new StringWriteLiteralView();
        await RenderViewAsync(view, _outputStream, useMixedSink: true);
    }

    [Benchmark(Description = "WriteLiteral(static IHtmlContent, mixed sink)")]
    public async Task WriteLiteral_Utf8_MixedSink()
    {
        _outputStream.Position = 0;
        _outputStream.SetLength(0);

        var view = new Utf8WriteLiteralView();
        await RenderViewAsync(view, _outputStream, useMixedSink: true);
    }

    private static async Task RenderViewAsync(RazorPage page, Stream outputStream, bool useMixedSink = false)
    {
        var bufferScope = new BenchmarkViewBufferScope();
        var buffer = new ViewBuffer(bufferScope, "benchmark-view", ViewBuffer.ViewPageSize);
        var viewBufferWriter = new ViewBufferTextWriter(buffer, Encoding.UTF8);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection()
                .AddSingleton<IViewBufferScope>(bufferScope)
                .BuildServiceProvider()
        };

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

        using var responseWriter = new HttpResponseStreamWriter(outputStream, Encoding.UTF8);
        if (useMixedSink)
        {
            await using var mixedWriter = new MixedUtf8BufferedTextWriter(ArrayPool<char>.Shared, responseWriter);
            await buffer.WriteToAsync(mixedWriter, HtmlEncoder.Default);
            await mixedWriter.FlushAsync();
            return;
        }

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

            for (var i = 0; i < 500; i++)
            {
                WriteLiteral("                <div class=\"col-md-4 mb-3\">\r\n                    <div class=\"card\">\r\n                        <div class=\"card-body\">\r\n                            <h5 class=\"card-title\">");
                Write("Model.Name"); // Simulates @Model.Name
                WriteLiteral("</h5>\r\n                            <p class=\"card-text text-muted\">");
                Write("Model.Description that's longer and needs more work"); // Simulates @Model.Description
                WriteLiteral("</p>\r\n                            <div class=\"d-flex justify-content-between align-items-center\">\r\n                                <span class=\"h5 mb-0\">");
                Write(123.45); // Simulates @Model.Price
                WriteLiteral("</span>\r\n                                <a href=\"/products/details/");
                Write(123456); // Simulates @Model.Id
                WriteLiteral("\" class=\"btn btn-primary\">View Details</a>\r\n                            </div>\r\n                        </div>\r\n                    </div>\r\n                </div>\r\n");
            }

            WriteLiteral("            </div>\r\n        </main>\r\n    </div>\r\n    <footer class=\"border-top footer text-muted\">\r\n        <div class=\"container\">\r\n            &copy; 2026 - My Store - <a href=\"/Home/Privacy\">Privacy</a>\r\n        </div>\r\n    </footer>\r\n    <script src=\"/js/site.js\"></script>\r\n</body>\r\n</html>");

            return Task.CompletedTask;
        }
    }

    // Simulated generated view using static UTF-8 backed IHtmlContent instances.
    [CompilerGenerated]
    private sealed class Utf8WriteLiteralView : RazorPage
    {
        private static readonly IHtmlContent s_literal0 = CreateUtf8HtmlContent("<!DOCTYPE html>\r\n<html lang=\"en\">\r\n<head>\r\n    <meta charset=\"utf-8\" />\r\n    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\" />\r\n    <title>Product Listing</title>\r\n    <link rel=\"stylesheet\" href=\"/css/site.css\" />\r\n</head>\r\n<body>\r\n    <header>\r\n        <nav class=\"navbar navbar-expand-sm navbar-light bg-white border-bottom box-shadow mb-3\">\r\n            <div class=\"container\">\r\n                <a class=\"navbar-brand\" href=\"/\">My Store</a>\r\n            </div>\r\n        </nav>\r\n    </header>\r\n    <div class=\"container\">\r\n        <main role=\"main\" class=\"pb-3\">\r\n            <h1>Products</h1>\r\n            <div class=\"row\">\r\n"u8);
        private static readonly IHtmlContent s_literal1 = CreateUtf8HtmlContent("                <div class=\"col-md-4 mb-3\">\r\n                    <div class=\"card\">\r\n                        <div class=\"card-body\">\r\n                            <h5 class=\"card-title\">"u8);
        private static readonly IHtmlContent s_literal2 = CreateUtf8HtmlContent("</h5>\r\n                            <p class=\"card-text text-muted\">"u8);
        private static readonly IHtmlContent s_literal3 = CreateUtf8HtmlContent("</p>\r\n                            <div class=\"d-flex justify-content-between align-items-center\">\r\n                                <span class=\"h5 mb-0\">"u8);
        private static readonly IHtmlContent s_literal4 = CreateUtf8HtmlContent("</span>\r\n                                <a href=\"/products/details/"u8);
        private static readonly IHtmlContent s_literal5 = CreateUtf8HtmlContent("\" class=\"btn btn-primary\">View Details</a>\r\n                            </div>\r\n                        </div>\r\n                    </div>\r\n                </div>\r\n"u8);
        private static readonly IHtmlContent s_literal6 = CreateUtf8HtmlContent("            </div>\r\n        </main>\r\n    </div>\r\n    <footer class=\"border-top footer text-muted\">\r\n        <div class=\"container\">\r\n            &copy; 2026 - My Store - <a href=\"/Home/Privacy\">Privacy</a>\r\n        </div>\r\n    </footer>\r\n    <script src=\"/js/site.js\"></script>\r\n</body>\r\n</html>"u8);

        public override Task ExecuteAsync()
        {
            WriteLiteral(s_literal0);

            for (var i = 0; i < 500; i++)
            {
                WriteLiteral(s_literal1);
                Write("Model.Name"); // Simulates @Model.Name
                WriteLiteral(s_literal2);
                Write("Model.Description that's longer and needs more work"); // Simulates @Model.Description
                WriteLiteral(s_literal3);
                Write(123.45); // Simulates @Model.Price
                WriteLiteral(s_literal4);
                Write(123456); // Simulates @Model.Id
                WriteLiteral(s_literal5);
            }

            WriteLiteral(s_literal6);

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
