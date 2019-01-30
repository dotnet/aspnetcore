// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure
{
    internal class DefaultPageModelFactoryProvider : IPageModelFactoryProvider
    {
        private static readonly Func<PropertyInfo, PropertyActivator<PageContext>> _createActivateInfo =
            CreateActivateInfo;
        private readonly IPageModelActivatorProvider _modelActivator;

        public DefaultPageModelFactoryProvider(IPageModelActivatorProvider modelActivator)
        {
            _modelActivator = modelActivator;
        }

        public Func<PageContext, object> CreateModelFactory(CompiledPageActionDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (descriptor.ModelTypeInfo == null)
            {
                return null;
            }

            var modelActivator = _modelActivator.CreateActivator(descriptor);
            var propertyActivator = PropertyActivator<PageContext>.GetPropertiesToActivate(
                    descriptor.ModelTypeInfo.AsType(),
                    typeof(PageContextAttribute),
                    _createActivateInfo,
                    includeNonPublic: false);

            return pageContext =>
            {
                var model = modelActivator(pageContext);
                for (var i = 0; i < propertyActivator.Length; i++)
                {
                    propertyActivator[i].Activate(model, pageContext);
                }

                return model;
            };
        }

        public Action<PageContext, object> CreateModelDisposer(CompiledPageActionDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (descriptor.ModelTypeInfo == null)
            {
                return null;
            }

            return _modelActivator.CreateReleaser(descriptor);
        }

        private static PropertyActivator<PageContext> CreateActivateInfo(PropertyInfo property) =>
            new PropertyActivator<PageContext>(property, pageContext => pageContext);
    }
}