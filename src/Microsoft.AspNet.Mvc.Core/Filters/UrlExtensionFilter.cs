using Microsoft.AspNet.Mvc.HeaderValueAbstractions;
using Microsoft.Framework.OptionsModel;
using System;
using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Core.Filters
{
    public class UrlExtensionFilter : IResultFilter
    {
        //private Dictionary<string, MediaTypeHeaderValue> FormatContentTypeMap = 
        //    new Dictionary<string, MediaTypeHeaderValue>();
        
        //public void AddFormatMapping(string format, MediaTypeHeaderValue contentType)
        //{
        //    if(FormatContentTypeMap.ContainsKey(format))
        //    {
        //        FormatContentTypeMap.Remove(format);
        //    }

        //    FormatContentTypeMap.Add(format, contentType);
        //}

        public void OnResultExecuting([NotNull] ResultExecutingContext context)
        {
            var options = (IOptions<MvcOptions>)context.HttpContext.RequestServices.GetService(typeof(IOptions<MvcOptions>));

            if (context.RouteData.Values.ContainsKey("format"))
            {
                var format = context.RouteData.Values["format"].ToString();
                var contentType = options.Options.OutputFormatterOptions.GetContentTypeForFormat(format);
                if (contentType != null)
                {
                    var objectResult = context.Result as ObjectResult;
                    objectResult.ContentTypes.Clear();
                    objectResult.ContentTypes.Add(contentType);
                }
                 else
                {
                    throw new InvalidOperationException("No formatter exists for format:" + format);
                }
            }        
        }
        
        public void OnResultExecuted([NotNull] ResultExecutedContext context)
        {
            
        }
    }
}