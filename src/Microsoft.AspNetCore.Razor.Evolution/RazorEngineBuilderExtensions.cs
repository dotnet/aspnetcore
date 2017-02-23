// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution.CodeGeneration;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    public static class RazorEngineBuilderExtensions
    {
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

        public static IRazorEngineBuilder SetClassName(this IRazorEngineBuilder builder, string className)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            ConfigureClass(builder, (document, @class) => @class.Name = className);
            return builder;
        }

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
