// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace Microsoft.AspNetCore.Mvc.Diagnostics
{
    public sealed class BeforeViewComponent : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeViewComponent);

        public ActionDescriptor ActionDescriptor { get; }
        public ViewComponentContext ViewComponentContext { get; }
        public object ViewComponent { get; }

        protected override int Count => 3;

        public BeforeViewComponent(ActionDescriptor actionDescriptor, ViewComponentContext viewComponentContext, object viewComponent)
        {
            ActionDescriptor = actionDescriptor;
            ViewComponentContext = viewComponentContext;
            ViewComponent = viewComponent;
        }

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ViewComponentContext), ViewComponentContext),
            2 => new KeyValuePair<string, object>(nameof(ViewComponent), ViewComponent),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterViewComponent : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterViewComponent);

        public ActionDescriptor ActionDescriptor { get; }
        public ViewComponentContext ViewComponentContext { get; }
        public IViewComponentResult ViewComponentResult { get; }
        public object ViewComponent { get; }

        protected override int Count => 4;

        public AfterViewComponent(ActionDescriptor actionDescriptor, ViewComponentContext viewComponentContext, IViewComponentResult viewComponentResult, object viewComponent)
        {
            ActionDescriptor = actionDescriptor;
            ViewComponentContext = viewComponentContext;
            ViewComponentResult = viewComponentResult;
            ViewComponent = viewComponent;
        }

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ViewComponentContext), ViewComponentContext),
            2 => new KeyValuePair<string, object>(nameof(ViewComponent), ViewComponent),
            3 => new KeyValuePair<string, object>(nameof(ViewComponentResult), ViewComponentResult),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class ViewComponentBeforeViewExecute : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(ViewComponentBeforeViewExecute);
        public ActionDescriptor ActionDescriptor { get; }
        public ViewComponentContext ViewComponentContext { get; }
        public IView View { get; }

        protected override int Count => 3;

        public ViewComponentBeforeViewExecute(ActionDescriptor actionDescriptor, ViewComponentContext viewComponentContext, IView view)
        {
            ActionDescriptor = actionDescriptor;
            ViewComponentContext = viewComponentContext;
            View = view;
        }

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ViewComponentContext), ViewComponentContext),
            2 => new KeyValuePair<string, object>(nameof(View), View),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class ViewComponentAfterViewExecute : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(ViewComponentAfterViewExecute);

        public ActionDescriptor ActionDescriptor { get; }
        public ViewComponentContext ViewComponentContext { get; }
        public IView View { get; }

        protected override int Count => 3;

        public ViewComponentAfterViewExecute(ActionDescriptor actionDescriptor, ViewComponentContext viewComponentContext, IView view)
        {
            ActionDescriptor = actionDescriptor;
            ViewComponentContext = viewComponentContext;
            View = view;
        }

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ViewComponentContext), ViewComponentContext),
            2 => new KeyValuePair<string, object>(nameof(View), View),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeView : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(BeforeView);

        public IView View { get; }
        public ViewContext ViewContext { get; }

        protected override int Count => 2;

        public BeforeView(IView view, ViewContext viewContext)
        {
            View = view;
            ViewContext = viewContext;
        }

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(View), View),
            1 => new KeyValuePair<string, object>(nameof(ViewContext), ViewContext),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterView : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(AfterView);

        public IView View { get; }
        public ViewContext ViewContext { get; }

        protected override int Count => 2;

        public AfterView(IView view, ViewContext viewContext)
        {
            View = view;
            ViewContext = viewContext;
        }

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(View), View),
            1 => new KeyValuePair<string, object>(nameof(ViewContext), ViewContext),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class ViewFound : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(ViewFound);

        public ActionContext ActionContext { get; }
        public bool IsMainPage { get; }
        public ActionResult Result { get; }
        public string ViewName { get; }
        public IView View { get; }

        protected override int Count => 5;

        public ViewFound(ActionContext actionContext, bool isMainPage, ActionResult result, string viewName, IView view)
        {
            ActionContext = actionContext;
            IsMainPage = isMainPage;
            Result = result;
            ViewName = viewName;
            View = view;
        }

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
            1 => new KeyValuePair<string, object>(nameof(IsMainPage), IsMainPage),
            2 => new KeyValuePair<string, object>(nameof(Result), Result),
            3 => new KeyValuePair<string, object>(nameof(ViewName), ViewName),
            4 => new KeyValuePair<string, object>(nameof(View), View),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class ViewNotFound : MvcDiagnostic
    {
        public const string EventName = EventNamespace + nameof(ViewNotFound);

        public ActionContext ActionContext { get; }
        public bool IsMainPage { get; }
        public ActionResult Result { get; }
        public string ViewName { get; }
        public IEnumerable<string> SearchedLocations { get; }

        protected override int Count => 5;

        public ViewNotFound(ActionContext actionContext, bool isMainPage, ActionResult result, string viewName, IEnumerable<string> searchedLocations)
        {
            ActionContext = actionContext;
            IsMainPage = isMainPage;
            Result = result;
            ViewName = viewName;
            SearchedLocations = searchedLocations;
        }

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionContext), ActionContext),
            1 => new KeyValuePair<string, object>(nameof(IsMainPage), IsMainPage),
            2 => new KeyValuePair<string, object>(nameof(Result), Result),
            3 => new KeyValuePair<string, object>(nameof(ViewName), ViewName),
            4 => new KeyValuePair<string, object>(nameof(SearchedLocations), SearchedLocations),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }
}