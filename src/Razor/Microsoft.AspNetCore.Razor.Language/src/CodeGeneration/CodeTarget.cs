// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

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
        Action<CodeTargetBuilder> configure)
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
        Action<CodeTargetBuilder> configure)
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

    internal static void AddDesignTimeDefaults(CodeTargetBuilder builder)
    {

    }

    internal static void AddRuntimeDefaults(CodeTargetBuilder builder)
    {

    }

    public abstract IntermediateNodeWriter CreateNodeWriter();

    public abstract TExtension GetExtension<TExtension>() where TExtension : class, ICodeTargetExtension;

    public abstract bool HasExtension<TExtension>() where TExtension : class, ICodeTargetExtension;
}
