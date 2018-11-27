// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class TypeParamDirective
    {
        public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
            "typeparam",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.AddMemberToken(Resources.TypeParamDirective_Token_Name, Resources.TypeParamDirective_Token_Description);
                builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                builder.Description = Resources.TypeParamDirective_Description;
            });

        public static RazorProjectEngineBuilder Register(RazorProjectEngineBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddDirective(Directive);
            return builder;
        }
    }
}
