// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor
{
    internal abstract class RazorProjectEngineFactoryService : ILanguageService
    {
        public abstract RazorProjectEngine Create(string projectPath, Action<RazorProjectEngineBuilder> configure);
    }
}