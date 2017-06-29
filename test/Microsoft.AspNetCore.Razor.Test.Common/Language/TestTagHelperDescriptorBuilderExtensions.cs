// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class TestTagHelperDescriptorBuilderExtensions
    {
        public static TagHelperDescriptorBuilder TypeName(this TagHelperDescriptorBuilder builder, string typeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.SetTypeName(typeName);
        }
    }
}
