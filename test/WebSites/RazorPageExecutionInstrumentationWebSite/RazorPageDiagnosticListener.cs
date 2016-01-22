// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DiagnosticAdapter;

namespace RazorPageExecutionInstrumentationWebSite
{
    public class RazorPageDiagnosticListener
    {
        public static readonly object WriterKey = new object();

        [DiagnosticName("Microsoft.AspNetCore.Mvc.Razor.BeginInstrumentationContext")]
        public virtual void OnBeginPageInstrumentationContext(
            HttpContext httpContext,
            string path,
            int position,
            int length,
            bool isLiteral)
        {
            var literal = isLiteral ? "Literal" : "Non-literal";
            var text = $"{path}: {literal} at {position} contains {length} characters.";

            var writer = (TextWriter)httpContext.Items[WriterKey];
            writer.WriteLine(text);
        }
    }
}
