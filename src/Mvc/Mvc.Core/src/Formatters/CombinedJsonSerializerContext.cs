// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    internal sealed class CompositeJsonserializerContext
    {
        private List<JsonSerializerContext>? _serializerContexts;

        public void Add(JsonSerializerContext jsonSerializerContext)
        {
            _serializerContexts ??= new();
            _serializerContexts.Add(jsonSerializerContext);
        }

        public JsonSerializerContext? GetSerializerContext(Type type)
        {
            if (_serializerContexts is null)
            {
                return null;
            }

            foreach (var context in CollectionsMarshal.AsSpan(_serializerContexts))
            {
                if (context.GetTypeInfo(type) is not null)
                {
                    return context;
                }
            }

            return null;
        }
    }
}
