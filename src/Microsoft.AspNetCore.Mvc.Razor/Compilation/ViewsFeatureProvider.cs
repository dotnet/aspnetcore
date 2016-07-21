// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.Mvc.Razor.Compilation
{
    /// <summary>
    /// An <see cref="IApplicationFeatureProvider{TFeature}"/> for <see cref="ViewsFeature"/>.
    /// </summary>
    public class ViewsFeatureProvider : IApplicationFeatureProvider<ViewsFeature>
    {
        /// <inheritdoc />
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewsFeature feature)
        {
            foreach (var provider in parts.OfType<IViewsProvider>())
            {
                var precompiledViews = provider.Views;
                if (precompiledViews != null)
                {
                    foreach (var viewInfo in precompiledViews)
                    {
                        feature.Views[viewInfo.Path] = viewInfo.Type;
                    }
                }
            }
        }
    }
}
