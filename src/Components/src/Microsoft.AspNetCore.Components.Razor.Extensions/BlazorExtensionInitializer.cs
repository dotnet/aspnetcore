// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Microsoft.AspNetCore.Components.Razor
{
    /// <summary>
    /// Initializes the Blazor extension.
    /// </summary>
    public class BlazorExtensionInitializer : RazorExtensionInitializer
    {
        /// <summary>
        /// Specifies the declaration configuration.
        /// </summary>
        public static readonly RazorConfiguration DeclarationConfiguration;

        /// <summary>
        /// Specifies the default configuration.
        /// </summary>
        public static readonly RazorConfiguration DefaultConfiguration;

        static BlazorExtensionInitializer()
        {
            // The configuration names here need to match what we put in the MSBuild configuration
            DeclarationConfiguration = RazorConfiguration.Create(
                RazorLanguageVersion.Experimental,
                "BlazorDeclaration-0.1",
                Array.Empty<RazorExtension>());

            DefaultConfiguration = RazorConfiguration.Create(
                RazorLanguageVersion.Experimental,
                "Blazor-0.1",
                Array.Empty<RazorExtension>());
        }

        /// <summary>
        /// Registers the Blazor extension.
        /// </summary>
        /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
        public static void Register(RazorProjectEngineBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            FunctionsDirective.Register(builder);
            ImplementsDirective.Register(builder);
            InheritsDirective.Register(builder);
            InjectDirective.Register(builder);
            LayoutDirective.Register(builder);
            PageDirective.Register(builder);
            TypeParamDirective.Register(builder);

            builder.Features.Remove(builder.Features.OfType<IImportProjectFeature>().Single());
            builder.Features.Add(new BlazorImportProjectFeature());

            var index = builder.Phases.IndexOf(builder.Phases.OfType<IRazorCSharpLoweringPhase>().Single());
            builder.Phases[index] = new BlazorRazorCSharpLoweringPhase();

            builder.Features.Add(new ConfigureBlazorCodeGenerationOptions());

            builder.AddTargetExtension(new BlazorTemplateTargetExtension());

            var isDeclarationOnlyCompile = builder.Configuration.ConfigurationName == DeclarationConfiguration.ConfigurationName;

            // Blazor-specific passes, in order.
            if (!isDeclarationOnlyCompile)
            {
                // There's no benefit in this optimization during the declaration-only compile
                builder.Features.Add(new TrimWhitespacePass());
            }
            builder.Features.Add(new ComponentDocumentClassifierPass());
            builder.Features.Add(new ComponentDocumentRewritePass());
            builder.Features.Add(new ScriptTagPass());
            builder.Features.Add(new ComplexAttributeContentPass());
            builder.Features.Add(new ComponentLoweringPass());
            builder.Features.Add(new EventHandlerLoweringPass());
            builder.Features.Add(new RefLoweringPass());
            builder.Features.Add(new BindLoweringPass());
            builder.Features.Add(new TemplateDiagnosticPass());
            builder.Features.Add(new GenericComponentPass());
            builder.Features.Add(new ChildContentDiagnosticPass());
            builder.Features.Add(new HtmlBlockPass());

            builder.Features.Add(new ComponentTagHelperDescriptorProvider());
            builder.Features.Add(new BindTagHelperDescriptorProvider());
            builder.Features.Add(new EventHandlerTagHelperDescriptorProvider());
            builder.Features.Add(new RefTagHelperDescriptorProvider());

            if (isDeclarationOnlyCompile)
            {
                // This is for 'declaration only' processing. We don't want to try and emit any method bodies during
                // the design time build because we can't do it correctly until the set of components is known.
                builder.Features.Add(new EliminateMethodBodyPass());
            }
        }

        /// <summary>
        /// Initializes the Blazor extension.
        /// </summary>
        /// <param name="builder">The <see cref="RazorProjectEngineBuilder"/>.</param>
        public override void Initialize(RazorProjectEngineBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            Register(builder);
        }

        private class ConfigureBlazorCodeGenerationOptions : IConfigureRazorCodeGenerationOptionsFeature
        {
            public int Order => 0;

            public RazorEngine Engine { get; set; }

            public void Configure(RazorCodeGenerationOptionsBuilder options)
            {
                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                // These metadata attributes require a reference to the Razor.Runtime package which we don't
                // otherwise need.
                options.SuppressMetadataAttributes = true;
            }
        }
    }
}
