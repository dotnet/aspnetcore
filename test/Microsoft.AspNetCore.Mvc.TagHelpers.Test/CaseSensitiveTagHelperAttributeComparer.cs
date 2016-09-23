// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    public class CaseSensitiveTagHelperAttributeComparer : IEqualityComparer<TagHelperAttribute>
    {
        public readonly static CaseSensitiveTagHelperAttributeComparer Default =
            new CaseSensitiveTagHelperAttributeComparer();

        private CaseSensitiveTagHelperAttributeComparer()
        {
        }

        public bool Equals(TagHelperAttribute attributeX, TagHelperAttribute attributeY)
        {
            if (attributeX == attributeY)
            {
                return true;
            }

            // Normal comparer (TagHelperAttribute.Equals()) doesn't care about the Name case, in tests we do.
            return attributeX != null &&
                string.Equals(attributeX.Name, attributeY.Name, StringComparison.Ordinal) &&
                attributeX.ValueStyle == attributeY.ValueStyle &&
                (attributeX.ValueStyle == HtmlAttributeValueStyle.Minimized ||
                 string.Equals(GetString(attributeX.Value), GetString(attributeY.Value)));
        }

        public int GetHashCode(TagHelperAttribute attribute)
        {
            // Manually combine hash codes here. We can't reference HashCodeCombiner because we have internals visible
            // from Mvc.Core and Mvc.TagHelpers; both of which reference HashCodeCombiner.
            var baseHashCode = 0x1505L;
            var attributeHashCode = attribute.GetHashCode();
            var combinedHash = ((baseHashCode << 5) + baseHashCode) ^ attributeHashCode;
            var nameHashCode = StringComparer.Ordinal.GetHashCode(attribute.Name);
            combinedHash = ((combinedHash << 5) + combinedHash) ^ nameHashCode;

            return combinedHash.GetHashCode();
        }

        private string GetString(object value)
        {
            var htmlContent = value as IHtmlContent;
            if (htmlContent != null)
            {
                using (var writer = new StringWriter())
                {
                    htmlContent.WriteTo(writer, NullHtmlEncoder.Default);
                    return writer.ToString();
                }
            }

            return value?.ToString() ?? string.Empty;
        }
    }
}