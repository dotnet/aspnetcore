// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal static class LayoutDirective
    {
        public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
            "layout",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.AddTypeToken(Resources.LayoutDirective_TypeToken_Name, Resources.LayoutDirective_TypeToken_Description);
                builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
                builder.Description = Resources.LayoutDirective_Description;
            });

        public static void Register(RazorProjectEngineBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddDirective(Directive);
            builder.Features.Add(new LayoutDirectivePass());
        }

        public static void Register(IRazorEngineBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddDirective(Directive);
            builder.Features.Add(new LayoutDirectivePass());
        }
    }
}