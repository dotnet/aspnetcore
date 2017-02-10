// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    public abstract class RuntimeTarget
    {
        public static RuntimeTarget CreateDefault(RazorCodeDocument codeDocument, RazorParserOptions options)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return CreateDefault(codeDocument, options, configure: null);
        }

        public static RuntimeTarget CreateDefault(
            RazorCodeDocument codeDocument,
            RazorParserOptions options,
            Action<IRuntimeTargetBuilder> configure)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var builder = new DefaultRuntimeTargetBuilder(codeDocument, options);

            if (builder.Options.DesignTimeMode)
            {
                AddDesignTimeDefaults(builder);
            }
            else
            {
                AddRuntimeDefaults(builder);
            }

            if (configure != null)
            {
                configure.Invoke(builder);
            }

            return builder.Build();
        }

        public static RuntimeTarget CreateEmpty(
            RazorCodeDocument codeDocument,
            RazorParserOptions options, 
            Action<IRuntimeTargetBuilder> configure)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var builder = new DefaultRuntimeTargetBuilder(codeDocument, options);
            configure?.Invoke(builder);
            return builder.Build();
        }

        internal static void AddDesignTimeDefaults(IRuntimeTargetBuilder builder)
        {

        }

        internal static void AddRuntimeDefaults(IRuntimeTargetBuilder builder)
        {

        }

        internal abstract PageStructureCSharpRenderer CreateRenderer(CSharpRenderingContext context);

        public abstract TExtension GetExtension<TExtension>() where TExtension : class, IRuntimeTargetExtension;

        public abstract bool HasExtension<TExtension>() where TExtension : class, IRuntimeTargetExtension;
    }
}
