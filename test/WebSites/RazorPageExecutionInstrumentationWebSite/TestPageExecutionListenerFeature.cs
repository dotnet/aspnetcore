// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.PageExecutionInstrumentation;

namespace RazorPageExecutionInstrumentationWebSite
{
    public class TestPageExecutionListenerFeature : IPageExecutionListenerFeature
    {
        private readonly TestPageExecutionContext _executionContext = new TestPageExecutionContext();

        public IHoldInstrumentationData Holder => _executionContext;

        public TextWriter DecorateWriter(TextWriter writer)
        {
            return writer;
        }

        public IPageExecutionContext GetContext(string sourceFilePath, TextWriter writer)
        {
            _executionContext.FilePath = sourceFilePath;

            return _executionContext;
        }

        private class TestPageExecutionContext : IHoldInstrumentationData, IPageExecutionContext
        {
            private readonly List<InstrumentationData> _values = new List<InstrumentationData>();
            private bool _ignoreNewData;

            public string FilePath { get; set; }

            public IEnumerable<InstrumentationData> Values => _values;

            public void IgnoreFurtherData()
            {
                _ignoreNewData = true;
            }

            public void BeginContext(int position, int length, bool isLiteral)
            {
                if (_ignoreNewData)
                {
                    return;
                }

                _values.Add(new InstrumentationData(FilePath, position, length, isLiteral));
            }

            public void EndContext()
            {
            }
        }
    }
}