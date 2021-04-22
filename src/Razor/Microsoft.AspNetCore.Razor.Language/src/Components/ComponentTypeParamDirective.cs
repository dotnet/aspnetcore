// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    internal class ComponentTypeParamDirective
    {
        public static DirectiveDescriptor Directive = null;

        public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder, bool supportConstraints)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (Directive == null)
            {
                // Do nothing and assume the first registration wins. In real life this directive is only ever registered once.
                if (supportConstraints)
                {
                    Directive = DirectiveDescriptor.CreateDirective(
                        "typeparam",
                        DirectiveKind.SingleLine,
                        builder =>
                        {
                            builder.AddMemberToken(ComponentResources.TypeParamDirective_Token_Name, ComponentResources.TypeParamDirective_Token_Description);
                            builder.AddOptionalGenericTypeConstraintToken(ComponentResources.TypeParamDirective_Constraint_Name, ComponentResources.TypeParamDirective_Constraint_Description);
                            builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                            builder.Description = ComponentResources.TypeParamDirective_Description;
                        });
                }
                else
                {
                    Directive = DirectiveDescriptor.CreateDirective(
                        "typeparam",
                        DirectiveKind.SingleLine,
                        builder =>
                        {
                            builder.AddMemberToken(ComponentResources.TypeParamDirective_Token_Name, ComponentResources.TypeParamDirective_Token_Description);
                            builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                            builder.Description = ComponentResources.TypeParamDirective_Description;
                        });
                }
            }

            builder.AddDirective(Directive, FileKinds.Component, FileKinds.ComponentImport);
            return builder;
        }
    }
}
