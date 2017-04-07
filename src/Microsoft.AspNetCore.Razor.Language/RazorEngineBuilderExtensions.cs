// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language
{
    /// <summary>
    /// Extension methods to <see cref="IRazorEngineBuilder" />.
    /// </summary>
    public static class RazorEngineBuilderExtensions
    {
        /// <summary>
        /// Adds the specified <see cref="DirectiveDescriptor"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IRazorEngineBuilder"/>.</param>
        /// <param name="directive">The <see cref="DirectiveDescriptor"/> to add.</param>
        /// <returns>The <see cref="IRazorEngineBuilder"/>.</returns>
        public static IRazorEngineBuilder AddDirective(this IRazorEngineBuilder builder, DirectiveDescriptor directive)
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
        /// Adds the specified <see cref="IRuntimeTargetExtension"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IRazorEngineBuilder"/>.</param>
        /// <param name="extension">The <see cref="IRuntimeTargetExtension"/> to add.</param>
        /// <returns>The <see cref="IRazorEngineBuilder"/>.</returns>
        public static IRazorEngineBuilder AddTargetExtension(this IRazorEngineBuilder builder, IRuntimeTargetExtension extension)
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
        /// Sets the base type for generated types.
        /// </summary>
        /// <param name="builder">The <see cref="IRazorEngineBuilder"/>.</param>
        /// <param name="baseType">The name of the base type.</param>
        /// <returns>The <see cref="IRazorEngineBuilder"/>.</returns>
        public static IRazorEngineBuilder SetBaseType(this IRazorEngineBuilder builder, string baseType)
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
        /// Registers a class configuration delegate that gets invoked during code generation.
        /// </summary>
        /// <param name="builder">The <see cref="IRazorEngineBuilder"/>.</param>
        /// <param name="configureClass"><see cref="Action"/> invoked to configure 
        /// <see cref="ClassDeclarationIRNode"/> during code generation.</param>
        /// <returns>The <see cref="IRazorEngineBuilder"/>.</returns>
        public static IRazorEngineBuilder ConfigureClass(
            this IRazorEngineBuilder builder, 
            Action<RazorCodeDocument, ClassDeclarationIRNode> configureClass)
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
        /// Sets the namespace for generated types.
        /// </summary>
        /// <param name="builder">The <see cref="IRazorEngineBuilder"/>.</param>
        /// <param name="namespaceName">The name of the namespace.</param>
        /// <returns>The <see cref="IRazorEngineBuilder"/>.</returns>
        public static IRazorEngineBuilder SetNamespace(this IRazorEngineBuilder builder, string namespaceName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var configurationFeature = GetDefaultDocumentClassifierPassFeature(builder);
            configurationFeature.ConfigureNamespace.Add((document, @namespace) => @namespace.Content = namespaceName);
            return builder;
        }

        private static IRazorDirectiveFeature GetDirectiveFeature(IRazorEngineBuilder builder)
        {
            var directiveFeature = builder.Features.OfType<IRazorDirectiveFeature>().FirstOrDefault();
            if (directiveFeature == null)
            {
                directiveFeature = new DefaultRazorDirectiveFeature();
                builder.Features.Add(directiveFeature);
            }

            return directiveFeature;
        }

        private static IRazorTargetExtensionFeature GetTargetExtensionFeature(IRazorEngineBuilder builder)
        {
            var targetExtensionFeature = builder.Features.OfType<IRazorTargetExtensionFeature>().FirstOrDefault();
            if (targetExtensionFeature == null)
            {
                targetExtensionFeature = new DefaultRazorTargetExtensionFeature();
                builder.Features.Add(targetExtensionFeature);
            }

            return targetExtensionFeature;
        }

        private static DefaultDocumentClassifierPassFeature GetDefaultDocumentClassifierPassFeature(IRazorEngineBuilder builder)
        {
            var configurationFeature = builder.Features.OfType<DefaultDocumentClassifierPassFeature>().FirstOrDefault();
            if (configurationFeature == null)
            {
                configurationFeature = new DefaultDocumentClassifierPassFeature();
                builder.Features.Add(configurationFeature);
            }

            return configurationFeature;
        }
    }
}
