// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class RazorProjectEngineBuilderExtensions
    {
        public static void SetImportFeature(this RazorProjectEngineBuilder builder, IImportProjectFeature feature)
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
            var existingFeatures = builder.Features.OfType<IImportProjectFeature>().ToArray();
            foreach (var existingFeature in existingFeatures)
            {
                builder.Features.Remove(existingFeature);
            }

            builder.Features.Add(feature);
        }

        /// <summary>
        /// Adds the specified <see cref="ICodeTargetExtension"/>.
        /// </summary>
        /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
        /// <param name="extension">The <see cref="ICodeTargetExtension"/> to add.</param>
        /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
        public static RazorProjectEngineBuilder AddTargetExtension(this RazorProjectEngineBuilder builder, ICodeTargetExtension extension)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            var targetExtensionFeature = GetTargetExtensionFeature(builder);
            targetExtensionFeature.TargetExtensions.Add(extension);

            return builder;
        }

        /// <summary>
        /// Adds the specified <see cref="DirectiveDescriptor"/>.
        /// </summary>
        /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
        /// <param name="directive">The <see cref="DirectiveDescriptor"/> to add.</param>
        /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
        public static RazorProjectEngineBuilder AddDirective(this RazorProjectEngineBuilder builder, DirectiveDescriptor directive)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            var directiveFeature = GetDirectiveFeature(builder);
            directiveFeature.Directives.Add(directive);

            return builder;
        }

        private static IRazorDirectiveFeature GetDirectiveFeature(RazorProjectEngineBuilder builder)
        {
            var directiveFeature = builder.Features.OfType<IRazorDirectiveFeature>().FirstOrDefault();
            if (directiveFeature == null)
            {
                directiveFeature = new DefaultRazorDirectiveFeature();
                builder.Features.Add(directiveFeature);
            }

            return directiveFeature;
        }

        private static IRazorTargetExtensionFeature GetTargetExtensionFeature(RazorProjectEngineBuilder builder)
        {
            var targetExtensionFeature = builder.Features.OfType<IRazorTargetExtensionFeature>().FirstOrDefault();
            if (targetExtensionFeature == null)
            {
                targetExtensionFeature = new DefaultRazorTargetExtensionFeature();
                builder.Features.Add(targetExtensionFeature);
            }

            return targetExtensionFeature;
        }
    }
}
