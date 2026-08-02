// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Discovery;

internal class DefaultRazorComponentApplication<TComponent> : IRazorComponentApplication
{
    public static IRazorComponentApplication Instance { get; } = new DefaultRazorComponentApplication<TComponent>();

    public DefaultRazorComponentApplication()
    {
    }

    public ComponentApplicationBuilder GetBuilder()
    {
        var builder = new ComponentApplicationBuilder();
        // TODO: We'll have to figure out the exact API shape here
        // once we support discovery, since this will be generated on the user
        // app and will be public.
        // Similarly, RegisterPages will have to be called by the user app code
        // with the source generated code.
        // The builder provides the ability to choose what assemblies will be
        // considered and checked as a source for endpoints.
        // We also want to have a bit more granularity and exclude/replace individual
        // pages from within a given assembly.
        // We don't know if we need a generic "Feature" abstraction that can be registered
        // or if we can instead extend the builder with new methods whenever we need to add
        // new functionality.
        // The builder is going to be responsible for generating the code that replaces
        // scanning and reflection wherever we do it, so it needs to be something that
        // the user can configure.
        // In general, I want to avoid the "everything is a feature" for things like Pages.
        // The way to tweak any aspect that affect a page is through conventions in the
        // endpoint metadata.
        // We might expose something like the PageCollection or PageFeature in the future
        // so that users can decide the list of things that get considered as endpoints.
        var assembly = typeof(TComponent).Assembly;
        return IRazorComponentApplication.GetBuilderForAssembly(builder, assembly);
    }
}
