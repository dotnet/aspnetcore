using System;
using Microsoft.Framework.OptionsModel;
using System.Threading.Tasks;
using System.Linq;

namespace Microsoft.AspNet.Mvc.Core.Filters
{
    public class FormatFilter : IFormatFilter
    {
        public void OnResourceExecuting([NotNull] ResourceExecutingContext context)
        {
            var options = (IOptions<MvcOptions>)context.HttpContext.RequestServices.GetService(
                typeof(IOptions<MvcOptions>));
            string format = null;
            
            if (context.RouteData.Values.ContainsKey("format"))
            {
                format = context.RouteData.Values["format"].ToString();
            }
            else if(context.HttpContext.Request.Query.ContainsKey("format"))
            {
                format = context.HttpContext.Request.Query.Get("format").ToString();
            }

            if (format != null)
            {
                var contentType = options.Options.OutputFormatterOptions.GetContentTypeForFormat(format);
                if (contentType == null)
                {
                    // no contentType exists for the foramt, return 404
                    context.Result = new HttpNotFoundResult();
                }
                else
                {
                    if (context.Filters.Any(f => f is ProducesAttribute))
                    {
                        var produces = context.Filters.First(f => f is ProducesAttribute) as ProducesAttribute;
                        if(!produces.ContentTypes.Contains(contentType))
                        {
                            context.Result = new HttpNotFoundResult();
                        }
                    }
                }
            }
        }
        
        public void OnResourceExecuted([NotNull] ResourceExecutedContext context)
        {
            
        }

        public void OnResultExecuting([NotNull]ResultExecutingContext context)
        {
            var options = (IOptions<MvcOptions>)context.HttpContext.RequestServices.GetService(
                typeof(IOptions<MvcOptions>));
            string format = null;

            if (context.RouteData.Values.ContainsKey("format"))
            {
                format = context.RouteData.Values["format"].ToString();
            }
            else if (context.HttpContext.Request.Query.ContainsKey("format"))
            {
                format = context.HttpContext.Request.Query.Get("format").ToString();
            }
            if (format != null)
            {
                var contentType = options.Options.OutputFormatterOptions.GetContentTypeForFormat(format);
                if (contentType != null)
                {
                    var objectResult = context.Result as ObjectResult;
                    if (objectResult != null)
                    {
                        objectResult.ContentTypes.Clear();
                        objectResult.ContentTypes.Add(contentType);
                    }
                }
                else
                {
                    context.Result = new HttpStatusCodeResult(404);
                }
            }            
        }

        public void OnResultExecuted([NotNull]ResultExecutedContext context)
        {
            
        }
    }
}