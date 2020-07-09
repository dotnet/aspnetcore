// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    internal static class ComponentPreserveWhitespaceDirective
    {
        public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
            "preservewhitespace",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.AddBooleanToken(ComponentResources.PreserveWhitespaceDirective_BooleanToken_Name, ComponentResources.PreserveWhitespaceDirective_BooleanToken_Description);
                builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
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
}
