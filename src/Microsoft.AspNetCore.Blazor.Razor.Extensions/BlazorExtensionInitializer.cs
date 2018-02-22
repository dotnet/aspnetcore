// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    public class BlazorExtensionInitializer : RazorExtensionInitializer
    {
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

            builder.Features.Remove(builder.Features.OfType<IImportProjectFeature>().Single());
            builder.Features.Add(new BlazorImportProjectFeature());

            builder.Features.Add(new ConfigureBlazorCodeGenerationOptions());

            builder.Features.Add(new ComponentDocumentClassifierPass());
        }

        // This is temporarily used to initialize a RazorEngine by the build tools until we get the features
        // we need into the RazorProjectEngine (namespace).
        public static void Register(IRazorEngineBuilder builder)
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

            builder.Features.Add(new ConfigureBlazorCodeGenerationOptions());

            builder.Features.Add(new ComponentDocumentClassifierPass());
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
