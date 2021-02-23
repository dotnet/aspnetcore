// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebView.Hosting
{
    public class WebViewHostBuilder
    {
        public IServiceCollection Services { get; } = new ServiceCollection();

        public static WebViewHostBuilder CreateDefault(string[] args = null)
        {
            return new WebViewHostBuilder();
        }

        public WebViewHost Build()
        {
            return new WebViewHost();
        }
    }
}
