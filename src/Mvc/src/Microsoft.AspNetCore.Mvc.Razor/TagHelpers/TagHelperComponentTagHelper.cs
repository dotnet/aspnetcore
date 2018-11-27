// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers
{
    /// <summary>
    /// Initializes and processes the <see cref="ITagHelperComponent"/>s added to the 
    /// <see cref="ITagHelperComponentManager.Components"/> in the specified order.
    /// </summary>
    public abstract class TagHelperComponentTagHelper : TagHelper
    {
        private readonly ILogger _logger;
        private readonly IEnumerable<ITagHelperComponent> _components;

        /// <summary>
        /// Creates a new <see cref="TagHelperComponentTagHelper"/> and orders the 
        /// the collection of <see cref="ITagHelperComponent"/>s in <see cref="ITagHelperComponentManager.Components"/>.
        /// </summary>
        /// <param name="manager">The <see cref="ITagHelperComponentManager"/> which contains the collection
        /// of <see cref="ITagHelperComponent"/>s.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        /// <remarks>The <see cref="ITagHelperComponentManager.Components"/> are ordered after the 
        /// creation of the <see cref="ITagHelperComponentManager"/> to position the <see cref="ITagHelperComponent"/>s
        /// added from controllers and views correctly.</remarks>
        public TagHelperComponentTagHelper(
            ITagHelperComponentManager manager,
            ILoggerFactory loggerFactory)
        {
            if (manager == null)
            {
                throw new ArgumentNullException(nameof(manager));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _components = manager.Components.OrderBy(p => p.Order).ToArray();
            _logger = loggerFactory.CreateLogger(GetType());
        }

        /// <summary>
        /// Activates the <see cref="ViewContext"/> property of all the <see cref="ITagHelperComponentManager.Components"/>.
        /// </summary>
        [HtmlAttributeNotBound]
        public ITagHelperComponentPropertyActivator PropertyActivator { get; set; }

        [ViewContext]
        [HtmlAttributeNotBound]
        public ViewContext ViewContext { get; set; }

        /// <inheritdoc />
        public override void Init(TagHelperContext context)
        {
            if (PropertyActivator == null)
            {
                var serviceProvider = ViewContext.HttpContext.RequestServices;
                PropertyActivator = serviceProvider.GetRequiredService<ITagHelperComponentPropertyActivator>();
            }

            foreach (var component in _components)
            {
                PropertyActivator.Activate(ViewContext, component);
                component.Init(context);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.TagHelperComponentInitialized(component.GetType().FullName);
                }
            }
        }

        /// <inheritdoc />
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            foreach (var component in _components)
            {
                await component.ProcessAsync(context, output);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.TagHelperComponentProcessed(component.GetType().FullName);
                }
            }
        }
    }
}
