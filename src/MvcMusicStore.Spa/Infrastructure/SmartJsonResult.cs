using System.Text;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace System.Web.Mvc
{
    public class SmartJsonResult : ActionResult
    {
        public SmartJsonResult() : base()
        {
            
        }

        public JsonSerializerSettings Settings { get; set; }

        public object Data { get; set; }

        public int? StatusCode { get; set; }

        public override void ExecuteResult(ControllerContext context)
        {
            if (!context.IsChildAction)
            {
                if (StatusCode.HasValue)
                {
                    context.HttpContext.Response.StatusCode = StatusCode.Value;
                }
                context.HttpContext.Response.ContentType = "application/json";
                context.HttpContext.Response.ContentEncoding = Encoding.UTF8;
            }

            context.HttpContext.Response.Write(JsonConvert.SerializeObject(Data, Settings ?? new JsonSerializerSettings()));
        }
    }
}