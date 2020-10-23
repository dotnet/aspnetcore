// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

            Pages.Add(args.Page, new PageInformation(args.Page, logger));
        }

        internal BrowserContextOptions Contextualize(BrowserContextOptions browserContextOptions)
        {
            if (browserContextOptions?.RecordHar?.Path != null)
            {
                var identifier = Guid.NewGuid().ToString("N");
                browserContextOptions.RecordHar.Path = Path.Combine(browserContextOptions.RecordHar.Path, $"{identifier}.har");
                _harPath = browserContextOptions.RecordHar.Path;
            }

            return browserContextOptions;
        }
    }
}
