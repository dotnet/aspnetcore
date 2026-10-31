// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <inheritdoc />
public class RazorPageActivator : IRazorPageActivator
{
    // Name of the "public TModel Model" property on RazorPage<TModel>
    private const string ModelPropertyName = "Model";
    private readonly ConcurrentDictionary<CacheKey, RazorPagePropertyActivator> _activationInfo;
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
        _activationInfo = new ConcurrentDictionary<CacheKey, RazorPagePropertyActivator>();
        _metadataProvider = metadataProvider;

        _propertyAccessors = new RazorPagePropertyActivator.PropertyValueAccessors
        {
            UrlHelperAccessor = urlHelperFactory.GetUrlHelper,
            JsonHelperAccessor = context => jsonHelper,
            DiagnosticSourceAccessor = context => diagnosticSource,
            HtmlEncoderAccessor = context => htmlEncoder,
            ModelExpressionProviderAccessor = context => modelExpressionProvider,
        };
    }

    internal void ClearCache()
    {
        _activationInfo.Clear();
    }

    internal ConcurrentDictionary<CacheKey, RazorPagePropertyActivator> ActivationInfo => _activationInfo;

    /// <inheritdoc />
    public void Activate(IRazorPage page, ViewContext context)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(context);

        var propertyActivator = GetOrAddCacheEntry(page);
        propertyActivator.Activate(page, context);
    }

    internal RazorPagePropertyActivator GetOrAddCacheEntry(IRazorPage page)
    {
        var pageType = page.GetType();
        Type? providedModelType = null;
        if (page is IModelTypeProvider modelTypeProvider)
        {
            providedModelType = modelTypeProvider.GetModelType();
        }

        // We only need to vary by providedModelType since it varies at runtime. Defined model type
        // is synonymous with the pageType and consequently does not need to be accounted for in the cache key.
        var cacheKey = new CacheKey(pageType, providedModelType);
        if (!_activationInfo.TryGetValue(cacheKey, out var propertyActivator))
        {
            // Look for a property named "Model". If it is non-null, we'll assume this is
            // the equivalent of TModel Model property on RazorPage<TModel>.
            //
            // Otherwise if we don't have a model property the activator will just skip setting
            // the view data.
            var modelType = providedModelType;
            if (modelType == null)
            {
                modelType = pageType.GetRuntimeProperty(ModelPropertyName)?.PropertyType;
            }

            propertyActivator = new RazorPagePropertyActivator(
                pageType,
                modelType,
                _metadataProvider,
                _propertyAccessors);

            propertyActivator = _activationInfo.GetOrAdd(cacheKey, propertyActivator);
        }

        return propertyActivator;
    }

    internal readonly struct CacheKey : IEquatable<CacheKey>
    {
        public CacheKey(Type pageType, Type? providedModelType)
        {
            PageType = pageType;
            ProvidedModelType = providedModelType;
        }

        public Type PageType { get; }

        public Type? ProvidedModelType { get; }

        public bool Equals(CacheKey other)
        {
            return PageType == other.PageType &&
                ProvidedModelType == other.ProvidedModelType;
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(PageType);
            if (ProvidedModelType != null)
            {
                hashCode.Add(ProvidedModelType);
            }

            return hashCode.ToHashCode();
        }
    }
}
