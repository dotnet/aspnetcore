// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace Microsoft.AspNetCore.Mvc.ViewComponents;

/// <summary>
/// A default implementation of <see cref="IViewComponentDescriptorCollectionProvider"/>
/// </summary>
public class DefaultViewComponentDescriptorCollectionProvider : IViewComponentDescriptorCollectionProvider
{
    private readonly IViewComponentDescriptorProvider _descriptorProvider;
    private ViewComponentDescriptorCollection _viewComponents;

    /// <summary>
    /// Creates a new instance of <see cref="DefaultViewComponentDescriptorCollectionProvider"/>.
    /// </summary>
    /// <param name="descriptorProvider">The <see cref="IViewComponentDescriptorProvider"/>.</param>
    public DefaultViewComponentDescriptorCollectionProvider(IViewComponentDescriptorProvider descriptorProvider)
    {
        _descriptorProvider = descriptorProvider;
    }

    /// <inheritdoc />
    public ViewComponentDescriptorCollection ViewComponents
    {
        get
        {
            if (_viewComponents == null)
            {
                _viewComponents = GetViewComponents();
            }

            return _viewComponents;
        }
    }

    private ViewComponentDescriptorCollection GetViewComponents()
    {
        var descriptors = _descriptorProvider.GetViewComponents();
        return new ViewComponentDescriptorCollection(descriptors.ToArray(), version: 0);
    }
}
