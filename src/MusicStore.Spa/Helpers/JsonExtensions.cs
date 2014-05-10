using System;
using System.Collections.Generic;
using Microsoft.AspNet.Routing;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public static class JsonExtensions
    {
        public static HtmlString Json<T, TData>(this IHtmlHelper<T> helper, TData data)
        {
            return Json(helper, data, new RouteValueDictionary());
        }

        public static HtmlString Json<T, TData>(this IHtmlHelper<T> helper, TData data, object htmlAttributes)
        {
            return Json(helper, data, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        }

        public static HtmlString Json<T, TData>(this IHtmlHelper<T> helper, TData data, IDictionary<string, object> htmlAttributes)
        {
            var builder = new TagBuilder("script");
            builder.Attributes["type"] = "application/json";
            builder.MergeAttributes(htmlAttributes);
            builder.InnerHtml =
                (data is JsonString
                    ? data.ToString()
                    : JsonConvert.SerializeObject(data))
                .Replace("<", "\u003C").Replace(">", "\u003E");

            return helper.Tag(builder);
        }

        public static HtmlString InlineData<T>(this IHtmlHelper<T> helper, string actionName, string controllerName)
        {
            //var result = helper.Action(actionName, controllerName);
            //var urlHelper = new UrlHelper(helper.ViewContext.RequestContext);
            //var url = urlHelper.Action(actionName, controllerName);

            //return helper.Json(new JsonString(result), new RouteValueDictionary {
            //    { "app-inline-data", null },
            //    { "for", url }
            //});

            return helper.Json(new JsonString(new object()), null);
        }
    }

    public class JsonString
    {
        public JsonString(object value)
            : this(value.ToString())
        {

        }

        public JsonString(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }

        public override string ToString()
        {
            return Value;
        }
    }
}