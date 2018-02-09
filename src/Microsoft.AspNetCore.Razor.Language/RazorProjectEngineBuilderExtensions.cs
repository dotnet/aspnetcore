// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class RazorProjectEngineBuilderExtensions
    {
        /// <summary>
        /// Registers a class configuration delegate that gets invoked during code generation.
        /// </summary>
        /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
        /// <param name="configureClass"><see cref="Action"/> invoked to configure 
        /// <see cref="ClassDeclarationIntermediateNode"/> during code generation.</param>
        /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
        public static RazorProjectEngineBuilder ConfigureClass(
            this RazorProjectEngineBuilder builder,
            Action<RazorCodeDocument, ClassDeclarationIntermediateNode> configureClass)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureClass == null)
            {
                throw new ArgumentNullException(nameof(configureClass));
            }

            var configurationFeature = GetDefaultDocumentClassifierPassFeature(builder);
            configurationFeature.ConfigureClass.Add(configureClass);
            return builder;
        }

        /// <summary>
        /// Sets the base type for generated types.
        /// </summary>
        /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
        /// <param name="baseType">The name of the base type.</param>
        /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
        public static RazorProjectEngineBuilder SetBaseType(this RazorProjectEngineBuilder builder, string baseType)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var configurationFeature = GetDefaultDocumentClassifierPassFeature(builder);
            configurationFeature.ConfigureClass.Add((document, @class) => @class.BaseType = baseType);
            return builder;
        }

        /// <summary>
        /// Sets the namespace for generated types.
        /// </summary>
        /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
        /// <param name="namespaceName">The name of the namespace.</param>
        /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
        public static RazorProjectEngineBuilder SetNamespace(this RazorProjectEngineBuilder builder, string namespaceName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var configurationFeature = GetDefaultDocumentClassifierPassFeature(builder);
            configurationFeature.ConfigureNamespace.Add((document, @namespace) => @namespace.Content = namespaceName);
            return builder;
        }

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

        /// <summary>
        /// Adds the provided <see cref="RazorProjectItem" />s as imports to all project items processed
        /// by the <see cref="RazorProjectEngine"/>.
        /// </summary>
        /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
        /// <param name="imports">The collection of imports.</param>
        /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
        public static RazorProjectEngineBuilder AddDefaultImports(this RazorProjectEngineBuilder builder, params RazorProjectItem[] imports)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var existingImportFeature = builder.Features.OfType<IImportProjectFeature>().First();
            var testImportFeature = new AdditionalImportsProjectFeature(existingImportFeature, imports);
            builder.SetImportFeature(testImportFeature);

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

        private static DefaultDocumentClassifierPassFeature GetDefaultDocumentClassifierPassFeature(RazorProjectEngineBuilder builder)
        {
            var configurationFeature = builder.Features.OfType<DefaultDocumentClassifierPassFeature>().FirstOrDefault();
            if (configurationFeature == null)
            {
                configurationFeature = new DefaultDocumentClassifierPassFeature();
                builder.Features.Add(configurationFeature);
            }

            return configurationFeature;
        }

        private class AdditionalImportsProjectFeature : RazorProjectEngineFeatureBase, IImportProjectFeature
        {
            private readonly IImportProjectFeature _existingImportFeature;
            private readonly RazorProjectItem[] _imports;

            public override RazorProjectEngine ProjectEngine
            {
                get => base.ProjectEngine;
                set
                {
                    _existingImportFeature.ProjectEngine = value;
                    base.ProjectEngine = value;
                }
            }

            public AdditionalImportsProjectFeature(IImportProjectFeature existingImportFeature, params RazorProjectItem[] imports)
            {
                _existingImportFeature = existingImportFeature;
                _imports = imports;
            }

            public IReadOnlyList<RazorProjectItem> GetImports(RazorProjectItem projectItem)
            {
                var imports = _existingImportFeature.GetImports(projectItem).ToList();
                imports.AddRange(_imports);

                return imports;
            }
        }
    }
}
