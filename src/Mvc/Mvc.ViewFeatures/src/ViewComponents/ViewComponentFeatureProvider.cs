// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
{
    /// <summary>
    /// Discovers view components from a list of <see cref="ApplicationPart"/> instances.
    /// </summary>
    public class ViewComponentFeatureProvider : IApplicationFeatureProvider<ViewComponentFeature>
    {
        /// <inheritdoc />
        public void PopulateFeature(IEnumerable<ApplicationPart> parts, ViewComponentFeature feature)
        {
            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }

            if (feature == null)
            {
                throw new ArgumentNullException(nameof(feature));
            }

            foreach (var type in parts.OfType<IApplicationPartTypeProvider>().SelectMany(p => p.Types))
            {
                if (ViewComponentConventions.IsComponent(type) && ! feature.ViewComponents.Contains(type))
                {
                    feature.ViewComponents.Add(type);
                }
            }
        }
    }
}
