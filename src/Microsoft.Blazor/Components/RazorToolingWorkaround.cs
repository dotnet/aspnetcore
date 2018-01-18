// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

/*
 * Currently if you have a .cshtml file in a project with <Project Sdk="Microsoft.NET.Sdk.Web">,
 * Visual Studio will run design-time builds for the .cshtml file that assume certain ASP.NET MVC
 * APIs exist. Since those namespaces and types wouldn't normally exist for Blazor client apps,
 * this leads to spurious errors in the Errors List pane, even though there aren't actually any
 * errors on build. As a workaround, we define here a minimal set of namespaces/types that satisfy
 * the design-time build.
 * 
 * TODO: Track down what is triggering the unwanted design-time build and find out how to disable it.
 * Then this file can be removed entirely.
 */

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Mvc
{
    public interface IUrlHelper { }
    public interface IViewComponentHelper { }
}

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class RazorPage<T> {

        // This needs to be defined otherwise the VS tooling complains that there's no ExecuteAsync method to override.
        public virtual Task ExecuteAsync()
            => throw new NotImplementedException($"Blazor components do not implement {nameof(ExecuteAsync)}.");

    }

    namespace Internal
    {
        public class RazorInjectAttributeAttribute : Attribute { }
    }
}

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    public interface IJsonHelper { }
    public interface IHtmlHelper<T> { }
}

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public interface IModelExpressionProvider { }
}
