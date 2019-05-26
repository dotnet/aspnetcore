// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class RequiredAttributeDescriptorBuilderExtensions
    {
        internal static bool IsDirectiveAttribute(this RequiredAttributeDescriptorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return
                builder.Metadata.TryGetValue(ComponentMetadata.Common.DirectiveAttribute, out var value) &&
                string.Equals(bool.TrueString, value);
        }
    }
}
