// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.Fallback;

namespace Microsoft.AspNet.Mvc
{
    public abstract class RazorPreCompileModule : ICompileModule
    {
        private readonly IServiceProvider _appServices;

        public RazorPreCompileModule(IServiceProvider services)
        {
            _appServices = services;
        }

        public virtual void BeforeCompile(IBeforeCompileContext context)
        {
            var sc = new ServiceCollection();
            sc.Add(MvcServices.GetDefaultServices());
            var sp = sc.BuildServiceProvider(_appServices);

            var viewCompiler = new RazorPreCompiler(sp);
            viewCompiler.CompileViews(context);
        }

        public void AfterCompile(IAfterCompileContext context)
        {
        }
    }
}

namespace Microsoft.Framework.Runtime
{
    [AssemblyNeutral]
    public interface ICompileModule
    {
        void BeforeCompile(IBeforeCompileContext context);

        void AfterCompile(IAfterCompileContext context);
    }

    [AssemblyNeutral]
    public interface IAfterCompileContext
    {
        CSharpCompilation CSharpCompilation { get; set; }

        IList<Diagnostic> Diagnostics { get; }
    }
}