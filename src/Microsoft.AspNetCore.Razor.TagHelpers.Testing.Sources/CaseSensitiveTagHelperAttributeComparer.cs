// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.TagHelpers.Testing
{
    internal class CaseSensitiveTagHelperAttributeComparer : IEqualityComparer<TagHelperAttribute>
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
            var hashCodeCombiner = HashCodeCombiner.Start();
            hashCodeCombiner.Add(attribute.GetHashCode());
            hashCodeCombiner.Add(attribute.Name, StringComparer.Ordinal);

            return hashCodeCombiner.CombinedHash;
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