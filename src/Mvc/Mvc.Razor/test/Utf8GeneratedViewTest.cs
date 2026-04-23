// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.WebEncoders.Testing;
using Moq;

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Tests that simulate Razor-compiled views using <c>WriteLiteral(ReadOnlyMemory&lt;byte&gt;)</c>
/// with UTF-8 string literals. These represent what the Razor compiler will emit once
/// it supports the <c>"..."u8</c> literal syntax for HTML content blocks.
/// </summary>
public class Utf8GeneratedViewTest
{
    [Fact]
    public async Task SimpleView_WritesUtf8LiteralsCorrectly()
    {
        var (page, buffer) = CreatePageWithBuffer<SimpleProductView>();

        await page.ExecuteAsync();

        var output = GetBufferContent(buffer);
        Assert.Equal(
            "<html>\r\n<head><title>Products</title></head>\r\n<body>\r\n<h1>Product List</h1>\r\n</body>\r\n</html>",
            output);
    }

    [Fact]
    public async Task ViewWithDynamicContent_MixesUtf8LiteralsAndEncodedValues()
    {
        var (page, buffer) = CreatePageWithBuffer<ProductDetailView>();
        page.ProductName = "Widget <Pro>";
        page.Price = 29.99m;

        await page.ExecuteAsync();

        var output = GetBufferContent(buffer);
        Assert.Equal(
            "<div class=\"product\">\r\n    <h2>HtmlEncode[[Widget <Pro>]]</h2>\r\n    <span class=\"price\">HtmlEncode[[29.99]]</span>\r\n</div>",
            output);
    }

    [Fact]
    public async Task ViewWithLoop_WritesUtf8LiteralsInLoop()
    {
        var (page, buffer) = CreatePageWithBuffer<ProductListView>();
        page.Items = ["Alpha", "Beta", "Gamma"];

        await page.ExecuteAsync();

        var output = GetBufferContent(buffer);
        Assert.Equal(
            "<ul>\r\n    <li>HtmlEncode[[Alpha]]</li>\r\n    <li>HtmlEncode[[Beta]]</li>\r\n    <li>HtmlEncode[[Gamma]]</li>\r\n</ul>",
            output);
    }

    [Fact]
    public async Task ViewWithConditional_WritesCorrectBranch()
    {
        var (page, buffer) = CreatePageWithBuffer<ConditionalView>();
        page.IsLoggedIn = true;
        page.UserName = "Alice";

        await page.ExecuteAsync();

        var output = GetBufferContent(buffer);
        Assert.Equal(
            "<nav>\r\n    <span>Welcome, HtmlEncode[[Alice]]!</span>\r\n</nav>",
            output);
    }

    [Fact]
    public async Task ViewWithConditional_WritesElseBranch()
    {
        var (page, buffer) = CreatePageWithBuffer<ConditionalView>();
        page.IsLoggedIn = false;

        await page.ExecuteAsync();

        var output = GetBufferContent(buffer);
        Assert.Equal(
            "<nav>\r\n    <a href=\"/login\">Sign In</a>\r\n</nav>",
            output);
    }

    [Fact]
    public async Task ViewWithMultiByteCharacters_PreservesUtf8Content()
    {
        var (page, buffer) = CreatePageWithBuffer<InternationalView>();

        await page.ExecuteAsync();

        var output = GetBufferContent(buffer);
        Assert.Equal(
            "<p>Héllo Wörld — 日本語テスト</p>",
            output);
    }

    private static (TPage page, ViewBuffer buffer) CreatePageWithBuffer<TPage>() where TPage : RazorPage, new()
    {
        var bufferScope = new TestViewBufferScope();
        var buffer = new ViewBuffer(bufferScope, "test-view", pageSize: 32);
        var writer = new ViewBufferTextWriter(buffer, Encoding.UTF8);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton<IViewBufferScope>(bufferScope)
            .BuildServiceProvider();

        var viewContext = new ViewContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            Mock.Of<IView>(),
            new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            Mock.Of<ITempDataDictionary>(),
            writer,
            new HtmlHelperOptions());

        var page = new TPage();
        page.ViewContext = viewContext;
        page.HtmlEncoder = new HtmlTestEncoder();

        return (page, buffer);
    }

    private static string GetBufferContent(ViewBuffer buffer)
    {
        using var writer = new StringWriter();
        buffer.WriteTo(writer, new HtmlTestEncoder());
        return writer.ToString();
    }

    #region Example Razor-compiled views using UTF-8 literals

    /// <summary>
    /// Simulates a simple Razor view that only contains HTML literals (no dynamic content).
    /// Equivalent to:
    /// <code>
    /// &lt;html&gt;
    /// &lt;head&gt;&lt;title&gt;Products&lt;/title&gt;&lt;/head&gt;
    /// &lt;body&gt;
    /// &lt;h1&gt;Product List&lt;/h1&gt;
    /// &lt;/body&gt;
    /// &lt;/html&gt;
    /// </code>
    /// </summary>
    [CompilerGenerated]
    internal sealed class SimpleProductView : RazorPage
    {
        private static class __Literals
        {
            public static readonly byte[] Literal_0 = "<html>\r\n<head><title>Products</title></head>\r\n<body>\r\n<h1>Product List</h1>\r\n</body>\r\n</html>"u8.ToArray();
        }

        public override Task ExecuteAsync()
        {
            WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_0));
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Simulates a Razor view with dynamic content mixed with UTF-8 HTML literals.
    /// Equivalent to:
    /// <code>
    /// &lt;div class="product"&gt;
    ///     &lt;h2&gt;@ProductName&lt;/h2&gt;
    ///     &lt;span class="price"&gt;@Price&lt;/span&gt;
    /// &lt;/div&gt;
    /// </code>
    /// </summary>
    [CompilerGenerated]
    internal sealed class ProductDetailView : RazorPage
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }

        private static class __Literals
        {
            public static readonly byte[] Literal_0 = "<div class=\"product\">\r\n    <h2>"u8.ToArray();
            public static readonly byte[] Literal_1 = "</h2>\r\n    <span class=\"price\">"u8.ToArray();
            public static readonly byte[] Literal_2 = "</span>\r\n</div>"u8.ToArray();
        }

        public override Task ExecuteAsync()
        {
            WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_0));
            Write(ProductName);
            WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_1));
            Write(Price);
            WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_2));
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Simulates a Razor view with a loop over dynamic content.
    /// Equivalent to:
    /// <code>
    /// &lt;ul&gt;
    /// @foreach (var item in Items)
    /// {
    ///     &lt;li&gt;@item&lt;/li&gt;
    /// }
    /// &lt;/ul&gt;
    /// </code>
    /// </summary>
    [CompilerGenerated]
    internal sealed class ProductListView : RazorPage
    {
        public IReadOnlyList<string> Items { get; set; } = [];

        private static class __Literals
        {
            public static readonly byte[] Literal_0 = "<ul>"u8.ToArray();
            public static readonly byte[] Literal_1 = "\r\n    <li>"u8.ToArray();
            public static readonly byte[] Literal_2 = "</li>"u8.ToArray();
            public static readonly byte[] Literal_3 = "\r\n</ul>"u8.ToArray();
        }

        public override Task ExecuteAsync()
        {
            WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_0));
            foreach (var item in Items)
            {
                WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_1));
                Write(item);
                WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_2));
            }
            WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_3));
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Simulates a Razor view with conditional rendering.
    /// Equivalent to:
    /// <code>
    /// &lt;nav&gt;
    /// @if (IsLoggedIn)
    /// {
    ///     &lt;span&gt;Welcome, @UserName!&lt;/span&gt;
    /// }
    /// else
    /// {
    ///     &lt;a href="/login"&gt;Sign In&lt;/a&gt;
    /// }
    /// &lt;/nav&gt;
    /// </code>
    /// </summary>
    [CompilerGenerated]
    internal sealed class ConditionalView : RazorPage
    {
        public bool IsLoggedIn { get; set; }
        public string UserName { get; set; } = string.Empty;

        private static class __Literals
        {
            public static readonly byte[] Literal_0 = "<nav>\r\n    "u8.ToArray();
            public static readonly byte[] Literal_1 = "<span>Welcome, "u8.ToArray();
            public static readonly byte[] Literal_2 = "!</span>"u8.ToArray();
            public static readonly byte[] Literal_3 = "<a href=\"/login\">Sign In</a>"u8.ToArray();
            public static readonly byte[] Literal_4 = "\r\n</nav>"u8.ToArray();
        }

        public override Task ExecuteAsync()
        {
            WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_0));
            if (IsLoggedIn)
            {
                WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_1));
                Write(UserName);
                WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_2));
            }
            else
            {
                WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_3));
            }
            WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_4));
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Simulates a Razor view with multi-byte UTF-8 characters to verify correct encoding handling.
    /// </summary>
    [CompilerGenerated]
    internal sealed class InternationalView : RazorPage
    {
        private static class __Literals
        {
            public static readonly byte[] Literal_0 = "<p>Héllo Wörld — 日本語テスト</p>"u8.ToArray();
        }

        public override Task ExecuteAsync()
        {
            WriteLiteral(new ReadOnlyMemory<byte>(__Literals.Literal_0));
            return Task.CompletedTask;
        }
    }

    #endregion
}
