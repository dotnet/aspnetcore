// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;

internal sealed class DefaultPageFactoryProvider : IPageFactoryProvider
{
    private readonly IPageActivatorProvider _pageActivator;
    private readonly IModelMetadataProvider _modelMetadataProvider;
    private readonly RazorPagePropertyActivator.PropertyValueAccessors _propertyAccessors;

    public DefaultPageFactoryProvider(
        IPageActivatorProvider pageActivator,
        IModelMetadataProvider metadataProvider,
        IUrlHelperFactory urlHelperFactory,
        IJsonHelper jsonHelper,
        DiagnosticListener diagnosticListener,
        HtmlEncoder htmlEncoder,
        IModelExpressionProvider modelExpressionProvider)
    {
        _pageActivator = pageActivator;
        _modelMetadataProvider = metadataProvider;
        _propertyAccessors = new RazorPagePropertyActivator.PropertyValueAccessors
        {
            UrlHelperAccessor = urlHelperFactory.GetUrlHelper,
            JsonHelperAccessor = context => jsonHelper,
            DiagnosticSourceAccessor = context => diagnosticListener,
            HtmlEncoderAccessor = context => htmlEncoder,
            ModelExpressionProviderAccessor = context => modelExpressionProvider,
        };
    }

    public Func<PageContext, ViewContext, object> CreatePageFactory(CompiledPageActionDescriptor actionDescriptor)
    {
        if (!typeof(PageBase).GetTypeInfo().IsAssignableFrom(actionDescriptor.PageTypeInfo))
        {
            throw new InvalidOperationException(Resources.FormatActivatedInstance_MustBeAnInstanceOf(
                _pageActivator.GetType().FullName,
                typeof(PageBase).FullName));
        }

        var activatorFactory = _pageActivator.CreateActivator(actionDescriptor);
        var declaredModelType = actionDescriptor.DeclaredModelTypeInfo?.AsType() ?? actionDescriptor.PageTypeInfo.AsType();
        var propertyActivator = new RazorPagePropertyActivator(
            actionDescriptor.PageTypeInfo.AsType(),
            declaredModelType,
            _modelMetadataProvider,
            _propertyAccessors);

        return (pageContext, viewContext) =>
        {
            var page = (PageBase)activatorFactory(pageContext, viewContext);
            page.PageContext = pageContext;
            page.Path = pageContext.ActionDescriptor.RelativePath;
            page.ViewContext = viewContext;
            propertyActivator.Activate(page, viewContext);
            return page;
        };
    }

    public Action<PageContext, ViewContext, object>? CreatePageDisposer(CompiledPageActionDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return _pageActivator.CreateReleaser(descriptor);
    }

    public Func<PageContext, ViewContext, object, ValueTask>? CreateAsyncPageDisposer(CompiledPageActionDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);

        return _pageActivator.CreateAsyncReleaser(descriptor);
    }
}
