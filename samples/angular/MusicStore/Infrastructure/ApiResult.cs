using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicStore.Infrastructure
{
    public class ApiResult : ActionResult
    {
        public ApiResult(ModelStateDictionary modelState)
            : this()
        {
            if (modelState.Any(m => m.Value.Errors.Any()))
            {
                StatusCode = 400;
                Message = "The model submitted was invalid. Please correct the specified errors and try again.";
                ModelErrors = modelState
                    .Where(m => m.Value.Errors.Any())
                    .ToDictionary(m => m.Key, m => m.Value.Errors.Select(me => me.ErrorMessage ));
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
        public IDictionary<string, IEnumerable<string>> ModelErrors { get; set; }

        public override Task ExecuteResultAsync(ActionContext context)
        {
            if (StatusCode.HasValue)
            {
                context.HttpContext.Response.StatusCode = StatusCode.Value;
            }

            return new ObjectResult(this).ExecuteResultAsync(context);
        }
    }
}
