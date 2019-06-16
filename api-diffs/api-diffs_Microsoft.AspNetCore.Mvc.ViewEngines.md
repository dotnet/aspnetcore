# Microsoft.AspNetCore.Mvc.ViewEngines

``` diff
 namespace Microsoft.AspNetCore.Mvc.ViewEngines {
     public class CompositeViewEngine : ICompositeViewEngine, IViewEngine {
         public CompositeViewEngine(IOptions<MvcViewOptions> optionsAccessor);
         public IReadOnlyList<IViewEngine> ViewEngines { get; }
         public ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage);
         public ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage);
     }
     public interface ICompositeViewEngine : IViewEngine {
         IReadOnlyList<IViewEngine> ViewEngines { get; }
     }
     public interface IView {
         string Path { get; }
         Task RenderAsync(ViewContext context);
     }
     public interface IViewEngine {
         ViewEngineResult FindView(ActionContext context, string viewName, bool isMainPage);
         ViewEngineResult GetView(string executingFilePath, string viewPath, bool isMainPage);
     }
     public class ViewEngineResult {
         public IEnumerable<string> SearchedLocations { get; private set; }
         public bool Success { get; }
         public IView View { get; private set; }
         public string ViewName { get; private set; }
         public ViewEngineResult EnsureSuccessful(IEnumerable<string> originalLocations);
         public static ViewEngineResult Found(string viewName, IView view);
         public static ViewEngineResult NotFound(string viewName, IEnumerable<string> searchedLocations);
     }
 }
```

