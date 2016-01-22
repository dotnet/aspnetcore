// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;

namespace Microsoft.AspNet.MiddlewareAnalysis
{
    public class AnalysisBuilder : IApplicationBuilder
    {
        private const string NextMiddlewareName = "analysis.NextMiddlewareName";

        public AnalysisBuilder(IApplicationBuilder inner)
        {
            InnerBuilder = inner;
        }

        private IApplicationBuilder InnerBuilder { get; }

        public IServiceProvider ApplicationServices
        {
            get { return InnerBuilder.ApplicationServices; }
            set { InnerBuilder.ApplicationServices = value; }
        }

        public IDictionary<string, object> Properties
        {
            get { return InnerBuilder.Properties; }
        }

        public IFeatureCollection ServerFeatures
        {
            get { return InnerBuilder.ServerFeatures;}
        }

        public RequestDelegate Build()
        {
            // Add one maker at the end before the default 404 middleware (or any fancy Join middleware).
            return InnerBuilder.UseMiddleware<AnalysisMiddleware>("EndOfPipeline")
                .Build();
        }

        public IApplicationBuilder New()
        {
            return new AnalysisBuilder(InnerBuilder.New());
        }

        public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware)
        {
            string middlewareName = string.Empty; // UseMiddleware doesn't work with null params.
            object middlewareNameObj;
            if (Properties.TryGetValue(NextMiddlewareName, out middlewareNameObj))
            {
                middlewareName = middlewareNameObj?.ToString();
                Properties.Remove(NextMiddlewareName);
            }

            return InnerBuilder.UseMiddleware<AnalysisMiddleware>(middlewareName)
                .Use(middleware);
        }
    }
}
