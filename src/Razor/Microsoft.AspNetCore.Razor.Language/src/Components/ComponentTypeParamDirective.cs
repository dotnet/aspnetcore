// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    internal class ComponentTypeParamDirective
    {
        public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
            "typeparam",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.AddMemberToken(ComponentResources.TypeParamDirective_Token_Name, ComponentResources.TypeParamDirective_Token_Description);
                builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                builder.Description = ComponentResources.TypeParamDirective_Description;
            });

        public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddDirective(Directive, FileKinds.Component, FileKinds.ComponentImport);
            return builder;
        }
    }
}
