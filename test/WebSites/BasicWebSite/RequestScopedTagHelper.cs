// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BasicWebSite
{
    public class RequestScopedTagHelper : TagHelper
    {
        public RequestScopedTagHelper(RequestIdService service)
        {
            RequestIdService = service;
        }

        public RequestIdService RequestIdService { get; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.Content.SetContent(RequestIdService.RequestId);
        }
    }
}