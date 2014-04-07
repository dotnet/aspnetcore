using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.Rendering
{
    internal static class ValidationHelpers
    {
        public static string GetUserErrorMessageOrDefault(ModelError modelError, ModelState modelState)
        {
            if (!string.IsNullOrEmpty(modelError.ErrorMessage))
            {
                return modelError.ErrorMessage;
            }

            if (modelState == null)
            {
                return string.Empty;
            }

            var attemptedValue = (modelState.Value != null) ? modelState.Value.AttemptedValue : "null";

            return Resources.FormatCommon_ValueNotValidForProperty(attemptedValue);
        }

        // Returns non-null list of model states, which caller will render in order provided.
        public static IEnumerable<ModelState> GetModelStateList(ViewDataDictionary viewData, bool excludePropertyErrors)
        {
            if (excludePropertyErrors)
            {
                ModelState ms;
                viewData.ModelState.TryGetValue(viewData.TemplateInfo.HtmlFieldPrefix, out ms);

                if (ms != null)
                {
                    return new[] { ms };
                }

                return Enumerable.Empty<ModelState>();
            }
            else
            {
                // Sort modelStates to respect the ordering in the metadata.                 
                // ModelState doesn't refer to ModelMetadata, but we can correlate via the property name.
                var ordering = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                var metadata = viewData.ModelMetadata;
                if (metadata != null)
                {
                    foreach (var data in metadata.Properties)
                    {
                        ordering[data.PropertyName] = data.Order;
                    }

                    return viewData.ModelState
                                   .OrderBy(data => ordering[data.Key])
                                   .Select(ms => ms.Value);
                }

                return viewData.ModelState.Values;
            }
        }
    }
}
