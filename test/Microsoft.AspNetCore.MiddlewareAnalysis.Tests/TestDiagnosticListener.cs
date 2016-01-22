// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DiagnosticAdapter;

namespace Microsoft.AspNetCore.MiddlewareAnalysis
{
    public class TestDiagnosticListener
    {
        public IList<string> MiddlewareStarting { get; } = new List<string>();
        public IList<string> MiddlewareFinished { get; } = new List<string>();
        public IList<string> MiddlewareException { get; } = new List<string>();

        [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareStarting")]
        public virtual void OnMiddlewareStarting(HttpContext httpContext, string name)
        {
            MiddlewareStarting.Add(name);
        }

        [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareException")]
        public virtual void OnMiddlewareException(Exception exception, string name)
        {
            MiddlewareException.Add(name);
        }

        [DiagnosticName("Microsoft.AspNetCore.MiddlewareAnalysis.MiddlewareFinished")]
        public virtual void OnMiddlewareFinished(HttpContext httpContext, string name)
        {
            MiddlewareFinished.Add(name);
        }
    }
}
