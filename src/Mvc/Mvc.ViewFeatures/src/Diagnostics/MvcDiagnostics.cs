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
    public sealed class BeforeViewComponentEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeViewComponent";

        public BeforeViewComponentEventData(ActionDescriptor actionDescriptor, ViewComponentContext viewComponentContext, object viewComponent)
        {
            ActionDescriptor = actionDescriptor;
            ViewComponentContext = viewComponentContext;
            ViewComponent = viewComponent;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ViewComponentContext ViewComponentContext { get; }
        public object ViewComponent { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ViewComponentContext), ViewComponentContext),
            2 => new KeyValuePair<string, object>(nameof(ViewComponent), ViewComponent),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterViewComponentEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterViewComponent";

        public AfterViewComponentEventData(ActionDescriptor actionDescriptor, ViewComponentContext viewComponentContext, IViewComponentResult viewComponentResult, object viewComponent)
        {
            ActionDescriptor = actionDescriptor;
            ViewComponentContext = viewComponentContext;
            ViewComponentResult = viewComponentResult;
            ViewComponent = viewComponent;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ViewComponentContext ViewComponentContext { get; }
        public IViewComponentResult ViewComponentResult { get; }
        public object ViewComponent { get; }

        protected override int Count => 4;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ViewComponentContext), ViewComponentContext),
            2 => new KeyValuePair<string, object>(nameof(ViewComponent), ViewComponent),
            3 => new KeyValuePair<string, object>(nameof(ViewComponentResult), ViewComponentResult),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class ViewComponentBeforeViewExecuteEventData : EventData
    {
        public const string EventName = EventNamespace + "ViewComponentBeforeViewExecute";

        public ViewComponentBeforeViewExecuteEventData(ActionDescriptor actionDescriptor, ViewComponentContext viewComponentContext, IView view)
        {
            ActionDescriptor = actionDescriptor;
            ViewComponentContext = viewComponentContext;
            View = view;
        }
        public ActionDescriptor ActionDescriptor { get; }
        public ViewComponentContext ViewComponentContext { get; }
        public IView View { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ViewComponentContext), ViewComponentContext),
            2 => new KeyValuePair<string, object>(nameof(View), View),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class ViewComponentAfterViewExecuteEventData : EventData
    {
        public const string EventName = EventNamespace + "ViewComponentAfterViewExecute";

        public ViewComponentAfterViewExecuteEventData(ActionDescriptor actionDescriptor, ViewComponentContext viewComponentContext, IView view)
        {
            ActionDescriptor = actionDescriptor;
            ViewComponentContext = viewComponentContext;
            View = view;
        }

        public ActionDescriptor ActionDescriptor { get; }
        public ViewComponentContext ViewComponentContext { get; }
        public IView View { get; }

        protected override int Count => 3;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(ActionDescriptor), ActionDescriptor),
            1 => new KeyValuePair<string, object>(nameof(ViewComponentContext), ViewComponentContext),
            2 => new KeyValuePair<string, object>(nameof(View), View),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class BeforeViewEventData : EventData
    {
        public const string EventName = EventNamespace + "BeforeView";

        public BeforeViewEventData(IView view, ViewContext viewContext)
        {
            View = view;
            ViewContext = viewContext;
        }

        public IView View { get; }
        public ViewContext ViewContext { get; }

        protected override int Count => 2;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(View), View),
            1 => new KeyValuePair<string, object>(nameof(ViewContext), ViewContext),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class AfterViewEventData : EventData
    {
        public const string EventName = EventNamespace + "AfterView";

        public AfterViewEventData(IView view, ViewContext viewContext)
        {
            View = view;
            ViewContext = viewContext;
        }

        public IView View { get; }
        public ViewContext ViewContext { get; }

        protected override int Count => 2;

        protected override KeyValuePair<string, object> this[int index] => index switch
        {
            0 => new KeyValuePair<string, object>(nameof(View), View),
            1 => new KeyValuePair<string, object>(nameof(ViewContext), ViewContext),
            _ => throw new IndexOutOfRangeException(nameof(index))
        };
    }

    public sealed class ViewFoundEventData : EventData
    {
        public const string EventName = EventNamespace + "ViewFound";

        public ViewFoundEventData(ActionContext actionContext, bool isMainPage, ActionResult result, string viewName, IView view)
        {
            ActionContext = actionContext;
            IsMainPage = isMainPage;
            Result = result;
            ViewName = viewName;
            View = view;
        }

        public ActionContext ActionContext { get; }
        public bool IsMainPage { get; }
        public ActionResult Result { get; }
        public string ViewName { get; }
        public IView View { get; }

        protected override int Count => 5;

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

    public sealed class ViewNotFoundEventData : EventData
    {
        public const string EventName = EventNamespace + "ViewNotFound";

        public ViewNotFoundEventData(ActionContext actionContext, bool isMainPage, ActionResult result, string viewName, IEnumerable<string> searchedLocations)
        {
            ActionContext = actionContext;
            IsMainPage = isMainPage;
            Result = result;
            ViewName = viewName;
            SearchedLocations = searchedLocations;
        }

        public ActionContext ActionContext { get; }
        public bool IsMainPage { get; }
        public ActionResult Result { get; }
        public string ViewName { get; }
        public IEnumerable<string> SearchedLocations { get; }

        protected override int Count => 5;

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