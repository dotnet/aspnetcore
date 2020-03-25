// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.WebAssembly.Services
{
    internal readonly struct WebAssemblyLoggerInformation
    {
        public WebAssemblyLoggerInformation(ILoggerProvider provider, string category) : this()
        {
            ProviderType = provider.GetType();
            Logger = provider.CreateLogger(category);
            Category = category;
        }

        public ILogger Logger { get; }

        public string Category { get; }

        public Type ProviderType { get; }
    }
}
