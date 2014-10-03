// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.PageExecutionInstrumentation;

namespace RazorInstrumentationWebSite
{
    public class TestPageExecutionContext : IPageExecutionContext
    {
        public List<Tuple<int, int, bool>> Values { get; }
            = new List<Tuple<int, int, bool>>();

        public void BeginContext(int position, int length, bool isLiteral)
        {
            Values.Add(Tuple.Create(position, length, isLiteral));
        }

        public void EndContext()
        {
        }
    }
}