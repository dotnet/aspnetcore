// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language;

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

    /// <summary>
    /// Sets the root namespace for the generated code.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <param name="rootNamespace">The root namespace.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public static RazorProjectEngineBuilder SetRootNamespace(this RazorProjectEngineBuilder builder, string rootNamespace)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Features.Add(new ConfigureRootNamespaceFeature(rootNamespace));
        return builder;
    }

    /// <summary>
    /// Sets the SupportLocalizedComponentNames property to make localized component name diagnostics available.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public static RazorProjectEngineBuilder SetSupportLocalizedComponentNames(this RazorProjectEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Features.Add(new SetSupportLocalizedComponentNamesFeature());
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
    /// Adds the specified <see cref="DirectiveDescriptor"/> for the provided file kind.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <param name="directive">The <see cref="DirectiveDescriptor"/> to add.</param>
    /// <param name="fileKinds">The file kinds, for which to register the directive. See <see cref="FileKinds"/>.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public static RazorProjectEngineBuilder AddDirective(this RazorProjectEngineBuilder builder, DirectiveDescriptor directive, params string[] fileKinds)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (directive == null)
        {
            throw new ArgumentNullException(nameof(directive));
        }

        if (fileKinds == null)
        {
            throw new ArgumentNullException(nameof(fileKinds));
        }

        var directiveFeature = GetDirectiveFeature(builder);

        foreach (var fileKind in fileKinds)
        {
            if (!directiveFeature.DirectivesByFileKind.TryGetValue(fileKind, out var directives))
            {
                directives = new List<DirectiveDescriptor>();
                directiveFeature.DirectivesByFileKind.Add(fileKind, directives);
            }

            directives.Add(directive);
        }

        return builder;
    }

    /// <summary>
    /// Adds the provided <see cref="RazorProjectItem" />s as imports to all project items processed
    /// by the <see cref="RazorProjectEngine"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
    /// <param name="imports">The collection of imports.</param>
    /// <returns>The <see cref="RazorProjectEngineBuilder"/>.</returns>
    public static RazorProjectEngineBuilder AddDefaultImports(this RazorProjectEngineBuilder builder, params string[] imports)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Features.Add(new AdditionalImportsProjectFeature(imports));

        return builder;
    }

    private static DefaultRazorDirectiveFeature GetDirectiveFeature(RazorProjectEngineBuilder builder)
    {
        var directiveFeature = builder.Features.OfType<DefaultRazorDirectiveFeature>().FirstOrDefault();
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
        private readonly IReadOnlyList<RazorProjectItem> _imports;

        public AdditionalImportsProjectFeature(params string[] imports)
        {
            _imports = imports.Select(import => new InMemoryProjectItem(import)).ToArray();
        }

        public IReadOnlyList<RazorProjectItem> GetImports(RazorProjectItem projectItem)
        {
            return _imports;
        }

        private class InMemoryProjectItem : RazorProjectItem
        {
            private readonly byte[] _importBytes;

            public InMemoryProjectItem(string content)
            {
                if (string.IsNullOrEmpty(content))
                {
                    throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, nameof(content));
                }

                var preamble = Encoding.UTF8.GetPreamble();
                var contentBytes = Encoding.UTF8.GetBytes(content);

                _importBytes = new byte[preamble.Length + contentBytes.Length];
                preamble.CopyTo(_importBytes, 0);
                contentBytes.CopyTo(_importBytes, preamble.Length);
            }

            public override string BasePath => null;

            public override string FilePath => null;

            public override string PhysicalPath => null;

            public override bool Exists => true;

            public override Stream Read() => new MemoryStream(_importBytes);
        }
    }

    private class SetSupportLocalizedComponentNamesFeature : RazorEngineFeatureBase, IConfigureRazorCodeGenerationOptionsFeature
    {
        public int Order { get; set; }

        public void Configure(RazorCodeGenerationOptionsBuilder options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.SupportLocalizedComponentNames = true;
        }
    }

    private class ConfigureRootNamespaceFeature : IConfigureRazorCodeGenerationOptionsFeature
    {
        private readonly string _rootNamespace;

        public ConfigureRootNamespaceFeature(string rootNamespace)
        {
            _rootNamespace = rootNamespace;
        }

        public int Order { get; set; }

        public RazorEngine Engine { get; set; }

        public void Configure(RazorCodeGenerationOptionsBuilder options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.RootNamespace = _rootNamespace;
        }
    }
}
