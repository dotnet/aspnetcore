// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Mvc.Performance
{
    public class HelperPerformanceBenchmark : RuntimePerformanceBenchmarkBase
    {
        private Random _rand = new Random();
        public HelperPerformanceBenchmark() : base(
            "~/Views/HelperTyped.cshtml",
            "~/Views/HelperDynamic.cshtml",
            "~/Views/HelperPartialSync.cshtml",
            "~/Views/HelperPartialAsync.cshtml",
            "~/Views/HelperExtensions.cshtml",
            "~/Views/HelperPartialTagHelper.cshtml")
        {
        }

        protected override object Model => _rand.Next().ToString();
    }
}
