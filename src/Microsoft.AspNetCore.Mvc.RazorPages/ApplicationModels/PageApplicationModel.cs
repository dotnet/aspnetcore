// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    /// <summary>
    /// Application model component for RazorPages.
    /// </summary>
    public class PageApplicationModel
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PageApplicationModel"/>.
        /// </summary>
        public PageApplicationModel(
            PageActionDescriptor actionDescriptor,
            TypeInfo handlerType,
            IReadOnlyList<object> handlerAttributes)
        {
            ActionDescriptor = actionDescriptor ?? throw new ArgumentNullException(nameof(actionDescriptor));
            HandlerType = handlerType;

            Filters = new List<IFilterMetadata>();
            Properties = new CopyOnWriteDictionary<object, object>(
                actionDescriptor.Properties, 
                EqualityComparer<object>.Default);
            HandlerMethods = new List<PageHandlerModel>();
            HandlerProperties = new List<PagePropertyModel>();
            HandlerTypeAttributes = handlerAttributes;
        }

        /// <summary>
        /// A copy constructor for <see cref="PageApplicationModel"/>.
        /// </summary>
        /// <param name="other">The <see cref="PageApplicationModel"/> to copy from.</param>
        public PageApplicationModel(PageApplicationModel other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            ActionDescriptor = other.ActionDescriptor;
            HandlerType = other.HandlerType;
            PageType = other.PageType;
            ModelType = other.ModelType;

            Filters = new List<IFilterMetadata>(other.Filters);
            Properties = new Dictionary<object, object>(other.Properties);

            HandlerMethods = new List<PageHandlerModel>(other.HandlerMethods.Select(m => new PageHandlerModel(m)));
            HandlerProperties  = new List<PagePropertyModel>(other.HandlerProperties.Select(p => new PagePropertyModel(p)));
            HandlerTypeAttributes = other.HandlerTypeAttributes;
        }

        /// <summary>
        /// Gets the <see cref="PageActionDescriptor"/>.
        /// </summary>
        public PageActionDescriptor ActionDescriptor { get; }

        /// <summary>
        /// Gets the application root relative path for the page.
        /// </summary>
        public string RelativePath => ActionDescriptor.RelativePath;

        /// <summary>
        /// Gets the path relative to the base path for page discovery.
        /// </summary>
        public string ViewEnginePath => ActionDescriptor.ViewEnginePath;

        /// <summary>
        /// Gets the route template for the page.
        /// </summary>
        public string RouteTemplate => ActionDescriptor.AttributeRouteInfo?.Template;

        /// <summary>
        /// Gets the applicable <see cref="IFilterMetadata"/> instances.
        /// </summary>
        public IList<IFilterMetadata> Filters { get; }

        /// <summary>
        /// Stores arbitrary metadata properties associated with the <see cref="PageApplicationModel"/>.
        /// </summary>
        public IDictionary<object, object> Properties { get; }

        /// <summary>
        /// Gets or sets the <see cref="TypeInfo"/> of the Razor page.
        /// </summary>
        public TypeInfo PageType { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="TypeInfo"/> of the Razor page model.
        /// </summary>
        public TypeInfo ModelType { get; set; }

        /// <summary>
        /// Gets the <see cref="TypeInfo"/> of the handler.
        /// </summary>
        public TypeInfo HandlerType { get; }

        /// <summary>
        /// Gets the sequence of attributes declared on <see cref="HandlerType"/>.
        /// </summary>
        public IReadOnlyList<object> HandlerTypeAttributes { get; }

        /// <summary>
        /// Gets the sequence of <see cref="PageHandlerModel"/> instances.
        /// </summary>
        public IList<PageHandlerModel> HandlerMethods { get; }

        /// <summary>
        /// Gets the sequence of <see cref="PagePropertyModel"/> instances on <see cref="PageHandlerModel"/>.
        /// </summary>
        public IList<PagePropertyModel> HandlerProperties { get; }
    }
}