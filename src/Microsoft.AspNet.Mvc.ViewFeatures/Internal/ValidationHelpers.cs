// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.ViewFeatures.Internal
{
    internal static class ValidationHelpers
    {
        public static string GetUserErrorMessageOrDefault(ModelError modelError, ModelStateEntry entry)
        {
            if (!string.IsNullOrEmpty(modelError.ErrorMessage))
            {
                return modelError.ErrorMessage;
            }

            if (entry == null)
            {
                return string.Empty;
            }

            var attemptedValue = entry.AttemptedValue ?? "null";
            return Resources.FormatCommon_ValueNotValidForProperty(attemptedValue);
        }

        // Returns non-null list of model states, which caller will render in order provided.
        public static IEnumerable<ModelStateEntry> GetModelStateList(
            ViewDataDictionary viewData,
            bool excludePropertyErrors)
        {
            if (excludePropertyErrors)
            {
                ModelStateEntry ms;
                viewData.ModelState.TryGetValue(viewData.TemplateInfo.HtmlFieldPrefix, out ms);

                if (ms != null)
                {
                    return new[] { ms };
                }

                return Enumerable.Empty<ModelStateEntry>();
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
            private readonly Dictionary<string, int> _ordering =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            public ErrorsOrderer(ModelMetadata metadata)
            {
                if (metadata == null)
                {
                    throw new ArgumentNullException(nameof(metadata));
                }

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
