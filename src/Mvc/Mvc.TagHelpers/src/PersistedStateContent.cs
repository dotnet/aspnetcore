// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    internal class PersistedStateContent : IHtmlContent
    {
        private readonly string _state;

        public PersistedStateContent(string state)
        {
            _state = state;
        }

        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            writer.Write("<!--Blazor-Component-State:");
            writer.Write(_state);
            writer.Write("-->");
        }
    }
}
