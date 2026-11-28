// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.StaticFiles.Tests;

public class HtmlDirectoryFormatterTests
{
    [Fact]
    public async Task GenerateContentAsync_LongRequestPathWithBackslashes_GeneratesBreadcrumbs()
    {
        const int segmentCount = 128;
        var requestPath = "/dir/" + string.Join("/", Enumerable.Repeat("\\", segmentCount)) + "/";

        var context = new DefaultHttpContext { Request = { Path = requestPath }, Response = { Body = new MemoryStream() } };

        var formatter = new HtmlDirectoryFormatter(HtmlEncoder.Default);

        await formatter.GenerateContentAsync(
            context,
            Enumerable.Empty<IFileInfo>());

        context.Response.Body.Position = 0;

        using var reader = new StreamReader(context.Response.Body);
        var html = await reader.ReadToEndAsync();

        Assert.Equal(segmentCount + 2, html.Split("<a href=").Length - 1);
        Assert.Contains("<a href=\"/dir/\\/\">", html);
        Assert.Contains("<a href=\"/dir/\\/\\/\">", html);
    }
}
