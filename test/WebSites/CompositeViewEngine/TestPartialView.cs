// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;

namespace CompositeViewEngine
{
    public class TestPartialView : IView
    {
        public async Task RenderAsync(ViewContext context)
        {
            await context.Writer.WriteLineAsync("world");
        }
    }
}