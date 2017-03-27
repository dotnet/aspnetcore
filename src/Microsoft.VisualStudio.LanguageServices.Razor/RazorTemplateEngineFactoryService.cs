// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Evolution;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public abstract class RazorTemplateEngineFactoryService
    {
        public abstract RazorTemplateEngine Create(string projectPath, Action<IRazorEngineBuilder> configure);
    }
}
