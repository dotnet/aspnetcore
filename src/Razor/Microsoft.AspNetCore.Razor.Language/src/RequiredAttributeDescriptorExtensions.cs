// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language;

public static class RequiredAttributeDescriptorExtensions
{
    public static bool IsDirectiveAttribute(this RequiredAttributeDescriptor descriptor)
    {
        if (descriptor == null)
        {
            throw new ArgumentNullException(nameof(descriptor));
        }

        return
            descriptor.Metadata.TryGetValue(ComponentMetadata.Common.DirectiveAttribute, out var value) &&
            string.Equals(bool.TrueString, value);
    }
}
