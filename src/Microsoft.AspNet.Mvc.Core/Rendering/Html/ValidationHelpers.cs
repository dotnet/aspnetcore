// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
        public static IEnumerable<ModelState> GetModelStateList(
            ViewDataDictionary viewData,
            bool excludePropertyErrors)
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
                var orderer = new ErrorsOrderer(metadata);

                return viewData.ModelState
                    .OrderBy(data => orderer.GetOrder(data.Key))
                    .Select(ms => ms.Value);
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
