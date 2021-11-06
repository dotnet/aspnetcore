// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract class RazorEngine
{
#pragma warning disable CS0618 // Type or member is obsolete
    private static RazorEngine CreateCore(RazorConfiguration configuration, bool designTime, Action<IRazorEngineBuilder> configure)
#pragma warning restore CS0618 // Type or member is obsolete
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var builder = new DefaultRazorEngineBuilder(designTime);
        AddDefaults(builder);

        if (designTime)
        {
            AddDefaultDesignTimeFeatures(configuration, builder.Features);
        }
        else
        {
            AddDefaultRuntimeFeatures(configuration, builder.Features);
        }

        configure?.Invoke(builder);
        return builder.Build();
    }

#pragma warning disable CS0618 // Type or member is obsolete
    private static void AddDefaults(IRazorEngineBuilder builder)
#pragma warning restore CS0618 // Type or member is obsolete
    {
        AddDefaultPhases(builder.Phases);
        AddDefaultFeatures(builder.Features);
    }

    private static void AddDefaultPhases(IList<IRazorEnginePhase> phases)
    {
        phases.Add(new DefaultRazorParsingPhase());
        phases.Add(new DefaultRazorSyntaxTreePhase());
        phases.Add(new DefaultRazorTagHelperBinderPhase());
        phases.Add(new DefaultRazorIntermediateNodeLoweringPhase());
        phases.Add(new DefaultRazorDocumentClassifierPhase());
        phases.Add(new DefaultRazorDirectiveClassifierPhase());
        phases.Add(new DefaultRazorOptimizationPhase());
        phases.Add(new DefaultRazorCSharpLoweringPhase());
    }

    private static void AddDefaultFeatures(ICollection<IRazorEngineFeature> features)
    {
        // General extensibility
        features.Add(new DefaultRazorDirectiveFeature());
        var targetExtensionFeature = new DefaultRazorTargetExtensionFeature();
        features.Add(targetExtensionFeature);
        features.Add(new DefaultMetadataIdentifierFeature());

        // Syntax Tree passes
        features.Add(new DefaultDirectiveSyntaxTreePass());
        features.Add(new HtmlNodeOptimizationPass());

        // Intermediate Node Passes
        features.Add(new DefaultDocumentClassifierPass());
        features.Add(new MetadataAttributePass());
        features.Add(new DirectiveRemovalOptimizationPass());
        features.Add(new DefaultTagHelperOptimizationPass());

        // Default Code Target Extensions
        targetExtensionFeature.TargetExtensions.Add(new MetadataAttributeTargetExtension());

        // Default configuration
        var configurationFeature = new DefaultDocumentClassifierPassFeature();
        configurationFeature.ConfigureClass.Add((document, @class) =>
        {
            @class.ClassName = "Template";
            @class.Modifiers.Add("public");
        });

        configurationFeature.ConfigureNamespace.Add((document, @namespace) =>
        {
            @namespace.Content = "Razor";
        });

        configurationFeature.ConfigureMethod.Add((document, method) =>
        {
            method.MethodName = "ExecuteAsync";
            method.ReturnType = $"global::{typeof(Task).FullName}";

            method.Modifiers.Add("public");
            method.Modifiers.Add("async");
            method.Modifiers.Add("override");
        });

        features.Add(configurationFeature);
    }

    private static void AddDefaultRuntimeFeatures(RazorConfiguration configuration, ICollection<IRazorEngineFeature> features)
    {
        // Configure options
        features.Add(new DefaultRazorParserOptionsFeature(designTime: false, version: configuration.LanguageVersion, fileKind: null));
        features.Add(new DefaultRazorCodeGenerationOptionsFeature(designTime: false));

        // Intermediate Node Passes
        features.Add(new PreallocatedTagHelperAttributeOptimizationPass());

        // Code Target Extensions
        var targetExtension = features.OfType<IRazorTargetExtensionFeature>().FirstOrDefault();
        Debug.Assert(targetExtension != null);

        targetExtension.TargetExtensions.Add(new DefaultTagHelperTargetExtension());
        targetExtension.TargetExtensions.Add(new PreallocatedAttributeTargetExtension());
    }

    private static void AddDefaultDesignTimeFeatures(RazorConfiguration configuration, ICollection<IRazorEngineFeature> features)
    {
        // Configure options
        features.Add(new DefaultRazorParserOptionsFeature(designTime: true, version: configuration.LanguageVersion, fileKind: null));
        features.Add(new DefaultRazorCodeGenerationOptionsFeature(designTime: true));
        features.Add(new SuppressChecksumOptionsFeature());

        // Intermediate Node Passes
        features.Add(new DesignTimeDirectivePass());

        // Code Target Extensions
        var targetExtension = features.OfType<IRazorTargetExtensionFeature>().FirstOrDefault();
        Debug.Assert(targetExtension != null);

        targetExtension.TargetExtensions.Add(new DefaultTagHelperTargetExtension());
        targetExtension.TargetExtensions.Add(new DesignTimeDirectiveTargetExtension());
    }

    public abstract IReadOnlyList<IRazorEngineFeature> Features { get; }

    public abstract IReadOnlyList<IRazorEnginePhase> Phases { get; }

    public abstract void Process(RazorCodeDocument document);

#nullable enable
    internal TFeature? GetFeature<TFeature>()
    {
        var count = Features.Count;
        for (var i = 0; i < count; i++)
        {
            if (Features[i] is TFeature feature)
            {
                return feature;
            }
        }

        return default;
    }
#nullable disable

    #region Obsolete
    [Obsolete("This method is obsolete and will be removed in a future version.")]
    public static RazorEngine Create()
    {
        return Create(configure: null);
    }

    [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is " + nameof(RazorProjectEngine) + "." + nameof(RazorProjectEngine.Create))]
    public static RazorEngine Create(Action<IRazorEngineBuilder> configure) => CreateCore(RazorConfiguration.Default, false, configure);

    [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is " + nameof(RazorProjectEngine) + "." + nameof(RazorProjectEngine.Create))]
    public static RazorEngine CreateDesignTime()
    {
        return CreateDesignTime(configure: null);
    }

    [Obsolete("This method is obsolete and will be removed in a future version. The recommended alternative is " + nameof(RazorProjectEngine) + "." + nameof(RazorProjectEngine.Create))]
    public static RazorEngine CreateDesignTime(Action<IRazorEngineBuilder> configure) => CreateCore(RazorConfiguration.Default, true, configure);

    [Obsolete("This method is obsolete and will be removed in a future version.")]
    public static RazorEngine CreateEmpty(Action<IRazorEngineBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new DefaultRazorEngineBuilder(designTime: false);
        configure(builder);
        return builder.Build();
    }

    [Obsolete("This method is obsolete and will be removed in a future version.")]
    public static RazorEngine CreateDesignTimeEmpty(Action<IRazorEngineBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new DefaultRazorEngineBuilder(designTime: true);
        configure(builder);
        return builder.Build();
    }
    #endregion
}

