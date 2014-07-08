using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc
{
    public class SmartJsonResult : ActionResult
    {
        public SmartJsonResult() : base()
        {
            
        }

        public JsonSerializerSettings Settings { get; set; }

        public object Data { get; set; }

        public int? StatusCode { get; set; }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            //if (!context.IsChildAction)
            //{
            //    if (StatusCode.HasValue)
            //    {
            //        context.HttpContext.Response.StatusCode = StatusCode.Value;
            //    }
            //    context.HttpContext.Response.ContentType = "application/json";
            //    context.HttpContext.Response.ContentEncoding = Encoding.UTF8;
            //}
            
            return context.HttpContext.Response.WriteAsync(JsonConvert.SerializeObject(Data, Settings ?? new JsonSerializerSettings()));
        }
    }
}