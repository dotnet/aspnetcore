// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using PlaywrightSharp;

namespace Microsoft.AspNetCore.BrowserTesting
{
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

        private void AttachToPage(object sender, PageEventArgs args)
        {
            var logger = _factory.CreateLogger<PageInformation>();
            if (_harPath != null)
            {
                logger.LogInformation($"Network trace will be saved at '{_harPath}'");
            }

            var pageInfo = new PageInformation(args.Page, logger);
            Pages.Add(args.Page, pageInfo);
            args.Page.Close += CleanupPage;
            args.Page.Crash += CleanupPage;
        }

        private void CleanupPage(object sender, EventArgs e)
        {
            var page = (IPage)sender;
            if (Pages.TryGetValue(page, out var info))
            {
                info.Dispose();
                Pages.Remove(page);
            }
        }

        internal BrowserContextOptions ConfigureUniqueHarPath(BrowserContextOptions browserContextOptions)
        {
            var uploadDir = Environment.GetEnvironmentVariable("HELIX_WORKITEM_UPLOAD_ROOT");
            if (browserContextOptions?.RecordHar?.Path != null)
            {
                var identifier = Guid.NewGuid().ToString("N");
                browserContextOptions.RecordHar.Path = Path.Combine(
                    string.IsNullOrEmpty(uploadDir) ? browserContextOptions.RecordHar.Path : uploadDir,
                    $"{identifier}.har");
                _harPath = browserContextOptions.RecordHar.Path;
            }

            if (browserContextOptions?.RecordVideo?.Dir != null)
            {
                if (!string.IsNullOrEmpty(uploadDir))
                {
                    browserContextOptions.RecordVideo.Dir = uploadDir;
                }
            }

            return browserContextOptions;
        }
    }
}
