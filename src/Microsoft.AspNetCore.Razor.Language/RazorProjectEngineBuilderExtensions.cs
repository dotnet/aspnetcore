// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class RazorProjectEngineBuilderExtensions
    {
        public static void SetImportFeature(this RazorProjectEngineBuilder builder, IRazorImportFeature feature)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (feature == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // Remove any existing import features in favor of the new one we're given.
            var existingFeatures = builder.Features.OfType<IRazorImportFeature>().ToArray();
            foreach (var existingFeature in existingFeatures)
            {
                builder.Features.Remove(existingFeature);
            }

            builder.Features.Add(feature);
        }
    }
}
