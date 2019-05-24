// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.Rendering
{
    public class HtmlRendererTests : HtmlRendererTestBase
    {
        protected override HtmlRenderer GetHtmlRenderer(IServiceProvider serviceProvider)
        {
            return new HtmlRenderer(serviceProvider, _encoder, Dispatcher);
        }
    }
}
