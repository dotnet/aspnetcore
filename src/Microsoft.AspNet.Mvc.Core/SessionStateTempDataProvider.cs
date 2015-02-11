// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Provides session-state data to the current <see cref="ITempDataDictionary"/> object.
    /// </summary>
    public class SessionStateTempDataProvider : ITempDataProvider
    {
        private static JsonSerializer jsonSerializer = new JsonSerializer();
        private static string TempDataSessionStateKey = "__ControllerTempData";

        /// <inheritdoc />
        public virtual IDictionary<string, object> LoadTempData([NotNull] HttpContext context)
        {
            if (!IsSessionEnabled(context))
            {
                // Session middleware is not enabled. No-op
                return null;
            }

            var tempDataDictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var session = context.Session;
            byte[] value;

            if (session != null && session.TryGetValue(TempDataSessionStateKey, out value))
            {
                using (var memoryStream = new MemoryStream(value))
                using (var writer = new BsonReader(memoryStream))
                {
                    tempDataDictionary = jsonSerializer.Deserialize<Dictionary<string, object>>(writer);
                }

                // If we got it from Session, remove it so that no other request gets it
                session.Remove(TempDataSessionStateKey);
            }

            return tempDataDictionary;
        }

        /// <inheritdoc />
        public virtual void SaveTempData([NotNull] HttpContext context, IDictionary<string, object> values)
        {
            var hasValues = (values != null && values.Count > 0);
            if (hasValues)
            {
                // Accessing Session property will throw if the session middleware is not enabled.
                var session = context.Session;
                
                using (var memoryStream = new MemoryStream())
                using (var writer = new BsonWriter(memoryStream))
                {
                    jsonSerializer.Serialize(writer, values);
                    session[TempDataSessionStateKey] = memoryStream.ToArray();
                }
            }
            else if (IsSessionEnabled(context))
            {
                var session = context.Session;
                session.Remove(TempDataSessionStateKey);
            }
        }

        private static bool IsSessionEnabled(HttpContext context)
        {
            return context.GetFeature<ISessionFeature>() != null;
        }
    }
}