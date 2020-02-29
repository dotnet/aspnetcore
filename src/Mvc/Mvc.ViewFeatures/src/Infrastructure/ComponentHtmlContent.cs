// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    internal class ComponentHtmlContent : IHtmlContent
    {
        private readonly IEnumerable<string> _preamble;
        private readonly IEnumerable<string> _componentResult;
        private readonly IEnumerable<string> _epilogue;

        public ComponentHtmlContent(IEnumerable<string> componentResult)
            : this(Array.Empty<string>(), componentResult, Array.Empty<string>()) { }

        public ComponentHtmlContent(IEnumerable<string> preamble, IEnumerable<string> componentResult, IEnumerable<string> epilogue) =>
            (_preamble, _componentResult, _epilogue) = (preamble, componentResult, epilogue);

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            foreach (var element in _preamble.Concat(_componentResult).Concat(_epilogue))
            {
                writer.Write(element);
            }
        }
    }
}
