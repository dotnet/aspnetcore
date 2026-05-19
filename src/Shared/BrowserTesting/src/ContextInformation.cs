// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace Microsoft.AspNetCore.BrowserTesting;

public class ContextInformation
{
    private readonly ILoggerFactory _factory;
    private string _harPath;

    public ContextInformation(ILoggerFactory factory)
    {
        _factory = factory;
    }

    public IDictionary<IPage, PageInformation> Pages { get; } = new Dictionary<IPage, PageInformation>();

    internal void Attach(IBrowserContext context)
    {
        context.Page += AttachToPage;
    }

    private void AttachToPage(object sender, IPage page)
    {
        var logger = _factory.CreateLogger<PageInformation>();
        if (_harPath != null)
        {
            logger.LogInformation($"Network trace will be saved at '{_harPath}'");
        }

        var pageInfo = new PageInformation(page, logger);
        Pages.Add(page, pageInfo);
        page.Close += CleanupPage;
        page.Crash += CleanupPage;
    }

    private void CleanupPage(object sender, IPage page)
    {
        if (Pages.TryGetValue(page, out var info))
        {
            info.Dispose();
            Pages.Remove(page);
        }
    }

    internal BrowserNewContextOptions ConfigureUniqueHarPath(BrowserNewContextOptions browserContextOptions)
    {
        var uploadDir = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
        if (browserContextOptions?.RecordHarPath != null)
        {
            var identifier = Guid.NewGuid().ToString("N");
            browserContextOptions.RecordHarPath = Path.Combine(
                string.IsNullOrEmpty(uploadDir) ? browserContextOptions.RecordHarPath : uploadDir,
                $"{identifier}.har");
            _harPath = browserContextOptions.RecordHarPath;
        }

        if (browserContextOptions?.RecordVideoDir != null)
        {
            if (!string.IsNullOrEmpty(uploadDir))
            {
                browserContextOptions.RecordVideoDir = uploadDir;
            }
        }

        return browserContextOptions;
    }
}
