// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers
{
    /// <summary>
    /// Default implementation of <see cref="ITagHelperComponentPropertyActivator"/>.
    /// </summary>
    internal class TagHelperComponentPropertyActivator : ITagHelperComponentPropertyActivator
    {
        private readonly ConcurrentDictionary<Type, PropertyActivator<ViewContext>[]> _propertiesToActivate;
        private readonly Func<Type, PropertyActivator<ViewContext>[]> _getPropertiesToActivate = GetPropertiesToActivate;
        private static readonly Func<PropertyInfo, PropertyActivator<ViewContext>> _createActivateInfo = CreateActivateInfo;

        public TagHelperComponentPropertyActivator()
        {
            _propertiesToActivate = new ConcurrentDictionary<Type, PropertyActivator<ViewContext>[]>();
        }

        /// <inheritdoc />
        public void Activate(ViewContext context, ITagHelperComponent tagHelperComponent)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var propertiesToActivate = _propertiesToActivate.GetOrAdd(
                tagHelperComponent.GetType(),
                _getPropertiesToActivate);

            for (var i = 0; i < propertiesToActivate.Length; i++)
            {
                var activateInfo = propertiesToActivate[i];
                activateInfo.Activate(tagHelperComponent, context);
            }
        }

        private static PropertyActivator<ViewContext> CreateActivateInfo(PropertyInfo property)
        {
            return new PropertyActivator<ViewContext>(property, viewContext => viewContext);
        }

        private static PropertyActivator<ViewContext>[] GetPropertiesToActivate(Type type)
        {
            return PropertyActivator<ViewContext>.GetPropertiesToActivate(
                type,
                typeof(ViewContextAttribute),
                _createActivateInfo);
        }
    }
}
