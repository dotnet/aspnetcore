// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
                var metadata = viewData.ModelMetadata;
                if (metadata != null)
                {
                    var orderer = new ErrorsOrderer(metadata);

                    return viewData.ModelState
                                   .OrderBy(data => orderer.GetOrder(data.Key))
                                   .Select(ms => ms.Value);
                }

                return viewData.ModelState.Values;
            }
        }

        // Helper for sorting modelStates to respect the ordering in the metadata.
        // ModelState doesn't refer to ModelMetadata, but we can correlate via the property name.
        private class ErrorsOrderer
        {
            private Dictionary<string, int> _ordering = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            public ErrorsOrderer([NotNull] ModelMetadata metadata)
            {
                foreach (var data in metadata.Properties)
                {
                    _ordering[data.PropertyName] = data.Order;
                }
            }

            public int GetOrder(string key)
            {
                int value;
                if (_ordering.TryGetValue(key, out value))
                {
                    return value;
                }
            
                return ModelMetadata.DefaultOrder;
            }
        }
    }
}
