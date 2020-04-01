// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.WebEncoders.Testing;

namespace Microsoft.AspNetCore.Mvc
{
    public class HtmlContentUtilities
    {
        public static string HtmlContentToString(IHtmlContent content, HtmlEncoder encoder = null)
        {
            if (encoder == null)
            {
                encoder = new HtmlTestEncoder();
            }

            using (var writer = new StringWriter())
            {
                content.WriteTo(writer, encoder);
                return writer.ToString();
            }
        }
    }
}
