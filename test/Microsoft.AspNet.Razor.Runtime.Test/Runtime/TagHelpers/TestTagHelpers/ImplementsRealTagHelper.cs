// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.TagHelpers;

namespace Microsoft.AspNet.Razor.Fake
{
    public class ImplementsRealITagHelper : Microsoft.AspNet.Razor.TagHelpers.ITagHelper
    {
        public int Order
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public void Init(TagHelperContext context)
        {
        }

        public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            throw new NotImplementedException();
        }
    }

}
