// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.Html.Abstractions;
using Microsoft.Framework.WebEncoders;
using Microsoft.Framework.WebEncoders.Testing;

namespace Microsoft.AspNet.Mvc.TestCommon
{
    public class HtmlContentUtilities
    {
        public static string HtmlContentToString(IHtmlContent content, IHtmlEncoder encoder = null)
        {
            if (encoder == null)
            {
                encoder = new CommonTestEncoder();
            }

            using (var writer = new StringWriter())
            {
                content.WriteTo(writer, encoder);
                return writer.ToString();
            }
        }
    }
}
