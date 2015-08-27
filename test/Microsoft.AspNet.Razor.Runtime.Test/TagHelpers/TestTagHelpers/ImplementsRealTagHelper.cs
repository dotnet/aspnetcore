// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;

namespace Microsoft.AspNet.Razor.Fake
{
    public class ImplementsRealITagHelper : Microsoft.AspNet.Razor.Runtime.TagHelpers.ITagHelper
    {
        public int Order
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            throw new NotImplementedException();
        }
    }

}
