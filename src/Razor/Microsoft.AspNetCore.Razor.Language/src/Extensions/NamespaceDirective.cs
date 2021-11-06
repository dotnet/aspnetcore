// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

public static class NamespaceDirective
{
    public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
        "namespace",
        DirectiveKind.SingleLine,
        builder =>
        {
            builder.AddNamespaceToken(
                Resources.NamespaceDirective_NamespaceToken_Name,
                Resources.NamespaceDirective_NamespaceToken_Description);
            builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
            builder.Description = Resources.NamespaceDirective_Description;
        });

    public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddDirective(Directive, FileKinds.Legacy, FileKinds.Component, FileKinds.ComponentImport);
        return builder;
    }
}
