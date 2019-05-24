// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class ComponentHtmlContent : IHtmlContent
    {
        private readonly IEnumerable<string> _componentResult;

        public ComponentHtmlContent(IEnumerable<string> componentResult)
        {
            _componentResult = componentResult;
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            foreach (var element in _componentResult)
            {
                writer.Write(element);
            }
        }
    }
}
