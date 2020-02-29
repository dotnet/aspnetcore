// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
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
}