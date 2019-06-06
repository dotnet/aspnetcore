// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class HtmlContentPrerenderComponentResultAdapter : IHtmlContent
    {
        private ComponentPrerenderResult _result;

        public HtmlContentPrerenderComponentResultAdapter(ComponentPrerenderResult result)
        {
            _result = result;
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            _result.WriteTo(writer);
        }
    }
}