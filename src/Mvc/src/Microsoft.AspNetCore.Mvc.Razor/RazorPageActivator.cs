// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    /// <inheritdoc />
    public class RazorPageActivator : IRazorPageActivator
    {
        // Name of the "public TModel Model" property on RazorPage<TModel>
        private const string ModelPropertyName = "Model";
        private readonly ConcurrentDictionary<Type, RazorPagePropertyActivator> _activationInfo;
        private readonly IModelMetadataProvider _metadataProvider;

        // Value accessors for common singleton properties activated in a RazorPage.
        private readonly RazorPagePropertyActivator.PropertyValueAccessors _propertyAccessors;

        /// <summary>
        /// Initializes a new instance of the <see cref="RazorPageActivator"/> class.
        /// </summary>
        public RazorPageActivator(
            IModelMetadataProvider metadataProvider,
            IUrlHelperFactory urlHelperFactory,
            IJsonHelper jsonHelper,
            DiagnosticSource diagnosticSource,
            HtmlEncoder htmlEncoder,
            IModelExpressionProvider modelExpressionProvider)
        {
            _activationInfo = new ConcurrentDictionary<Type, RazorPagePropertyActivator>();
            _metadataProvider = metadataProvider;

            _propertyAccessors = new RazorPagePropertyActivator.PropertyValueAccessors
            {
                UrlHelperAccessor = context => urlHelperFactory.GetUrlHelper(context),
                JsonHelperAccessor = context => jsonHelper,
                DiagnosticSourceAccessor = context => diagnosticSource,
                HtmlEncoderAccessor = context => htmlEncoder,
                ModelExpressionProviderAccessor = context => modelExpressionProvider,
            };
        }

        /// <inheritdoc />
        public void Activate(IRazorPage page, ViewContext context)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var pageType = page.GetType();
            RazorPagePropertyActivator propertyActivator;
            if (!_activationInfo.TryGetValue(pageType, out propertyActivator))
            {
                // Look for a property named "Model". If it is non-null, we'll assume this is
                // the equivalent of TModel Model property on RazorPage<TModel>.
                //
                // Otherwise if we don't have a model property the activator will just skip setting
                // the view data.
                var modelType = pageType.GetRuntimeProperty(ModelPropertyName)?.PropertyType;
                propertyActivator = new RazorPagePropertyActivator(
                    pageType,
                    modelType,
                    _metadataProvider,
                    _propertyAccessors);

                propertyActivator = _activationInfo.GetOrAdd(pageType, propertyActivator);
            }

            propertyActivator.Activate(page, context);
        }
    }
}