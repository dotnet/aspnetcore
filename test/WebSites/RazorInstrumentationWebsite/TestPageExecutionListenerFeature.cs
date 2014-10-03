// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNet.PageExecutionInstrumentation;

namespace RazorInstrumentationWebSite
{
    public class TestPageExecutionListenerFeature : IPageExecutionListenerFeature
    {
        private readonly IPageExecutionContext _context;

        public TestPageExecutionListenerFeature(IPageExecutionContext context)
        {
            _context = context; 
        }

        public TextWriter DecorateWriter(TextWriter writer)
        {
            return writer;
        }

        public IPageExecutionContext GetContext(string sourceFilePath, TextWriter writer)
        {
            return _context;
        }
    }
}