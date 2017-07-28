// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Razor.Extensions;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class DefaultTemplateEngineFactoryService : RazorTemplateEngineFactoryService
    {
        public override RazorTemplateEngine Create(string projectPath, Action<IRazorEngineBuilder> configure)
        {
            if (projectPath == null)
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            var engine = RazorEngine.CreateDesignTime(b =>
            {
                configure?.Invoke(b);

                // For now we're hardcoded to use MVC's extensibility.
                RazorExtensions.Register(b);
            });

            var templateEngine = new MvcRazorTemplateEngine(engine, RazorProject.Create(projectPath));
            templateEngine.Options.ImportsFileName = "_ViewImports.cshtml";
            return templateEngine;
        }
    }
}
