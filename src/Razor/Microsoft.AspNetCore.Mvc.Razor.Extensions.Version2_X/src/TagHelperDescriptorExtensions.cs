// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X
{
    public static class TagHelperDescriptorExtensions
    {
        /// <summary>
        /// Indicates whether a <see cref="TagHelperDescriptor"/> represents a view component.
        /// </summary>
        /// <param name="tagHelper">The <see cref="TagHelperDescriptor"/> to check.</param>
        /// <returns>Whether a <see cref="TagHelperDescriptor"/> represents a view component.</returns>
        public static bool IsViewComponentKind(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            return string.Equals(ViewComponentTagHelperConventions.Kind, tagHelper.Kind, StringComparison.Ordinal);
        }

        public static string GetViewComponentName(this TagHelperDescriptor tagHelper)
        {
            if (tagHelper == null)
            {
                throw new ArgumentNullException(nameof(tagHelper));
            }

            tagHelper.Metadata.TryGetValue(ViewComponentTagHelperMetadata.Name, out var result);
            return result;
        }
    }
}
