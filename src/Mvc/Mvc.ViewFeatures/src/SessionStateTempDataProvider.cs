// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    /// <summary>
    /// Provides session-state data to the current <see cref="ITempDataDictionary"/> object.
    /// </summary>
    public class SessionStateTempDataProvider : ITempDataProvider
    {
        // Internal for testing
        internal const string TempDataSessionStateKey = "__ControllerTempData";
        private readonly TempDataSerializer _tempDataSerializer;

        public SessionStateTempDataProvider(TempDataSerializer tempDataSerializer)
        {
            _tempDataSerializer = tempDataSerializer;
        }

        /// <inheritdoc />
        public virtual IDictionary<string, object> LoadTempData(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Accessing Session property will throw if the session middleware is not enabled.
            var session = context.Session;

            if (session.TryGetValue(TempDataSessionStateKey, out var value))
            {
                // If we got it from Session, remove it so that no other request gets it
                session.Remove(TempDataSessionStateKey);

                return _tempDataSerializer.Deserialize(value);
            }

            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public virtual void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Accessing Session property will throw if the session middleware is not enabled.
            var session = context.Session;

            var hasValues = (values != null && values.Count > 0);
            if (hasValues)
            {
                var bytes = _tempDataSerializer.Serialize(values);
                session.Set(TempDataSessionStateKey, bytes);
            }
            else
            {
                session.Remove(TempDataSessionStateKey);
            }
        }
    }
}