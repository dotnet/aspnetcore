// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;

namespace CompositeViewEngineWebSite
{
    public class TestView : IView
    {
        public string Path { get; set; }

        public async Task RenderAsync(ViewContext context)
        {
            await context.Writer.WriteLineAsync("Content from test view");
        }
    }
}