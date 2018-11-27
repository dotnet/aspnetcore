// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal static class ImplementsDirective
    {
        public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
            "implements",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.AddTypeToken(Resources.ImplementsDirective_TypeToken_Name, Resources.ImplementsDirective_TypeToken_Description);
                builder.Usage = DirectiveUsage.FileScopedMultipleOccurring;
                builder.Description = Resources.ImplementsDirective_Description;
            });

        public static void Register(RazorProjectEngineBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.AddDirective(Directive);
            builder.Features.Add(new ImplementsDirectivePass());
        }
    }
}