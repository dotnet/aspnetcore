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
    public sealed class BeforeViewPage : MvcDiagnostic
    {
        public const string EventName = EventNamespace + 
            "Razor." +
            nameof(BeforeViewPage);

        public IRazorPage Page { get; }
        public ViewContext ViewContext { get; }
        public ActionDescriptor ActionDescriptor { get; }
        public HttpContext HttpContext { get; }

        public override int Count => 4;

        public BeforeViewPage(IRazorPage page, ViewContext viewContext, ActionDescriptor actionDescriptor, HttpContext httpContext)
        {
            Page = page;
            ViewContext = viewContext;
            ActionDescriptor = actionDescriptor;
            HttpContext = httpContext;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(Page), Page),
            1 => new KeyValuePair<string, object>(nameof(ViewContext), ViewContext),
            2 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            3 => new KeyValuePair<string, object>(nameof(HttpContext), HttpContext),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterViewPage : MvcDiagnostic
    {
        public const string EventName = EventNamespace +
            "Razor." +
            nameof(AfterViewPage);

        public IRazorPage Page { get; }
        public ViewContext ViewContext { get; }
        public ActionDescriptor ActionDescriptor { get; }
        public HttpContext HttpContext { get; }

        public override int Count => 4;

        public AfterViewPage(IRazorPage page, ViewContext viewContext, ActionDescriptor actionDescriptor, HttpContext httpContext)
        {
            Page = page;
            ViewContext = viewContext;
            ActionDescriptor = actionDescriptor;
            HttpContext = httpContext;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(Page), Page),
            1 => new KeyValuePair<string, object>(nameof(ViewContext), ViewContext),
            2 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            3 => new KeyValuePair<string, object>(nameof(HttpContext), HttpContext),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeginInstrumentationContext : MvcDiagnostic
    {
        public const string EventName = EventNamespace +
            "Razor." +
            nameof(BeginInstrumentationContext);

        public HttpContext HttpContext { get; }
        public string Path { get; }
        public int Position { get; }
        public int Length { get; }
        public bool IsLiteral { get; }

        public override int Count => 5;

        public BeginInstrumentationContext(
            HttpContext httpContext,
            string path,
            int position,
            int length,
            bool isLiteral)
        {
            HttpContext = httpContext;
            Path = path;
            Position = position;
            Length = length;
            IsLiteral = isLiteral;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(HttpContext), HttpContext),
            1 => new KeyValuePair<string, object>(nameof(Path), Path),
            2 => new KeyValuePair<string, object>(nameof(Position), Position),
            3 => new KeyValuePair<string, object>(nameof(Length), Length),
            4 => new KeyValuePair<string, object>(nameof(IsLiteral), IsLiteral),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class EndInstrumentationContext : MvcDiagnostic
    {
        public const string EventName = EventNamespace +
            "Razor." +
            nameof(EndInstrumentationContext);

        public HttpContext HttpContext { get; }
        public string Path { get; }

        public override int Count => 2;

        public EndInstrumentationContext(
            HttpContext httpContext,
            string path)
        {
            HttpContext = httpContext;
            Path = path;
        }

        public override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(HttpContext), HttpContext),
            1 => new KeyValuePair<string, object>(nameof(Path), Path),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }
}