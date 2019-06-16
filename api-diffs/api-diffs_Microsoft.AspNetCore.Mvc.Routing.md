# Microsoft.AspNetCore.Mvc.Routing

``` diff
 namespace Microsoft.AspNetCore.Mvc.Routing {
     public class AttributeRouteInfo {
         public AttributeRouteInfo();
         public string Name { get; set; }
         public int Order { get; set; }
         public bool SuppressLinkGeneration { get; set; }
         public bool SuppressPathMatching { get; set; }
         public string Template { get; set; }
     }
     public abstract class HttpMethodAttribute : Attribute, IActionHttpMethodProvider, IRouteTemplateProvider {
         public HttpMethodAttribute(IEnumerable<string> httpMethods);
         public HttpMethodAttribute(IEnumerable<string> httpMethods, string template);
         public IEnumerable<string> HttpMethods { get; }
         Nullable<int> Microsoft.AspNetCore.Mvc.Routing.IRouteTemplateProvider.Order { get; }
         public string Name { get; set; }
         public int Order { get; set; }
         public string Template { get; }
     }
     public interface IActionHttpMethodProvider {
         IEnumerable<string> HttpMethods { get; }
     }
     public interface IRouteTemplateProvider {
         string Name { get; }
         Nullable<int> Order { get; }
         string Template { get; }
     }
     public interface IRouteValueProvider {
         string RouteKey { get; }
         string RouteValue { get; }
     }
     public interface IUrlHelperFactory {
         IUrlHelper GetUrlHelper(ActionContext context);
     }
     public class KnownRouteValueConstraint : IParameterPolicy, IRouteConstraint {
-        public KnownRouteValueConstraint();

         public KnownRouteValueConstraint(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider);
         public bool Match(HttpContext httpContext, IRouter route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection);
     }
     public abstract class RouteValueAttribute : Attribute, IRouteValueProvider {
         protected RouteValueAttribute(string routeKey, string routeValue);
         public string RouteKey { get; }
         public string RouteValue { get; }
     }
     public class UrlActionContext {
         public UrlActionContext();
         public string Action { get; set; }
         public string Controller { get; set; }
         public string Fragment { get; set; }
         public string Host { get; set; }
         public string Protocol { get; set; }
         public object Values { get; set; }
     }
     public class UrlHelper : UrlHelperBase {
         public UrlHelper(ActionContext actionContext);
         protected HttpContext HttpContext { get; }
         protected IRouter Router { get; }
         public override string Action(UrlActionContext actionContext);
         protected virtual string GenerateUrl(string protocol, string host, VirtualPathData pathData, string fragment);
         protected virtual VirtualPathData GetVirtualPathData(string routeName, RouteValueDictionary values);
         public override string RouteUrl(UrlRouteContext routeContext);
     }
     public abstract class UrlHelperBase : IUrlHelper {
         protected UrlHelperBase(ActionContext actionContext);
         public ActionContext ActionContext { get; }
         protected RouteValueDictionary AmbientValues { get; }
         public abstract string Action(UrlActionContext actionContext);
         public virtual string Content(string contentPath);
         protected string GenerateUrl(string protocol, string host, string path);
         protected string GenerateUrl(string protocol, string host, string virtualPath, string fragment);
         protected RouteValueDictionary GetValuesDictionary(object values);
         public virtual bool IsLocalUrl(string url);
         public virtual string Link(string routeName, object values);
         public abstract string RouteUrl(UrlRouteContext routeContext);
     }
     public class UrlHelperFactory : IUrlHelperFactory {
         public UrlHelperFactory();
         public IUrlHelper GetUrlHelper(ActionContext context);
     }
     public class UrlRouteContext {
         public UrlRouteContext();
         public string Fragment { get; set; }
         public string Host { get; set; }
         public string Protocol { get; set; }
         public string RouteName { get; set; }
         public object Values { get; set; }
     }
 }
```

