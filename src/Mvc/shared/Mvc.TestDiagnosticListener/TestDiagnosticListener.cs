// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DiagnosticAdapter;

namespace Microsoft.AspNetCore.Mvc;

public class TestDiagnosticListener
{
    public class OnBeforeResourceEventData
    {        
        public IProxyActionDescriptor ActionDescriptor { get; set;  }        
        public object ExecutingContext { get; set; }        
        public object Filter { get; set; }
    }

    public OnBeforeResourceEventData BeforeResource { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.BeforeOnResourceExecution")]
    public virtual void OnBeforeResource(
        IProxyActionDescriptor actionDescriptor,
        object resourceExecutingContext,
        object filter)
    {        
        BeforeResource = new OnBeforeResourceEventData()
        {
            ActionDescriptor = actionDescriptor,
            ExecutingContext = resourceExecutingContext,
            Filter = filter
        };
    }

    public class OnAfterResourceEventData
    {
        public IProxyActionDescriptor ActionDescriptor { get; set; }
        public object ExecutedContext { get; set; }
        public object Filter { get; set; }
    }

    public OnAfterResourceEventData AfterResource { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.AfterOnResourceExecution")]
    public virtual void OnAfterResource(
        IProxyActionDescriptor actionDescriptor,
        object resourceExecutedContext,
        object filter)
    {
        AfterResource = new OnAfterResourceEventData()
        {
            ActionDescriptor = actionDescriptor,
            ExecutedContext = resourceExecutedContext,
            Filter = filter
        };
    }    

    public class OnBeforeActionEventData
    {
        public IProxyActionDescriptor ActionDescriptor { get; set; }
        public IProxyHttpContext HttpContext { get; set; }
        public IProxyRouteData RouteData { get; set; }
    }

    public OnBeforeActionEventData BeforeAction { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.BeforeAction")]
    public virtual void OnBeforeAction(
        IProxyHttpContext httpContext,
        IProxyRouteData routeData,
        IProxyActionDescriptor actionDescriptor)
    {
        BeforeAction = new OnBeforeActionEventData()
        {
            ActionDescriptor = actionDescriptor,
            HttpContext = httpContext,
            RouteData = routeData,
        };
    }

    public class OnAfterActionEventData
    {
        public IProxyActionDescriptor ActionDescriptor { get; set; }
        public IProxyHttpContext HttpContext { get; set; }
    }

    public OnAfterActionEventData AfterAction { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.AfterAction")]
    public virtual void OnAfterAction(
        IProxyHttpContext httpContext,
        IProxyActionDescriptor actionDescriptor)
    {
        AfterAction = new OnAfterActionEventData()
        {
            ActionDescriptor = actionDescriptor,
            HttpContext = httpContext,
        };
    }

    public class OnBeforeActionMethodEventData
    {
        public IProxyActionContext ActionContext { get; set; }
        public IReadOnlyDictionary<string, object> Arguments { get; set; }
    }

    public OnBeforeActionMethodEventData BeforeActionMethod { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.BeforeActionMethod")]
    public virtual void OnBeforeActionMethod(
        IProxyActionContext actionContext,
        IReadOnlyDictionary<string, object> arguments)
    {
        BeforeActionMethod = new OnBeforeActionMethodEventData()
        {
            ActionContext = actionContext,
            Arguments = arguments,
        };
    }

    public class OnAfterActionMethodEventData
    {
        public IProxyActionContext ActionContext { get; set; }
        public IProxyActionResult Result { get; set; }
    }

    public OnAfterActionMethodEventData AfterActionMethod { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.AfterActionMethod")]
    public virtual void OnAfterActionMethod(
        IProxyActionContext actionContext,
        IProxyActionResult result)
    {
        AfterActionMethod = new OnAfterActionMethodEventData()
        {
            ActionContext = actionContext,
            Result = result,
        };
    }

    public class OnBeforeActionResultEventData
    {
        public IProxyActionContext ActionContext { get; set; }
        public IProxyActionResult Result { get; set; }
    }

    public OnBeforeActionResultEventData BeforeActionResult { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.BeforeActionResult")]
    public virtual void OnBeforeActionResult(IProxyActionContext actionContext, IProxyActionResult result)
    {
        BeforeActionResult = new OnBeforeActionResultEventData()
        {
            ActionContext = actionContext,
            Result = result,
        };
    }

    public class OnAfterActionResultEventData
    {
        public IProxyActionContext ActionContext { get; set; }
        public IProxyActionResult Result { get; set; }
    }

    public OnAfterActionResultEventData AfterActionResult { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.AfterActionResult")]
    public virtual void OnAfterActionResult(IProxyActionContext actionContext, IProxyActionResult result)
    {
        AfterActionResult = new OnAfterActionResultEventData()
        {
            ActionContext = actionContext,
            Result = result,
        };
    }

    public class OnViewFoundEventData
    {
        public IProxyActionContext ActionContext { get; set; }
        public bool IsMainPage { get; set; }
        public IProxyActionResult Result { get; set; }
        public string ViewName { get; set; }
        public IProxyView View { get; set; }
    }

    public OnViewFoundEventData ViewFound { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.ViewFound")]
    public virtual void OnViewFound(
        IProxyActionContext actionContext,
        bool isMainPage,
        IProxyActionResult result,
        string viewName,
        IProxyView view)
    {
        ViewFound = new OnViewFoundEventData()
        {
            ActionContext = actionContext,
            IsMainPage = isMainPage,
            Result = result,
            ViewName = viewName,
            View = view,
        };
    }

    public class OnViewNotFoundEventData
    {
        public IProxyActionContext ActionContext { get; set; }
        public bool IsMainPage { get; set; }
        public IProxyActionResult Result { get; set; }
        public string ViewName { get; set; }
        public IEnumerable<string> SearchedLocations { get; set; }
    }

    public OnViewNotFoundEventData ViewNotFound { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.ViewNotFound")]
    public virtual void OnViewNotFound(
        IProxyActionContext actionContext,
        bool isMainPage,
        IProxyActionResult result,
        string viewName,
        IEnumerable<string> searchedLocations)
    {
        ViewNotFound = new OnViewNotFoundEventData()
        {
            ActionContext = actionContext,
            IsMainPage = isMainPage,
            Result = result,
            ViewName = viewName,
            SearchedLocations = searchedLocations,
        };
    }

    public class OnBeforeViewEventData
    {
        public IProxyView View { get; set; }
        public IProxyViewContext ViewContext { get; set; }
    }

    public OnBeforeViewEventData BeforeView { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.BeforeView")]
    public virtual void OnBeforeView(IProxyView view, IProxyViewContext viewContext)
    {
        BeforeView = new OnBeforeViewEventData()
        {
            View = view,
            ViewContext = viewContext,
        };
    }

    public class OnAfterViewEventData
    {
        public IProxyView View { get; set; }
        public IProxyViewContext ViewContext { get; set; }
    }

    public OnAfterViewEventData AfterView { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.AfterView")]
    public virtual void OnAfterView(IProxyView view, IProxyViewContext viewContext)
    {
        AfterView = new OnAfterViewEventData()
        {
            View = view,
            ViewContext = viewContext,
        };
    }

    public class OnBeforeViewPageEventData
    {
        public IProxyPage Page { get; set; }
        public IProxyViewContext ViewContext { get; set; }
        public IProxyActionDescriptor ActionDescriptor { get; set; }
        public IProxyHttpContext HttpContext { get; set; }
    }

    public OnBeforeViewPageEventData BeforeViewPage { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.Razor.BeforeViewPage")]
    public virtual void OnBeforeViewPage(
        IProxyPage page,
        IProxyViewContext viewContext,
        IProxyActionDescriptor actionDescriptor,
        IProxyHttpContext httpContext)
    {
        BeforeViewPage = new OnBeforeViewPageEventData()
        {
            Page = page,
            ViewContext = viewContext,
            ActionDescriptor = actionDescriptor,
            HttpContext = httpContext,
        };
    }

    public class OnAfterViewPageEventData
    {
        public IProxyPage Page { get; set; }
        public IProxyViewContext ViewContext { get; set; }
        public IProxyActionDescriptor ActionDescriptor { get; set; }
        public IProxyHttpContext HttpContext { get; set; }
    }

    public OnAfterViewPageEventData AfterViewPage { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.Razor.AfterViewPage")]
    public virtual void OnAfterViewPage(
        IProxyPage page,
        IProxyViewContext viewContext,
        IProxyActionDescriptor actionDescriptor,
        IProxyHttpContext httpContext)
    {
        AfterViewPage = new OnAfterViewPageEventData()
        {
            Page = page,
            ViewContext = viewContext,
            ActionDescriptor = actionDescriptor,
            HttpContext = httpContext,
        };
    }

    public class OnBeforeViewComponentEventData
    {
        public IProxyActionDescriptor ActionDescriptor { get; set; }

        public IProxyViewComponentContext ViewComponentContext { get; set; }

        public object ViewComponent { get; set; }
    }

    public OnBeforeViewComponentEventData BeforeViewComponent { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.BeforeViewComponent")]
    public virtual void OnBeforeViewComponent(
        IProxyActionDescriptor actionDescriptor,
        IProxyViewComponentContext viewComponentContext,
        object viewComponent)
    {
        BeforeViewComponent = new OnBeforeViewComponentEventData()
        {
            ActionDescriptor = actionDescriptor,
            ViewComponentContext = viewComponentContext,
            ViewComponent = viewComponent
        };
    }

    public class OnAfterViewComponentEventData
    {
        public IProxyActionDescriptor ActionDescriptor { get; set; }

        public IProxyViewComponentContext ViewComponentContext { get; set; }

        public IProxyViewComponentResult ViewComponentResult { get; set; }

        public object ViewComponent { get; set; }
    }

    public OnAfterViewComponentEventData AfterViewComponent { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.AfterViewComponent")]
    public virtual void OnAfterViewComponent(
        IProxyActionDescriptor actionDescriptor,
        IProxyViewComponentContext viewComponentContext,
        IProxyViewComponentResult viewComponentResult,
        object viewComponent)
    {
        AfterViewComponent = new OnAfterViewComponentEventData()
        {
            ActionDescriptor = actionDescriptor,
            ViewComponentContext = viewComponentContext,
            ViewComponentResult = viewComponentResult,
            ViewComponent = viewComponent
        };
    }

    public class OnViewComponentBeforeViewExecuteEventData
    {
        public IProxyActionDescriptor ActionDescriptor { get; set; }

        public IProxyViewComponentContext ViewComponentContext { get; set; }

        public IProxyView View { get; set; }
    }

    public OnViewComponentBeforeViewExecuteEventData ViewComponentBeforeViewExecute { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.ViewComponentBeforeViewExecute")]
    public virtual void OnViewComponentBeforeViewExecute(
        IProxyActionDescriptor actionDescriptor,
        IProxyViewComponentContext viewComponentContext,
        IProxyView view)
    {
        ViewComponentBeforeViewExecute = new OnViewComponentBeforeViewExecuteEventData()
        {
            ActionDescriptor = actionDescriptor,
            ViewComponentContext = viewComponentContext,
            View = view
        };
    }

    public class OnViewComponentAfterViewExecuteEventData
    {
        public IProxyActionDescriptor ActionDescriptor { get; set; }

        public IProxyViewComponentContext ViewComponentContext { get; set; }

        public IProxyView View { get; set; }
    }

    public OnViewComponentAfterViewExecuteEventData ViewComponentAfterViewExecute { get; set; }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.ViewComponentAfterViewExecute")]
    public virtual void OnViewComponentAfterViewExecute(
        IProxyActionDescriptor actionDescriptor,
        IProxyViewComponentContext viewComponentContext,
        IProxyView view)
    {
        ViewComponentAfterViewExecute = new OnViewComponentAfterViewExecuteEventData()
        {
            ActionDescriptor = actionDescriptor,
            ViewComponentContext = viewComponentContext,
            View = view
        };
    }

    public class BeginPageInstrumentationData
    {
        public IProxyHttpContext HttpContext { get; set; }

        public string Path { get; set; }

        public int Position { get; set; }

        public int Length { get; set; }

        public bool IsLiteral { get; set; }
    }

    public class EndPageInstrumentationData
    {
        public IProxyHttpContext HttpContext { get; set; }

        public string Path { get; set; }
    }

    public List<object> PageInstrumentationData { get; set; } = new List<object>();

    [DiagnosticName("Microsoft.AspNetCore.Mvc.Razor.BeginInstrumentationContext")]
    public virtual void OnBeginPageInstrumentationContext(
        IProxyHttpContext httpContext,
        string path,
        int position,
        int length,
        bool isLiteral)
    {
        PageInstrumentationData.Add(new BeginPageInstrumentationData
        {
            HttpContext = httpContext,
            Path = path,
            Position = position,
            Length = length,
            IsLiteral = isLiteral,
        });
    }

    [DiagnosticName("Microsoft.AspNetCore.Mvc.Razor.EndInstrumentationContext")]
    public virtual void OnEndPageInstrumentationContext(
        IProxyHttpContext httpContext,
        string path,
        int position,
        int length,
        bool isLiteral)
    {
        PageInstrumentationData.Add(new EndPageInstrumentationData
        {
            HttpContext = httpContext,
            Path = path,
        });
    }
}
