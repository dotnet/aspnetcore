// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Reflection;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Buffers;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// Default implementation for <see cref="IViewComponentHelper"/>.
/// </summary>
public class DefaultViewComponentHelper : IViewComponentHelper, IViewContextAware
{
    private readonly IViewComponentDescriptorCollectionProvider _descriptorProvider;
    private readonly HtmlEncoder _htmlEncoder;
    private readonly IViewComponentInvokerFactory _invokerFactory;
    private readonly IViewComponentSelector _selector;
    private readonly IViewBufferScope _viewBufferScope;
    private ViewContext _viewContext = default!;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultViewComponentHelper"/>.
    /// </summary>
    /// <param name="descriptorProvider">The <see cref="IViewComponentDescriptorCollectionProvider"/>
    /// used to locate view components.</param>
    /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/>.</param>
    /// <param name="selector">The <see cref="IViewComponentSelector"/>.</param>
    /// <param name="invokerFactory">The <see cref="IViewComponentInvokerFactory"/>.</param>
    /// <param name="viewBufferScope">The <see cref="IViewBufferScope"/> that manages the lifetime of
    /// <see cref="ViewBuffer"/> instances.</param>
    public DefaultViewComponentHelper(
        IViewComponentDescriptorCollectionProvider descriptorProvider,
        HtmlEncoder htmlEncoder,
        IViewComponentSelector selector,
        IViewComponentInvokerFactory invokerFactory,
        IViewBufferScope viewBufferScope)
    {
        ArgumentNullException.ThrowIfNull(descriptorProvider);
        ArgumentNullException.ThrowIfNull(htmlEncoder);
        ArgumentNullException.ThrowIfNull(selector);
        ArgumentNullException.ThrowIfNull(invokerFactory);
        ArgumentNullException.ThrowIfNull(viewBufferScope);

        _descriptorProvider = descriptorProvider;
        _htmlEncoder = htmlEncoder;
        _selector = selector;
        _invokerFactory = invokerFactory;
        _viewBufferScope = viewBufferScope;
    }

    /// <inheritdoc />
    public void Contextualize(ViewContext viewContext)
    {
        ArgumentNullException.ThrowIfNull(viewContext);

        _viewContext = viewContext;
    }

    /// <inheritdoc />
    public Task<IHtmlContent> InvokeAsync(string name, object? arguments)
    {
        ArgumentNullException.ThrowIfNull(name);

        var descriptor = _selector.SelectComponent(name);
        if (descriptor == null)
        {
            throw new InvalidOperationException(Resources.FormatViewComponent_CannotFindComponent(
                name,
                nameof(ViewComponentAttribute),
                ViewComponentConventions.ViewComponentSuffix,
                nameof(NonViewComponentAttribute)));
        }

        return InvokeCoreAsync(descriptor, arguments);
    }

    /// <inheritdoc />
    public Task<IHtmlContent> InvokeAsync(Type componentType, object? arguments)
    {
        ArgumentNullException.ThrowIfNull(componentType);

        var descriptor = SelectComponent(componentType);
        return InvokeCoreAsync(descriptor, arguments);
    }

    private ViewComponentDescriptor SelectComponent(Type componentType)
    {
        var descriptors = _descriptorProvider.ViewComponents;
        for (var i = 0; i < descriptors.Items.Count; i++)
        {
            var descriptor = descriptors.Items[i];
            if (descriptor.TypeInfo == componentType.GetTypeInfo())
            {
                return descriptor;
            }
        }

        throw new InvalidOperationException(Resources.FormatViewComponent_CannotFindComponent(
            componentType.FullName,
            nameof(ViewComponentAttribute),
            ViewComponentConventions.ViewComponentSuffix,
            nameof(NonViewComponentAttribute)));
    }

    // Internal for testing
    internal static IDictionary<string, object?> GetArgumentDictionary(ViewComponentDescriptor descriptor, object? arguments)
    {
        if (arguments != null)
        {
            if (descriptor.Parameters.Count == 1 && descriptor.Parameters[0].ParameterType.IsAssignableFrom(arguments.GetType()))
            {
                return new Dictionary<string, object?>(capacity: 1, comparer: StringComparer.OrdinalIgnoreCase)
                    {
                        { descriptor.Parameters[0].Name!, arguments }
                    };
            }
        }

        return PropertyHelper.ObjectToDictionary(arguments);
    }

    private async Task<IHtmlContent> InvokeCoreAsync(ViewComponentDescriptor descriptor, object? arguments)
    {
        var argumentDictionary = GetArgumentDictionary(descriptor, arguments);

        var viewBuffer = new ViewBuffer(_viewBufferScope, descriptor.FullName, ViewBuffer.ViewComponentPageSize);
        using (var writer = new ViewBufferTextWriter(viewBuffer, _viewContext.Writer.Encoding))
        {
            var context = new ViewComponentContext(descriptor, argumentDictionary, _htmlEncoder, _viewContext, writer);

            var invoker = _invokerFactory.CreateInstance(context);
            if (invoker == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatViewComponent_IViewComponentFactory_ReturnedNull(descriptor.FullName));
            }

            await invoker.InvokeAsync(context);
            return viewBuffer;
        }
    }
}
