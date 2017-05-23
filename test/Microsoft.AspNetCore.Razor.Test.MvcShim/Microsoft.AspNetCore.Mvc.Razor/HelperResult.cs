// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class HelperResult : IHtmlContent
    {
        public HelperResult(Func<TextWriter, Task> asyncAction)
        {
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
        }
    }
}
