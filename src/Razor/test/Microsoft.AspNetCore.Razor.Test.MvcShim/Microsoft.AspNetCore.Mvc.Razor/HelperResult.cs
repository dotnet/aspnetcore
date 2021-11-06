// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Razor;

public class HelperResult : IHtmlContent
{
    public HelperResult(Func<TextWriter, Task> asyncAction)
    {
    }

    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
    }
}
