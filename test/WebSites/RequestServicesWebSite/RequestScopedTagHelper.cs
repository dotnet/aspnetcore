// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Razor.Runtime.TagHelpers;
using Microsoft.AspNet.Razor.TagHelpers;

namespace RequestServicesWebSite
{
    public class RequestScopedTagHelper : TagHelper
    {
        [Activate]
        public RequestIdService RequestIdService { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Content = RequestIdService.RequestId;
        }
    }
}