using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding;
using Newtonsoft.Json;

namespace Microsoft.AspNet.Mvc
{
    public class ApiResult : ActionResult
    {
        public ApiResult(ModelStateDictionary modelState)
            : this()
        {
            if (modelState.Any(m => m.Value.Errors.Count > 0))
            {
                StatusCode = 400;
                Message = "The model submitted was invalid. Please correct the specified errors and try again.";
                ModelErrors = modelState
                    .SelectMany(m => m.Value.Errors.Select(me => new ModelError
                    {
                        FieldName = m.Key,
                        ErrorMessage = me.ErrorMessage
                    }));
            }
        }

        public ApiResult()
        {
            
        }

        [JsonIgnore]
        public int? StatusCode { get; set; }
        
        public string Message { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<ModelError> ModelErrors { get; set; }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (StatusCode.HasValue)
            {
                context.HttpContext.Response.StatusCode = StatusCode.Value;
            }

            var json = new JsonResult(this);
            return json.ExecuteResultAsync(context);
        }

        public class ModelError
        {
            public string FieldName { get; set; }

            public string ErrorMessage { get; set; }
        }
    }
}