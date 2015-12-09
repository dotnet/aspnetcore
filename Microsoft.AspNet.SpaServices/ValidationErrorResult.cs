using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.SpaServices
{
    public class ValidationErrorResult : ObjectResult {
        public const int DefaultStatusCode = 400;

        public ValidationErrorResult(ModelStateDictionary modelState, int errorStatusCode = DefaultStatusCode)
            : base(CreateResultObject(modelState))
        {
            if (!modelState.IsValid) {
                this.StatusCode = errorStatusCode;
            }
        }

        private static IDictionary<string, IEnumerable<string>> CreateResultObject(ModelStateDictionary modelState)
        {
            if (modelState.IsValid) {
                return null;
            } else {
                return modelState
                    .Where(m => m.Value.Errors.Any())
                    .ToDictionary(m => m.Key, m => m.Value.Errors.Select(me => me.ErrorMessage));
            }
        }
    }
}
