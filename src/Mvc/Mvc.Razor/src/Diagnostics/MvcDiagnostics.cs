// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Microsoft.AspNetCore.Mvc.Diagnostics
{
    public sealed class BeforeViewPageEventData : EventData
    {
        public const string EventName = EventNamespace + 
            "Razor." +
            "BeforeViewPage";

        public BeforeViewPageEventData(IRazorPage page, ViewContext viewContext, ActionDescriptor actionDescriptor, HttpContext httpContext)
        {
            Page = page;
            ViewContext = viewContext;
            ActionDescriptor = actionDescriptor;
            HttpContext = httpContext;
        }

        public IRazorPage Page { get; }
        public ViewContext ViewContext { get; }
        public ActionDescriptor ActionDescriptor { get; }
        public HttpContext HttpContext { get; }

        protected override int Count => 4;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(Page), Page),
            1 => new KeyValuePair<string, object>(nameof(ViewContext), ViewContext),
            2 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            3 => new KeyValuePair<string, object>(nameof(HttpContext), HttpContext),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterViewPageEventData : EventData
    {
        public const string EventName = EventNamespace +
            "Razor." +
            "AfterViewPage";

        public AfterViewPageEventData(IRazorPage page, ViewContext viewContext, ActionDescriptor actionDescriptor, HttpContext httpContext)
        {
            Page = page;
            ViewContext = viewContext;
            ActionDescriptor = actionDescriptor;
            HttpContext = httpContext;
        }

        public IRazorPage Page { get; }
        public ViewContext ViewContext { get; }
        public ActionDescriptor ActionDescriptor { get; }
        public HttpContext HttpContext { get; }

        protected override int Count => 4;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(Page), Page),
            1 => new KeyValuePair<string, object>(nameof(ViewContext), ViewContext),
            2 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            3 => new KeyValuePair<string, object>(nameof(HttpContext), HttpContext),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }
}