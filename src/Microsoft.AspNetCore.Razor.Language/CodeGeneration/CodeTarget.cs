// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public abstract class CodeTarget
    {
        public static CodeTarget CreateDefault(RazorCodeDocument codeDocument, RazorCodeGenerationOptions options)
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

        public static CodeTarget CreateDefault(
            RazorCodeDocument codeDocument,
            RazorCodeGenerationOptions options,
            Action<ICodeTargetBuilder> configure)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var builder = new DefaultCodeTargetBuilder(codeDocument, options);

            if (builder.Options.DesignTime)
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

        public static CodeTarget CreateEmpty(
            RazorCodeDocument codeDocument,
            RazorCodeGenerationOptions options, 
            Action<ICodeTargetBuilder> configure)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var builder = new DefaultCodeTargetBuilder(codeDocument, options);
            configure?.Invoke(builder);
            return builder.Build();
        }

        internal static void AddDesignTimeDefaults(ICodeTargetBuilder builder)
        {

        }

        internal static void AddRuntimeDefaults(ICodeTargetBuilder builder)
        {

        }

        public abstract IntermediateNodeWriter CreateNodeWriter();

        public abstract TExtension GetExtension<TExtension>() where TExtension : class, ICodeTargetExtension;

        public abstract bool HasExtension<TExtension>() where TExtension : class, ICodeTargetExtension;
    }
}
