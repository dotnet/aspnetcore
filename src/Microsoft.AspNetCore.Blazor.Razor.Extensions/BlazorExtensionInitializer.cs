// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    public class BlazorExtensionInitializer : RazorExtensionInitializer
    {
        public static readonly RazorConfiguration DeclarationConfiguration;

        public static readonly RazorConfiguration DefaultConfiguration;

        static BlazorExtensionInitializer()
        {
            // RazorConfiguration is changing between 15.7 and preview2 builds of Razor, this is a reflection-based
            // workaround.
            DeclarationConfiguration = Create("BlazorDeclaration-0.1");
            DefaultConfiguration = Create("Blazor-0.1");

            RazorConfiguration Create(string configurationName)
            {
                var args = new object[] { RazorLanguageVersion.Version_2_1, configurationName, Array.Empty<RazorExtension>(), };

                MethodInfo method;
                ConstructorInfo constructor;
                if ((method = typeof(RazorConfiguration).GetMethod("Create", BindingFlags.Public | BindingFlags.Static)) != null)
                {
                    return (RazorConfiguration)method.Invoke(null, args);
                }
                else if ((constructor = typeof(RazorConfiguration).GetConstructors().FirstOrDefault()) != null)
                {
                    return (RazorConfiguration)constructor.Invoke(args);
                }
                else
                {
                    throw new InvalidOperationException("Can't create a configuration. This is bad.");
                }
            }
        }

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

            builder.Features.Remove(builder.Features.OfType<IImportProjectFeature>().Single());
            builder.Features.Add(new BlazorImportProjectFeature());

            var index = builder.Phases.IndexOf(builder.Phases.OfType<IRazorCSharpLoweringPhase>().Single());
            builder.Phases[index] = new BlazorRazorCSharpLoweringPhase();

            builder.Features.Add(new ConfigureBlazorCodeGenerationOptions());

            builder.Features.Add(new ComponentDocumentClassifierPass());
            builder.Features.Add(new ComplexAttributeContentPass());
            builder.Features.Add(new ComponentLoweringPass());

            builder.Features.Add(new ComponentTagHelperDescriptorProvider());

            if (builder.Configuration.ConfigurationName == DeclarationConfiguration.ConfigurationName)
            {
                // This is for 'declaration only' processing. We don't want to try and emit any method bodies during
                // the design time build because we can't do it correctly until the set of components is known.
                builder.Features.Add(new EliminateMethodBodyPass());
            }
        }

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
