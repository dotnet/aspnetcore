// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal static class ComponentPreserveWhitespaceDirective
{
    public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
        "preservewhitespace",
        DirectiveKind.SingleLine,
        builder =>
        {
            builder.AddBooleanToken(ComponentResources.PreserveWhitespaceDirective_BooleanToken_Name, ComponentResources.PreserveWhitespaceDirective_BooleanToken_Description);
            builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
            builder.Description = ComponentResources.PreserveWhitespaceDirective_Description;
        });

    public static void Register(RazorProjectEngineBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddDirective(Directive, FileKinds.Component, FileKinds.ComponentImport);
    }
}
