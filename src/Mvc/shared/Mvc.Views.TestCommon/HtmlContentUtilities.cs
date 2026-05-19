// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.WebEncoders.Testing;

namespace Microsoft.AspNetCore.Mvc;

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
