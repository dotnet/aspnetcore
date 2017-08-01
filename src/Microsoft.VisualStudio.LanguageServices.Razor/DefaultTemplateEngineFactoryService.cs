// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Host;
using Mvc1_X = Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;
using MvcLatest = Microsoft.AspNetCore.Mvc.Razor.Extensions;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class DefaultTemplateEngineFactoryService : RazorTemplateEngineFactoryService
    {
        private static readonly Version LatestSupportedMvc = new Version(2, 1, 0);

        private readonly HostLanguageServices _services;

        public DefaultTemplateEngineFactoryService(HostLanguageServices services)
        {
            _services = services;
        }

        public override RazorTemplateEngine Create(string projectPath, Action<IRazorEngineBuilder> configure)
        {
            if (projectPath == null)
            {
                throw new ArgumentNullException(nameof(projectPath));
            }

            RazorEngine engine;
            var mvcVersion = GetMvcVersion(projectPath);
            if (mvcVersion?.Major == 1)
            {
                engine = RazorEngine.CreateDesignTime(b =>
                {
                    configure?.Invoke(b);

                    Mvc1_X.RazorExtensions.Register(b);

                    if (mvcVersion?.Minor >= 1)
                    {
                        Mvc1_X.RazorExtensions.RegisterViewComponentTagHelpers(b);
                    }
                });

                var templateEngine = new Mvc1_X.MvcRazorTemplateEngine(engine, RazorProject.Create(projectPath));
                templateEngine.Options.ImportsFileName = "_ViewImports.cshtml";
                return templateEngine;
            }
            else
            {
                if (mvcVersion?.Major != LatestSupportedMvc.Major)
                {
                    // TODO: Log unknown Mvc version. Something like
                    // Could not construct Razor engine for Mvc version '{mvcVersion}'. Falling back to Razor engine for Mvc '{LatestSupportedMvc}'.
                }

                engine = RazorEngine.CreateDesignTime(b =>
                {
                    configure?.Invoke(b);

                    MvcLatest.RazorExtensions.Register(b);
                });

                var templateEngine = new MvcLatest.MvcRazorTemplateEngine(engine, RazorProject.Create(projectPath));
                templateEngine.Options.ImportsFileName = "_ViewImports.cshtml";
                return templateEngine;
            }
        }

        private Version GetMvcVersion(string projectPath)
        {
            // TODO: Need to figure out the actual referenced Mvc version in the project. https://github.com/aspnet/Razor/issues/1183
            return null;
        }
    }
}
