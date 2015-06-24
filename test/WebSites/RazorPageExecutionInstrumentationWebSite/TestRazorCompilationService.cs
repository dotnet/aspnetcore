// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using System.Text;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.AspNet.Mvc.Razor.Compilation;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.Framework.OptionsModel;

namespace RazorPageExecutionInstrumentationWebSite
{
    public class TestRazorCompilationService : RazorCompilationService
    {
        public TestRazorCompilationService(
            ICompilationService compilationService,
            IMvcRazorHost razorHost,
            IOptions<RazorViewEngineOptions> viewEngineOptions)
            : base(compilationService, razorHost, viewEngineOptions)
        {
        }

        protected override GeneratorResults GenerateCode(string relativePath, Stream inputStream)
        {
            // Normalize line endings to '\n' (LF). This removes core.autocrlf, core.eol, core.safecrlf, and
            // .gitattributes from the equation and treats "\r\n", "\r", and "\n" as equivalent. Does not handle
            // some obscure line endings (e.g. "\n\r") but otherwise ensures instrumentation locations are
            // consistent.
            string text;
            using (var streamReader = new StreamReader(inputStream))
            {
                text = streamReader.ReadToEnd()
                    .Replace("\r\n", "\n")  // Windows line endings
                    .Replace("\r", "\n");   // Older Mac OS line endings
            }

            var bytes = Encoding.UTF8.GetBytes(text);
            inputStream = new MemoryStream(bytes);

            return base.GenerateCode(relativePath, inputStream);
        }
    }
}