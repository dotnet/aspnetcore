// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc
{
    public class JsonViewComponentResult : IViewComponentResult
    {
        public JsonViewComponentResult([NotNull] object data)
        {
            Data = data;
        }

        public object Data { get; private set; }

        public void Execute([NotNull] ViewComponentContext context)
        {
            var formatter = new JsonOutputFormatter();
            formatter.WriteObject(context.Writer, Data);
        }

        #pragma warning disable 1998
        public async Task ExecuteAsync([NotNull] ViewComponentContext context)
        {
            Execute(context);
        }
        #pragma warning restore 1998
    }
}
