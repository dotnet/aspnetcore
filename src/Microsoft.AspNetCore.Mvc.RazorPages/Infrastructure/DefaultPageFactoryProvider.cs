// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    public class DefaultPageFactoryProvider : IPageFactoryProvider
    {
        private readonly IPageActivatorProvider _pageActivator;
        private readonly IModelMetadataProvider _modelMetadataProvider;
        private readonly RazorPagePropertyActivator.PropertyValueAccessors _propertyAccessors;

        public DefaultPageFactoryProvider(
            IPageActivatorProvider pageActivator,
            IModelMetadataProvider metadataProvider,
            IUrlHelperFactory urlHelperFactory,
            IJsonHelper jsonHelper,
            DiagnosticSource diagnosticSource,
            HtmlEncoder htmlEncoder,
            IModelExpressionProvider modelExpressionProvider)
        {
            _pageActivator = pageActivator;
            _modelMetadataProvider = metadataProvider;
            _propertyAccessors = new RazorPagePropertyActivator.PropertyValueAccessors
            {
                UrlHelperAccessor = context => urlHelperFactory.GetUrlHelper(context),
                JsonHelperAccessor = context => jsonHelper,
                DiagnosticSourceAccessor = context => diagnosticSource,
                HtmlEncoderAccessor = context => htmlEncoder,
                ModelExpressionProviderAccessor = context => modelExpressionProvider,
            };
        }

        public virtual Func<PageContext, ViewContext, object> CreatePageFactory(CompiledPageActionDescriptor actionDescriptor)
        {
            if (!typeof(PageBase).GetTypeInfo().IsAssignableFrom(actionDescriptor.PageTypeInfo))
            {
                throw new InvalidOperationException(Resources.FormatActivatedInstance_MustBeAnInstanceOf(
                    _pageActivator.GetType().FullName,
                    typeof(PageBase).FullName));
            }

            var activatorFactory = _pageActivator.CreateActivator(actionDescriptor);
            var modelType = actionDescriptor.ModelTypeInfo?.AsType() ?? actionDescriptor.PageTypeInfo.AsType();
            var propertyActivator = new RazorPagePropertyActivator(
                actionDescriptor.PageTypeInfo.AsType(),
                modelType,
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

        public virtual Action<PageContext, ViewContext, object> CreatePageDisposer(CompiledPageActionDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return _pageActivator.CreateReleaser(descriptor);
        }
    }
}