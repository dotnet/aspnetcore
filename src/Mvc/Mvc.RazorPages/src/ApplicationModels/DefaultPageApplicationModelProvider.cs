// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Options;
using Resources = Microsoft.AspNetCore.Mvc.RazorPages.Resources;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

internal sealed class DefaultPageApplicationModelProvider : IPageApplicationModelProvider
{
    private const string ModelPropertyName = "Model";
    private readonly PageHandlerPageFilter _pageHandlerPageFilter = new PageHandlerPageFilter();
    private readonly PageHandlerResultFilter _pageHandlerResultFilter = new PageHandlerResultFilter();
    private readonly IModelMetadataProvider _modelMetadataProvider;
    private readonly RazorPagesOptions _razorPagesOptions;
    private readonly IPageApplicationModelPartsProvider _pageApplicationModelPartsProvider;
    private readonly HandleOptionsRequestsPageFilter _handleOptionsRequestsFilter;

    public DefaultPageApplicationModelProvider(
        IModelMetadataProvider modelMetadataProvider,
        IOptions<RazorPagesOptions> razorPagesOptions,
        IPageApplicationModelPartsProvider pageApplicationModelPartsProvider)
    {
        _modelMetadataProvider = modelMetadataProvider;
        _razorPagesOptions = razorPagesOptions.Value;
        _pageApplicationModelPartsProvider = pageApplicationModelPartsProvider;

        _handleOptionsRequestsFilter = new HandleOptionsRequestsPageFilter();
    }

    /// <inheritdoc />
    public int Order => -1000;

    /// <inheritdoc />
    public void OnProvidersExecuting(PageApplicationModelProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.PageApplicationModel = CreateModel(context.ActionDescriptor, context.PageType);
    }

    /// <inheritdoc />
    public void OnProvidersExecuted(PageApplicationModelProviderContext context)
    {
    }

    /// <summary>
    /// Creates a <see cref="PageApplicationModel"/> for the given <paramref name="pageTypeInfo"/>.
    /// </summary>
    /// <param name="actionDescriptor">The <see cref="PageActionDescriptor"/>.</param>
    /// <param name="pageTypeInfo">The <see cref="TypeInfo"/>.</param>
    /// <returns>A <see cref="PageApplicationModel"/> for the given <see cref="TypeInfo"/>.</returns>
    private PageApplicationModel CreateModel(
        PageActionDescriptor actionDescriptor,
        TypeInfo pageTypeInfo)
    {
        ArgumentNullException.ThrowIfNull(actionDescriptor);
        ArgumentNullException.ThrowIfNull(pageTypeInfo);

        if (!typeof(PageBase).GetTypeInfo().IsAssignableFrom(pageTypeInfo))
        {
            throw new InvalidOperationException(Resources.FormatInvalidPageType_WrongBase(
                pageTypeInfo.FullName,
                typeof(PageBase).FullName));
        }

        // Pages always have a model type. If it's not set explicitly by the developer using
        // @model, it will be the same as the page type.
        var modelProperty = pageTypeInfo.GetProperty(ModelPropertyName, BindingFlags.Public | BindingFlags.Instance);
        if (modelProperty == null)
        {
            throw new InvalidOperationException(Resources.FormatInvalidPageType_NoModelProperty(
                pageTypeInfo.FullName,
                ModelPropertyName));
        }

        var modelTypeInfo = modelProperty.PropertyType.GetTypeInfo();
        var declaredModelType = modelTypeInfo;

        // Now we want figure out which type is the handler type.
        TypeInfo handlerType;
        var pageTypeAttributes = pageTypeInfo.GetCustomAttributes(inherit: true);
        object[] handlerTypeAttributes;
        if (modelProperty.PropertyType.IsDefined(typeof(PageModelAttribute), inherit: true))
        {
            handlerType = modelTypeInfo;

            // If a PageModel is specified, combine the attributes specified on the Page and the Model type.
            // Attributes that appear earlier in the are more significant. In this case, we'll treat attributes on the model (code)
            // to be more signficant than the page (code-generated).
            handlerTypeAttributes = modelTypeInfo.GetCustomAttributes(inherit: true).Concat(pageTypeAttributes).ToArray();
        }
        else
        {
            handlerType = pageTypeInfo;
            handlerTypeAttributes = pageTypeInfo.GetCustomAttributes(inherit: true);
        }

        var pageModel = new PageApplicationModel(
            actionDescriptor,
            declaredModelType,
            handlerType,
            handlerTypeAttributes)
        {
            PageType = pageTypeInfo,
            ModelType = modelTypeInfo,
        };

        PopulateHandlerMethods(pageModel);
        PopulateHandlerProperties(pageModel);
        PopulateFilters(pageModel);

        return pageModel;
    }

    // Internal for unit testing
    internal void PopulateHandlerProperties(PageApplicationModel pageModel)
    {
        var properties = PropertyHelper.GetVisibleProperties(pageModel.HandlerType.AsType());

        for (var i = 0; i < properties.Length; i++)
        {
            var propertyModel = _pageApplicationModelPartsProvider.CreatePropertyModel(properties[i].Property);
            if (propertyModel != null)
            {
                propertyModel.Page = pageModel;
                pageModel.HandlerProperties.Add(propertyModel);
            }
        }
    }

    // Internal for unit testing
    internal void PopulateHandlerMethods(PageApplicationModel pageModel)
    {
        var methods = pageModel.HandlerType.GetMethods();

        for (var i = 0; i < methods.Length; i++)
        {
            var handler = _pageApplicationModelPartsProvider.CreateHandlerModel(methods[i]);
            if (handler != null)
            {
                pageModel.HandlerMethods.Add(handler);
            }
        }
    }

    internal void PopulateFilters(PageApplicationModel pageModel)
    {
        for (var i = 0; i < pageModel.HandlerTypeAttributes.Count; i++)
        {
            if (pageModel.HandlerTypeAttributes[i] is IFilterMetadata filter)
            {
                pageModel.Filters.Add(filter);
            }
        }

        if (typeof(IAsyncPageFilter).IsAssignableFrom(pageModel.HandlerType) ||
            typeof(IPageFilter).IsAssignableFrom(pageModel.HandlerType))
        {
            pageModel.Filters.Add(_pageHandlerPageFilter);
        }

        if (typeof(IAsyncResultFilter).IsAssignableFrom(pageModel.HandlerType) ||
            typeof(IResultFilter).IsAssignableFrom(pageModel.HandlerType))
        {
            pageModel.Filters.Add(_pageHandlerResultFilter);
        }

        pageModel.Filters.Add(_handleOptionsRequestsFilter);
    }
}
