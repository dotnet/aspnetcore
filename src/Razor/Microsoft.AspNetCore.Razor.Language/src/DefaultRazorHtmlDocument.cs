// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal class DefaultRazorHtmlDocument : RazorHtmlDocument
    {
        private readonly string _generatedHtml;
        private readonly RazorCodeGenerationOptions _options;

        public DefaultRazorHtmlDocument(
            string generatedHtml,
            RazorCodeGenerationOptions options)
        {
            if (generatedHtml == null)
            {
                throw new ArgumentNullException(nameof(generatedHtml));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _generatedHtml = generatedHtml;
            _options = options;
        }

        public override string GeneratedHtml => _generatedHtml;

        public override RazorCodeGenerationOptions Options => _options;
    }
}
