// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.TagHelpers
{
    public abstract class TagHelperComponentTagHelper : TagHelper
    {
        private readonly ILogger _logger;

        private IEnumerable<ITagHelperComponent> _components;

        /// <summary>
        /// Creates a new <see cref="TagHelperComponentTagHelper"/>.
        /// </summary>
        /// <param name="components">The list of <see cref="ITagHelperComponent"/>.</param>
        /// <param name="loggerFactory">The <see cref="ILoggerFactory"/>.</param>
        public TagHelperComponentTagHelper(IEnumerable<ITagHelperComponent> components,
            ILoggerFactory loggerFactory)
        {
            if (components == null)
            {
                throw new ArgumentNullException(nameof(components));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _components = components;
            _logger = loggerFactory.CreateLogger(GetType());
        }

        /// <inheritdoc />
        public override void Init(TagHelperContext context)
        {
            _components = _components.OrderBy(p => p.Order);
            foreach (var component in _components)
            {
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
