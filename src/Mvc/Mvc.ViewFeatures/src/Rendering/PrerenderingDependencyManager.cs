// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    internal class PrerenderingDependencyManager : IPrerenderingDependencyManager
    {
        private readonly IComponentRenderer _componentRenderer;

        private IDictionary<string, object> _firstRenderedComponentParameters;
        private IHtmlContent _firstRenderedComponentContent;

        internal Type FirstRenderedComponentType { get; private set; }

        public PrerenderingDependencyManager(IComponentRenderer componentRenderer)
        {
            _componentRenderer = componentRenderer;
        }

        public async Task<IHtmlContent> GetOrRenderContentAsync(ViewContext viewContext)
        {
            if (FirstRenderedComponentType is null)
            {
                throw new InvalidOperationException("No prerendering component dependency was specified.");
            }

            if (_firstRenderedComponentContent is null)
            {
                _firstRenderedComponentContent = await _componentRenderer.RenderComponentAsync(viewContext, FirstRenderedComponentType, RenderMode.ServerPrerendered, _firstRenderedComponentParameters);
            }

            return _firstRenderedComponentContent;
        }

        public void DependsOn<TComponent>()
        {
            DependsOn<TComponent>(new Dictionary<string, object>());
        }

        public void DependsOn<TComponent>(IDictionary<string, object> parameters)
        {
            if (FirstRenderedComponentType is not null)
            {
                throw new InvalidOperationException("Cannot depend on more than one component type for prerendering.");
            }

            FirstRenderedComponentType = typeof(TComponent);
            _firstRenderedComponentParameters = parameters;
        }
    }
}
