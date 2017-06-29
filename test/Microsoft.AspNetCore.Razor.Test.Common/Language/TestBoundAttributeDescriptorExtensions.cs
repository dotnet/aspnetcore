// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public static class TestBoundAttributeDescriptorBuilderExtensions
    {
        public static BoundAttributeDescriptorBuilder PropertyName(this BoundAttributeDescriptorBuilder builder, string propertyName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.SetPropertyName(propertyName);
        }
    }
}
