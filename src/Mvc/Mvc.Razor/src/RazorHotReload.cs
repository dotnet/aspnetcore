// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Razor.Compilation;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.Razor;

internal sealed class RazorHotReload
{
    private readonly RazorCompiledItemFeatureProvider? _razorCompiledItemFeatureProvider;
    private readonly DefaultViewCompiler? _defaultViewCompiler;
    private readonly RazorViewEngine? _razorViewEngine;
    private readonly RazorPageActivator? _razorPageActivator;
    private readonly DefaultTagHelperFactory? _defaultTagHelperFactory;
    private readonly TagHelperComponentPropertyActivator? _tagHelperComponentPropertyActivator;

    public RazorHotReload(
        IRazorViewEngine razorViewEngine,
        IRazorPageActivator razorPageActivator,
        ITagHelperFactory tagHelperFactory,
        IViewCompilerProvider viewCompilerProvider,
        ITagHelperComponentPropertyActivator tagHelperComponentPropertyActivator,
        ApplicationPartManager applicationPartManager)
    {
        // For Razor view services, use the service locator pattern because they views not be registered by default.
        _razorCompiledItemFeatureProvider = applicationPartManager.FeatureProviders.OfType<RazorCompiledItemFeatureProvider>().FirstOrDefault();

        if (viewCompilerProvider is DefaultViewCompilerProvider defaultViewCompilerProvider)
        {
            _defaultViewCompiler = defaultViewCompilerProvider.Compiler;
        }

        if (razorViewEngine.GetType() == typeof(RazorViewEngine))
        {
            _razorViewEngine = (RazorViewEngine)razorViewEngine;
        }

        if (razorPageActivator.GetType() == typeof(RazorPageActivator))
        {
            _razorPageActivator = (RazorPageActivator)razorPageActivator;
        }

        if (tagHelperFactory is DefaultTagHelperFactory defaultTagHelperFactory)
        {
            _defaultTagHelperFactory = defaultTagHelperFactory;
        }

        if (tagHelperComponentPropertyActivator is TagHelperComponentPropertyActivator defaultTagHelperComponentPropertyActivator)
        {
            _tagHelperComponentPropertyActivator = defaultTagHelperComponentPropertyActivator;
        }
    }

    public void ClearCache(Type[]? changedTypes)
    {
        // Update the RazorCompiledItemFeatureProvider cache before the DefaultViewCompiler's cache is cleared.
        _razorCompiledItemFeatureProvider?.UpdateCache(changedTypes);

        _defaultViewCompiler?.ClearCache();
        _razorViewEngine?.ClearCache();
        _razorPageActivator?.ClearCache();
        _defaultTagHelperFactory?.ClearCache();
        _tagHelperComponentPropertyActivator?.ClearCache();
    }
}
