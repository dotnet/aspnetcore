// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop.Internal;
using SimpleJson;
using System;
using System.Collections.Generic;

namespace Microsoft.JSInterop
{
    internal class InteropArgSerializerStrategy : PocoJsonSerializerStrategy
    {
        private readonly JSRuntimeBase _jsRuntime;
        private const string _dotNetObjectPrefix = "__dotNetObject:";
        private object _storageLock = new object();
        private long _nextId = 1; // Start at 1, because 0 signals "no object"
        private Dictionary<long, DotNetObjectRef> _trackedRefsById = new Dictionary<long, DotNetObjectRef>();
        private Dictionary<DotNetObjectRef, long> _trackedIdsByRef = new Dictionary<DotNetObjectRef, long>();

        public InteropArgSerializerStrategy(JSRuntimeBase jsRuntime)
        {
            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
        }

        protected override bool TrySerializeKnownTypes(object input, out object output)
        {
            switch (input)
            {
                case DotNetObjectRef marshalByRefValue:
                    EnsureDotNetObjectTracked(marshalByRefValue, out var id);

                    // Special value format recognized by the code in Microsoft.JSInterop.js
                    // If we have to make it more clash-resistant, we can do
                    output = _dotNetObjectPrefix + id;

                    return true;

                case ICustomArgSerializer customArgSerializer:
                    output = customArgSerializer.ToJsonPrimitive();
                    return true;

                default:
                    return base.TrySerializeKnownTypes(input, out output);
            }
        }

        public override object DeserializeObject(object value, Type type)
        {
            if (value is string valueString)
            {
                if (valueString.StartsWith(_dotNetObjectPrefix))
                {
                    var dotNetObjectId = long.Parse(valueString.Substring(_dotNetObjectPrefix.Length));
                    return FindDotNetObject(dotNetObjectId);
                }
            }

            return base.DeserializeObject(value, type);
        }

        public object FindDotNetObject(long dotNetObjectId)
        {
            lock (_storageLock)
            {
                return _trackedRefsById.TryGetValue(dotNetObjectId, out var dotNetObjectRef)
                    ? dotNetObjectRef.Value
                    : throw new ArgumentException($"There is no tracked object with id '{dotNetObjectId}'. Perhaps the reference was already released.", nameof(dotNetObjectId));
            }
        }

        /// <summary>
        /// Stops tracking the specified .NET object reference.
        /// This overload is typically invoked from JS code via JS interop.
        /// </summary>
        /// <param name="dotNetObjectId">The ID of the <see cref="DotNetObjectRef"/>.</param>
        public void ReleaseDotNetObject(long dotNetObjectId)
        {
            lock (_storageLock)
            {
                if (_trackedRefsById.TryGetValue(dotNetObjectId, out var dotNetObjectRef))
                {
                    _trackedRefsById.Remove(dotNetObjectId);
                    _trackedIdsByRef.Remove(dotNetObjectRef);
                }
            }
        }

        /// <summary>
        /// Stops tracking the specified .NET object reference.
        /// This overload is typically invoked from .NET code by <see cref="DotNetObjectRef.Dispose"/>.
        /// </summary>
        /// <param name="dotNetObjectRef">The <see cref="DotNetObjectRef"/>.</param>
        public void ReleaseDotNetObject(DotNetObjectRef dotNetObjectRef)
        {
            lock (_storageLock)
            {
                if (_trackedIdsByRef.TryGetValue(dotNetObjectRef, out var dotNetObjectId))
                {
                    _trackedRefsById.Remove(dotNetObjectId);
                    _trackedIdsByRef.Remove(dotNetObjectRef);
                }
            }
        }

        private void EnsureDotNetObjectTracked(DotNetObjectRef dotNetObjectRef, out long dotNetObjectId)
        {
            dotNetObjectRef.EnsureAttachedToJsRuntime(_jsRuntime);

            lock (_storageLock)
            {
                // Assign an ID only if it doesn't already have one
                if (!_trackedIdsByRef.TryGetValue(dotNetObjectRef, out dotNetObjectId))
                {
                    dotNetObjectId = _nextId++;
                    _trackedRefsById.Add(dotNetObjectId, dotNetObjectRef);
                    _trackedIdsByRef.Add(dotNetObjectRef, dotNetObjectId);
                }
            }
        }
    }
}
